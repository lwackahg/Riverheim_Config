using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using RiverheimConfig;

namespace RiverheimConfig.Tests
{
    // Command-line checks of RiverheimSettings against a Riverheim assembly. Loads the DLL by reflection, asks its
    // ConfigManager for the default generation config, and compares. Works with Riverheim.dll (the BepInEx plugin,
    // whose config types do not touch Unity) and with the Riverheim.Core.dll that AtlasBuilder unpacks.
    //
    //   rhconfig-test check <Riverheim.dll>                 every path resolves, defaults equal Riverheim's, forced apply of defaults changes nothing
    //   rhconfig-test stock-cfg <Riverheim.dll> <out.cfg>   writes a cfg holding the DLL's own values for every setting (for the AtlasBuilder tests)
    //   rhconfig-test apply <Riverheim.dll> <cfg>           applies a cfg and prints every field that changed against the defaults
    //   rhconfig-test dump <Riverheim.dll> [version|master] prints every leaf field of the default config
    //   rhconfig-test json <out.json>                       exports the settings table for the AtlasBuilder launcher
    //   rhconfig-test md <out.md>                           writes SETTINGS.md
    //   rhconfig-test cfg <out.cfg>                         writes a cfg with every setting at its listed default
    //   rhconfig-test retarget <Riverheim.dll> <Riverheim.Dev.dll> <outDir> [version]
    //                                                       makes AtlasBuilder run the given Riverheim (see Retarget)
    public static class Program
    {
        private const BindingFlags Any = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("usage: rhconfig-test check|stock-cfg|apply|dump <Riverheim.dll> [...] | json <out.json> | md <out.md> | cfg <out.cfg> | retarget <Riverheim.dll> <Riverheim.Dev.dll> <outDir> [version]");
                return 2;
            }
            switch (args[0])
            {
                case "check": return Check(args[1]);
                case "stock-cfg": return StockCfg(args[1], args[2]);
                case "apply": return ApplyCfg(args[1], args[2]);
                case "dump": return Dump(args[1], args.Length > 2 ? args[2] : null);
                case "retarget": return Retarget(args[1], args[2], args[3], args.Length > 4 ? int.Parse(args[4], CultureInfo.InvariantCulture) : (int?)null);
                case "json": File.WriteAllText(args[1], Json(), new UTF8Encoding(false)); Console.WriteLine("Wrote " + args[1]); return 0;
                case "md": File.WriteAllText(args[1], Markdown(), new UTF8Encoding(false)); Console.WriteLine("Wrote " + args[1]); return 0;
                case "cfg": File.WriteAllText(args[1], Cfg(null), new UTF8Encoding(false)); Console.WriteLine("Wrote " + args[1]); return 0;
                default: Console.Error.WriteLine("Unknown command " + args[0]); return 2;
            }
        }

        private sealed class Riverheim
        {
            public Assembly Assembly;
            public Type ConfigManager;
            public int DefaultVersion;
            public string Name;

            public object DefaultConfig(int version)
            {
                return ConfigManager.GetMethod("GetDefaultConfig", Any).Invoke(null, new object[] { version });
            }

            public object MasterConfig()
            {
                return ConfigManager.GetMethod("GetMasterConfig", Any).Invoke(null, null);
            }
        }

        private static Riverheim Load(string path)
        {
            string full = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(full);
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                string candidate = Path.Combine(directory, name.Name + ".dll");
                return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
            };
            Assembly assembly = Assembly.LoadFrom(full);
            Type manager = assembly.GetType("Riverheim.Configuration.ConfigManager");
            if (manager == null)
            {
                throw new InvalidOperationException(full + " has no Riverheim.Configuration.ConfigManager");
            }
            return new Riverheim
            {
                Assembly = assembly,
                ConfigManager = manager,
                DefaultVersion = (int)manager.GetField("PRESET_DEFAULT_VERSION", Any).GetValue(null),
                Name = assembly.GetName().Name + " " + assembly.GetName().Version + " (default preset version " + (int)manager.GetField("PRESET_DEFAULT_VERSION", Any).GetValue(null) + ")"
            };
        }

        private static int Check(string dll)
        {
            Riverheim riverheim = Load(dll);
            Console.WriteLine("Checking " + RiverheimSettings.All.Length + " settings against " + riverheim.Name);
            object stock = riverheim.DefaultConfig(riverheim.DefaultVersion);
            int failures = 0;
            int skipped = 0;

            // 1. Every path resolves and the listed default equals Riverheim's value.
            foreach (RiverheimSettings.Setting setting in RiverheimSettings.All)
            {
                if (setting.Path == null)
                {
                    continue;
                }
                object actual = RiverheimSettings.Read(stock, setting.Path);
                if (actual == null)
                {
                    if (setting.Optional)
                    {
                        Console.WriteLine("  skip  " + setting.Key + ": " + setting.Path + " does not exist in this build (optional)");
                        skipped++;
                        continue;
                    }
                    Console.WriteLine("  FAIL  " + setting.Key + ": " + setting.Path + " does not exist");
                    failures++;
                    continue;
                }
                double value = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
                if (Math.Abs(value - setting.Default) > 1e-9)
                {
                    Console.WriteLine("  FAIL  " + setting.Key + ": listed default " + setting.DefaultText + " but Riverheim has " + value.ToString(CultureInfo.InvariantCulture) + " (" + setting.Path + ")");
                    failures++;
                }
            }

            // 2. Forcing every default through Apply reproduces the stock config exactly (this also checks the band curves).
            object applied = riverheim.DefaultConfig(riverheim.DefaultVersion);
            List<string> failed = new List<string>();
            List<string> skippedPaths = new List<string>();
            RiverheimSettings.Apply(applied, new Dictionary<string, double>(), true, failed, skippedPaths);
            foreach (string failure in failed)
            {
                Console.WriteLine("  FAIL  apply: " + failure);
                failures++;
            }
            List<string> differences = Diff(Flatten(stock), Flatten(applied));
            foreach (string difference in differences)
            {
                Console.WriteLine("  FAIL  forced defaults changed " + difference);
                failures++;
            }

            // 3. Applying nothing (all defaults, not forced) touches nothing.
            object untouched = riverheim.DefaultConfig(riverheim.DefaultVersion);
            RiverheimSettings.Apply(untouched, new Dictionary<string, double>(), false, failed, skippedPaths);
            foreach (string difference in Diff(Flatten(stock), Flatten(untouched)))
            {
                Console.WriteLine("  FAIL  unforced defaults changed " + difference);
                failures++;
            }

            // 4. Every setting changes exactly its own field when set away from the default.
            foreach (RiverheimSettings.Setting setting in RiverheimSettings.All)
            {
                if (setting.Path == null || (setting.Optional && !RiverheimSettings.Exists(stock, setting.Path)))
                {
                    continue;
                }
                object probe = riverheim.DefaultConfig(riverheim.DefaultVersion);
                Dictionary<string, double> values = new Dictionary<string, double>();
                values[setting.Id] = setting.Default + (setting.IsInteger ? 1 : Math.Max(1, Math.Abs(setting.Default)) * 0.5);
                List<string> probeFailed = new List<string>();
                RiverheimSettings.Apply(probe, values, false, probeFailed, new List<string>());
                List<string> changed = Diff(Flatten(stock), Flatten(probe));
                if (probeFailed.Count > 0 || changed.Count != 1 || !changed[0].StartsWith(setting.Path + ":"))
                {
                    Console.WriteLine("  FAIL  " + setting.Key + " changed " + (changed.Count == 0 ? "nothing" : string.Join("; ", changed)) + (probeFailed.Count > 0 ? " and failed: " + string.Join(", ", probeFailed) : ""));
                    failures++;
                }
            }

            Console.WriteLine(failures == 0
                ? "OK: all paths resolve, defaults match, " + skipped + " optional setting(s) skipped"
                : failures + " failure(s)");
            return failures == 0 ? 0 : 1;
        }

        private static int StockCfg(string dll, string output)
        {
            Riverheim riverheim = Load(dll);
            object stock = riverheim.DefaultConfig(riverheim.DefaultVersion);
            Dictionary<string, double> values = new Dictionary<string, double>();
            foreach (RiverheimSettings.Setting setting in RiverheimSettings.All)
            {
                if (setting.Path == null)
                {
                    continue;
                }
                object actual = RiverheimSettings.Read(stock, setting.Path);
                if (actual != null)
                {
                    values[setting.Id] = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
                }
            }
            File.WriteAllText(output, Cfg(values), new UTF8Encoding(false));
            Console.WriteLine("Wrote " + output + " with " + riverheim.Name + "'s own values");
            return 0;
        }

        private static int ApplyCfg(string dll, string cfg)
        {
            Riverheim riverheim = Load(dll);
            object stock = riverheim.DefaultConfig(riverheim.DefaultVersion);
            object applied = riverheim.DefaultConfig(riverheim.DefaultVersion);
            Dictionary<string, double> values = RiverheimSettings.Parse(File.ReadAllLines(cfg));
            List<string> failed = new List<string>();
            List<string> skipped = new List<string>();
            RiverheimSettings.Apply(applied, values, false, failed, skipped);
            Console.WriteLine(RiverheimSettings.Summary(values));
            foreach (string difference in Diff(Flatten(stock), Flatten(applied)))
            {
                Console.WriteLine("  " + difference);
            }
            foreach (string skip in skipped)
            {
                Console.WriteLine("  skipped (not in this build): " + skip);
            }
            foreach (string failure in failed)
            {
                Console.WriteLine("  FAILED: " + failure);
            }
            return failed.Count == 0 ? 0 : 1;
        }

        // Prepares an installed Riverheim (the BepInEx plugin Riverheim.dll) to run inside Gurebu's AtlasBuilder, so the
        // render tests use the player's actual Riverheim instead of the older build AtlasBuilder bundles:
        //   1. Riverheim.dll is written as Riverheim.Core.dll, the assembly name AtlasBuilder references. The code is
        //      unchanged; its pipeline registry scans the whole assembly, so BepInEx, Harmony and Unity assemblies
        //      must be resolvable at run time (tests/DepsHook.cs does that).
        //   2. AtlasBuilder's Riverheim.Dev.dll hard-codes the Default preset as ("builtin.amur", 1). The version is
        //      replaced by the Riverheim's own PRESET_DEFAULT_VERSION (2 in 1.2.1), which is what new worlds get in
        //      game, so --preset Default renders the world a player would see.
        // Nothing is modified in place; both files are written to outDir.
        private static int Retarget(string riverheimDll, string devDll, string outDir, int? versionOverride)
        {
            Directory.CreateDirectory(outDir);
            Mono.Cecil.AssemblyDefinition core = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.GetFullPath(riverheimDll));
            Mono.Cecil.TypeDefinition manager = core.MainModule.GetType("Riverheim.Configuration.ConfigManager");
            if (manager == null)
            {
                Console.Error.WriteLine(riverheimDll + " has no Riverheim.Configuration.ConfigManager");
                return 1;
            }
            int version = versionOverride ?? (int)manager.Fields.First(f => f.Name == "PRESET_DEFAULT_VERSION").Constant;
            Console.WriteLine("Retargeting AtlasBuilder to " + core.Name.Name + " " + core.Name.Version + " (default preset version " + version + ")");
            core.Name.Name = "Riverheim.Core";
            core.MainModule.Name = "Riverheim.Core.dll";
            core.Write(Path.Combine(outDir, "Riverheim.Core.dll"));

            Mono.Cecil.AssemblyDefinition dev = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.GetFullPath(devDll));
            int patched = 0;
            foreach (Mono.Cecil.TypeDefinition type in dev.MainModule.GetTypes())
            {
                foreach (Mono.Cecil.MethodDefinition method in type.Methods.Where(m => m.HasBody))
                {
                    // Pattern: ldstr "builtin.amur"; ldc.i4 <version>; newobj WorldSettings(string, int)
                    Mono.Collections.Generic.Collection<Mono.Cecil.Cil.Instruction> code = method.Body.Instructions;
                    for (int i = 0; i + 2 < code.Count; i++)
                    {
                        Mono.Cecil.MethodReference ctor = code[i + 2].Operand as Mono.Cecil.MethodReference;
                        if (code[i].OpCode == Mono.Cecil.Cil.OpCodes.Ldstr && (string)code[i].Operand == "builtin.amur"
                            && ctor != null && ctor.DeclaringType.Name == "WorldSettings")
                        {
                            code[i + 1].OpCode = Mono.Cecil.Cil.OpCodes.Ldc_I4;
                            code[i + 1].Operand = version;
                            patched++;
                            Console.WriteLine("  " + type.Name + "." + method.Name + ": Default preset version -> " + version);
                        }
                    }
                }
            }
            dev.Write(Path.Combine(outDir, "Riverheim.Dev.dll"));
            if (patched == 0)
            {
                Console.Error.WriteLine("No Default preset definition found in " + devDll + "; AtlasBuilder's layout changed.");
                return 1;
            }
            Console.WriteLine("Wrote Riverheim.Core.dll and Riverheim.Dev.dll to " + outDir);
            return 0;
        }

        private static int Dump(string dll, string version)
        {
            Riverheim riverheim = Load(dll);
            object config = version == "master" ? riverheim.MasterConfig() : riverheim.DefaultConfig(version == null ? riverheim.DefaultVersion : int.Parse(version));
            foreach (KeyValuePair<string, string> leaf in Flatten(config))
            {
                Console.WriteLine(leaf.Key + " = " + leaf.Value);
            }
            return 0;
        }

        // Every leaf field of a config as path -> text, so two configs can be compared.
        private static List<KeyValuePair<string, string>> Flatten(object config)
        {
            List<KeyValuePair<string, string>> leaves = new List<KeyValuePair<string, string>>();
            Walk("", config, leaves, 0);
            return leaves;
        }

        private static void Walk(string path, object value, List<KeyValuePair<string, string>> leaves, int depth)
        {
            if (value == null)
            {
                leaves.Add(new KeyValuePair<string, string>(path, "null"));
                return;
            }
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || depth > 12)
            {
                leaves.Add(new KeyValuePair<string, string>(path, Convert.ToString(value, CultureInfo.InvariantCulture)));
                return;
            }
            if (type.IsGenericType && type.Name.StartsWith("StaticArray"))
            {
                int count = (int)type.GetField("count", Any).GetValue(value);
                List<string> points = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    object point = type.GetField("p" + i, Any).GetValue(value);
                    points.Add("(" + string.Join(" ", point.GetType().GetFields(Any).Where(f => !f.IsStatic).Select(f => Convert.ToString(f.GetValue(point), CultureInfo.InvariantCulture))) + ")");
                }
                leaves.Add(new KeyValuePair<string, string>(path, "[" + string.Join(", ", points) + "]"));
                return;
            }
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                string name = field.Name.StartsWith("<") ? field.Name.Substring(1, field.Name.IndexOf('>') - 1) : field.Name;
                Walk(path.Length == 0 ? name : path + "." + name, field.GetValue(value), leaves, depth + 1);
            }
        }

        private static List<string> Diff(List<KeyValuePair<string, string>> before, List<KeyValuePair<string, string>> after)
        {
            Dictionary<string, string> previous = before.ToDictionary(p => p.Key, p => p.Value);
            List<string> differences = new List<string>();
            foreach (KeyValuePair<string, string> leaf in after)
            {
                string old;
                if (!previous.TryGetValue(leaf.Key, out old) || old != leaf.Value)
                {
                    differences.Add(leaf.Key + ": " + (old ?? "(missing)") + " -> " + leaf.Value);
                }
            }
            return differences;
        }

        // A cfg in the plugin's format. Values default to the listed defaults.
        private static string Cfg(IDictionary<string, double> values)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("## Riverheim Config settings. Written by rhconfig-test.");
            string section = null;
            foreach (RiverheimSettings.Setting setting in RiverheimSettings.All)
            {
                if (setting.Section != section)
                {
                    section = setting.Section;
                    text.AppendLine();
                    text.AppendLine("[" + section + "]");
                }
                double value;
                if (values == null || !values.TryGetValue(setting.Id, out value))
                {
                    value = setting.Default;
                }
                text.AppendLine(setting.Key + " = " + value.ToString("0.##########", CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        // settings.json for the launcher: sections with titles, then one entry per setting in display order.
        private static string Json()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("{");
            text.AppendLine("  \"generatedFrom\": \"RiverheimSettings.cs\",");
            text.AppendLine("  \"sections\": [");
            for (int i = 0; i < RiverheimSettings.Sections.Length; i++)
            {
                RiverheimSettings.Section section = RiverheimSettings.Sections[i];
                text.AppendLine("    { \"name\": " + Quote(section.Name) + ", \"title\": " + Quote(section.Title) + " }" + (i < RiverheimSettings.Sections.Length - 1 ? "," : ""));
            }
            text.AppendLine("  ],");
            text.AppendLine("  \"settings\": [");
            for (int i = 0; i < RiverheimSettings.All.Length; i++)
            {
                RiverheimSettings.Setting setting = RiverheimSettings.All[i];
                text.AppendLine("    {");
                text.AppendLine("      \"section\": " + Quote(setting.Section) + ",");
                text.AppendLine("      \"key\": " + Quote(setting.Key) + ",");
                text.AppendLine("      \"label\": " + Quote(setting.Label) + ",");
                text.AppendLine("      \"default\": " + Num(setting.Default) + ",");
                text.AppendLine("      \"integer\": " + (setting.IsInteger ? "true" : "false") + ",");
                text.AppendLine("      \"min\": " + Num(setting.Min) + ",");
                text.AppendLine("      \"max\": " + Num(setting.Max) + ",");
                text.AppendLine("      \"step\": " + Num(setting.Step) + ",");
                text.AppendLine("      \"decimals\": " + setting.Decimals + ",");
                text.AppendLine("      \"path\": " + (setting.Path == null ? "null" : Quote(setting.Path)) + ",");
                text.AppendLine("      \"optional\": " + (setting.Optional ? "true" : "false") + ",");
                text.AppendLine("      \"previewNote\": " + (setting.PreviewNote == null ? "null" : Quote(setting.PreviewNote)) + ",");
                text.AppendLine("      \"description\": " + Quote(setting.Description));
                text.AppendLine("    }" + (i < RiverheimSettings.All.Length - 1 ? "," : ""));
            }
            text.AppendLine("  ]");
            text.AppendLine("}");
            return text.ToString();
        }

        // SETTINGS.md: the settings reference, one table per section, straight from the same table the mod binds.
        private static string Markdown()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("# Riverheim Config settings");
            text.AppendLine();
            text.AppendLine("Generated from `RiverheimSettings.cs` by `rhconfig-test md`; do not edit by hand. Defaults are Riverheim 1.2.1's own values.");
            text.AppendLine("A setting left at its default is not written, so Riverheim keeps its own value for it. Section names are the headings in");
            text.AppendLine("`BepInEx/config/com.valheim.riverheim.config.cfg`.");
            foreach (RiverheimSettings.Section section in RiverheimSettings.Sections)
            {
                text.AppendLine();
                text.AppendLine("## [" + section.Name + "] " + section.Title);
                text.AppendLine();
                text.AppendLine("| Setting | Default | Range | What it does |");
                text.AppendLine("| --- | --- | --- | --- |");
                foreach (RiverheimSettings.Setting setting in RiverheimSettings.All)
                {
                    if (setting.Section != section.Name)
                    {
                        continue;
                    }
                    string notes = setting.Description;
                    if (setting.PreviewNote != null)
                    {
                        notes += " " + setting.PreviewNote;
                    }
                    text.AppendLine("| `" + setting.Key + "` (" + setting.Label + ") | " + setting.DefaultText + " | " + Num(setting.Min) + " to " + Num(setting.Max) + (setting.IsInteger ? ", whole number" : "") + " | " + notes.Replace("|", "\\|") + " |");
                }
            }
            return text.ToString();
        }

        private static string Num(double value)
        {
            return value.ToString("0.##########", CultureInfo.InvariantCulture);
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
