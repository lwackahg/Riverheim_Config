using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RiverheimConfig
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class RiverheimConfigPlugin : BaseUnityPlugin
    {
        private const string PluginGuid = "com.valheim.riverheim.config";
        private const string PluginName = "Riverheim Config";
        private const string PluginVersion = "2.0.0";
        private const string RiverheimConfigMethod = "Riverheim.Configuration.ConfigManager:GetConfig";

        private static ManualLogSource log;
        private static ConfigSync configSync;
        private static bool patchApplied;
        private static readonly Dictionary<string, ConfigEntryBase> entries = new Dictionary<string, ConfigEntryBase>();

        private void Awake()
        {
            log = Logger;
            configSync = new ConfigSync(PluginGuid)
            {
                DisplayName = PluginName,
                CurrentVersion = PluginVersion,
                MinimumRequiredVersion = PluginVersion
            };

            BindConfiguration();
        }

        // BepInEx loads plugins in directory order. Riverheim Config can therefore Awake before
        // Riverheim itself, whose assembly is not discoverable through Harmony until its plugin loads.
        // Start runs on the next Unity lifecycle frame after the plugin loader has completed.
        private IEnumerator Start()
        {
            yield return null;
            ApplyPatch();
        }

        private void BindConfiguration()
        {
            foreach (RiverheimSettings.Setting setting in RiverheimSettings.All)
            {
                ConfigDescription description = new ConfigDescription(setting.Description + " Riverheim 1.1 default: " + setting.DefaultText + ".");
                if (setting.IsInteger)
                {
                    ConfigEntry<int> entry = Config.Bind(setting.Section, setting.Key, (int)setting.Default, description);
                    configSync.AddConfigEntry(entry).SynchronizedConfig = true;
                    entries[setting.Id] = entry;
                }
                else
                {
                    ConfigEntry<double> entry = Config.Bind(setting.Section, setting.Key, setting.Default, description);
                    configSync.AddConfigEntry(entry).SynchronizedConfig = true;
                    entries[setting.Id] = entry;
                }
            }
        }

        private static void ApplyPatch()
        {
            MethodInfo target = AccessTools.Method(RiverheimConfigMethod);
            if (target == null)
            {
                log.LogError("Riverheim 1.1 ConfigManager.GetConfig was not found. Riverheim Config 2.0.0 is disabled; install Gurebu-Riverheim 1.1.x.");
                return;
            }

            new Harmony(PluginGuid).Patch(target, postfix: new HarmonyMethod(typeof(RiverheimConfigPlugin), nameof(PatchGenerationConfig)));
            patchApplied = true;
            log.LogInfo("Riverheim 1.1 configuration patch applied. Server configuration is synchronized to clients.");
        }

        // Riverheim calls this immediately before computing its generation pipeline. The result is a boxed
        // WorldGenerationConfig, so field-path updates work without taking a compile-time dependency on its internals.
        private static void PatchGenerationConfig(ref object __result)
        {
            if (__result == null)
            {
                log.LogError("Riverheim returned a null world generation configuration.");
                return;
            }

            Dictionary<string, double> values = new Dictionary<string, double>();
            foreach (KeyValuePair<string, ConfigEntryBase> pair in entries)
            {
                values[pair.Key] = Convert.ToDouble(pair.Value.BoxedValue, CultureInfo.InvariantCulture);
            }

            List<string> failed = new List<string>();
            RiverheimSettings.Apply(__result, values, failed);
            if (failed.Count > 0)
            {
                log.LogError("Riverheim 1.1 configuration layout changed or a value is invalid; these settings were not applied: " + string.Join(", ", failed.ToArray()));
                return;
            }

            log.LogInfo("Applied Riverheim config: " + RiverheimSettings.Summary(values) + ".");
        }

        private void OnDestroy()
        {
            if (patchApplied)
            {
                new Harmony(PluginGuid).UnpatchSelf();
                patchApplied = false;
            }
        }
    }

    // Riverheim Config's settings and how they map onto Riverheim 1.1's WorldGenerationConfig.
    // Deliberately free of BepInEx, Harmony and Unity so it can be tested against Riverheim.dll outside the game.
    public static class RiverheimSettings
    {
        public const string World = "1. World";
        public const string Rivers = "2. Rivers";
        public const string Lakes = "3. Lakes";
        public const string Biomes = "4. Biomes";
        public const string Amounts = "5. Biome Amounts";
        public const string Bands = "6. Biome Bands";

        public sealed class Setting
        {
            public string Section;
            public string Key;
            public double Default;
            public bool IsInteger;
            public string Path;
            public string Description;

            public string Id { get { return Section + "|" + Key; } }

            public string DefaultText { get { return Default.ToString(CultureInfo.InvariantCulture); } }
        }

        private static Setting Number(string section, string key, double value, string path, string description)
        {
            return new Setting { Section = section, Key = key, Default = value, Path = path, Description = description };
        }

        private static Setting Whole(string section, string key, int value, string path, string description)
        {
            return new Setting { Section = section, Key = key, Default = value, IsInteger = true, Path = path, Description = description };
        }

        // Defaults are Riverheim 1.1's own values (DefaultConfig plus the default preset's version overrides).
        // Settings without a path are biome bands, applied as curves in Apply.
        public static readonly Setting[] All =
        {
            Number(World, "WorldRadius", 10500, "common.worldSize",
                "Radius of the world Riverheim generates, in metres. Keep 10500 and use Expand World Size stretching for a bigger or smaller world: biome bands scale with this value, but the poles, the landmass swirl and the rainfall pattern do not, so changing it distorts the layout."),
            Number(World, "TileSpacing", 60, "common.tileSpacing",
                "Distance between generation points in metres. Lower values add detail but take much longer, and river and lake counts grow with the number of points."),
            Number(World, "SeaFloorDepth", -40, "common.oceanDepth",
                "Height the sea floor settles to at the world edge and in the polar trenches, also used as the floor when shaping terrain. More negative values mean deeper edges and trenches."),
            Number(World, "OceanBiomeDepth", -20, "world.biomes.conditionalPlacement.main.oceanDepth",
                "Water must be deeper than this, and far enough from the coast, to count as the Ocean biome. More negative values mean fewer waters count as Ocean."),
            Number(World, "MountainHeight", 50, "common.mountainHeight",
                "Terrain at or above this height becomes the Mountain biome, and terrain shaping around mountains uses it too. Lower values make more mountains."),
            Number(World, "StartingAreaRadius", 870, "common.startingAreaRadius",
                "Size of the gentle area around spawn in metres, where hills are shaped like flatland. Larger values make a bigger gentle start."),

            Number(Rivers, "OriginDensity", 0.25, "world.rivers.origins.main.density",
                "Share of suitable coastline tiles that become river mouths. Higher values make more rivers; 0 turns rivers off."),
            Whole(Rivers, "MinLandNeighbors", 2, "world.rivers.origins.main.minLandNeighbors",
                "Land tiles a coastline tile needs next to it to start a river. Higher values keep river mouths off thin peninsulas and small islands."),
            Number(Rivers, "WidthScale", 140, "world.rivers.width.main.scale",
                "River width = WidthScale x flow ^ WidthPower + WidthOffset, never below zero. Scales every river's width."),
            Number(Rivers, "WidthPower", 0.4, "world.rivers.width.main.power",
                "How strongly river width grows with water flow. Higher values make big rivers much wider than small streams."),
            Number(Rivers, "WidthOffset", -6, "world.rivers.width.main.offset",
                "Added to every river width. Negative values thin out small streams."),
            Whole(Rivers, "MinStrahler", 2, "world.rivers.prune.main.minStrahler",
                "River branches below this Strahler order are removed. Higher values keep only the bigger branches."),
            Number(Rivers, "MinWidth", 14.5, "world.rivers.prune.main.minWidth",
                "River segments narrower than this are removed. Higher values drop thin streams."),
            Number(Rivers, "MaxWidthDifference", 2.2, "world.rivers.prune.main.maxWidthDiff",
                "Where rivers join, a side branch is removed if the widest incoming branch is more than this many times wider. Higher values keep more small tributaries; 0 keeps them all."),
            Number(Rivers, "MeanderPeriod", 3.4, "world.rivers.meander.main.period",
                "Length of river bends, measured in river widths. Higher values make longer, gentler bends."),
            Number(Rivers, "MeanderAmplitude", 1.35, "world.rivers.meander.main.riverAmplitude",
                "Strength of river bends. Higher values make rivers wind more."),

            Number(Lakes, "Budget", 0.0055, "world.height.lakes.main.budget",
                "Share of generation points picked as lake starting points, which then grow into lakes. Higher values make more lake area."),
            Number(Lakes, "NoiseScale", 600, "world.height.lakes.main.affinity.noiseScale",
                "Size of the random pattern that decides where lakes prefer to form. Larger values group lakes into bigger regions."),
            Number(Lakes, "LowlandContribution", 0.35, "world.height.lakes.main.affinity.kFlatland",
                "How strongly lakes prefer flat lowland."),
            Number(Lakes, "CuriosityContribution", 0.8, "world.height.lakes.main.affinity.kCuriosity",
                "How strongly lakes prefer notable spots along river systems."),

            Number(Biomes, "MeadowsBias", 0, "world.biomes.affinity.main.meadows.bias",
                "Flat bonus to the Meadows score wherever biomes compete for land. Higher values let it win more land."),
            Number(Biomes, "ForestBias", -10, "world.biomes.affinity.main.forest.bias",
                "Flat bonus to the Black Forest score wherever biomes compete for land. Higher values let it win more land."),
            Number(Biomes, "SwampBias", 8, "world.biomes.affinity.main.swamp.bias",
                "Flat bonus to the Swamp score wherever biomes compete for land. Higher values let it win more land."),
            Number(Biomes, "PlainsBias", 5, "world.biomes.affinity.main.plains.bias",
                "Flat bonus to the Plains score wherever biomes compete for land. Higher values let it win more land."),
            Number(Biomes, "MistlandsBias", 0, "world.biomes.affinity.main.mistlands.bias",
                "Flat bonus to the Mistlands score wherever biomes compete for land. Higher values let it win more land."),

            Number(Amounts, "MeadowsWeight", 69.5, "world.biomes.competitivePlacement.main.biomes.meadows.weight",
                "How much of the contested land Meadows takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands."),
            Number(Amounts, "ForestWeight", 261, "world.biomes.competitivePlacement.main.biomes.forest.weight",
                "How much of the contested land Black Forest takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands."),
            Number(Amounts, "SwampWeight", 76, "world.biomes.competitivePlacement.main.biomes.swamp.weight",
                "How much of the contested land Swamp takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands."),
            Number(Amounts, "PlainsWeight", 283, "world.biomes.competitivePlacement.main.biomes.plains.weight",
                "How much of the contested land Plains takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands."),
            Number(Amounts, "MistlandsWeight", 305, "world.biomes.competitivePlacement.main.biomes.mistlands.weight",
                "How much of the contested land Mistlands takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands."),

            Number(Bands, "MeadowsFadeEnd", 50, null,
                "Meadows becomes less likely the further you travel from spawn and reaches its full penalty at this percentage of the world radius. Lower values keep Meadows closer to spawn."),
            Number(Bands, "SwampStart", 20, null,
                "Swamp can appear from this percentage of the way from spawn to the world edge (travel distance), fading in over the 2% before it."),
            Number(Bands, "SwampEnd", 58, null,
                "Swamp stops appearing past this percentage of the way from spawn to the world edge, fading out over the next 2%."),
            Number(Bands, "PlainsStart", 29, null,
                "Plains can appear from this percentage of the way from spawn to the world edge (travel distance), fading in over the 2% before it."),
            Number(Bands, "PlainsEnd", 66, null,
                "Plains stops appearing past this percentage of the way from spawn to the world edge, fading out over the next 2%."),
            Number(Bands, "MistlandsStart", 60, null,
                "Mistlands can appear from this percentage of the way from spawn to the world edge (travel distance) onwards, fading in over the 5% before it.")
        };

        private const string Affinity = "world.biomes.affinity.main.";

        public static void Apply(object config, IDictionary<string, double> values, List<string> failed)
        {
            foreach (Setting setting in All)
            {
                if (setting.Path != null)
                {
                    double value = Value(values, setting.Section, setting.Key);
                    Assign(config, setting.Path, failed, fieldType => ConvertValue(value, fieldType));
                }
            }

            // Biome bands are Riverheim's relativeTravelDistance curves: (share of the world radius, score) points.
            // With the default percentages these rebuild exactly Riverheim 1.1's default curves.
            double meadowsFade = Value(values, Bands, "MeadowsFadeEnd") / 100.0;
            if (meadowsFade > 0)
            {
                SetCurve(config, Affinity + "meadows.relativeTravelDistance", failed, 0, 0, meadowsFade, -40);
            }
            else
            {
                failed.Add("MeadowsFadeEnd (must be above 0)");
            }

            double swampStart = Value(values, Bands, "SwampStart") / 100.0;
            double swampEnd = Value(values, Bands, "SwampEnd") / 100.0;
            if (swampStart - 0.02 > 0 && swampEnd > swampStart)
            {
                SetCurve(config, Affinity + "swamp.relativeTravelDistance", failed,
                    0, -1000, Round(swampStart - 0.02), -100, swampStart, 0, swampEnd, 0, Round(swampEnd + 0.02), -100);
            }
            else
            {
                failed.Add("SwampStart/SwampEnd (start must be above 2% and below the end)");
            }

            double plainsStart = Value(values, Bands, "PlainsStart") / 100.0;
            double plainsEnd = Value(values, Bands, "PlainsEnd") / 100.0;
            if (plainsStart - 0.02 > 0 && plainsEnd > plainsStart)
            {
                SetCurve(config, Affinity + "plains.relativeTravelDistance", failed,
                    0, -1000, Round(plainsStart - 0.02), -100, plainsStart, 0, plainsEnd, 0, Round(plainsEnd + 0.02), -40);
            }
            else
            {
                failed.Add("PlainsStart/PlainsEnd (start must be above 2% and below the end)");
            }

            double mistlandsStart = Value(values, Bands, "MistlandsStart") / 100.0;
            if (mistlandsStart - 0.05 > 0)
            {
                SetCurve(config, Affinity + "mistlands.relativeTravelDistance", failed,
                    0, -1000, Round(mistlandsStart - 0.05), -100, mistlandsStart, 0);
            }
            else
            {
                failed.Add("MistlandsStart (must be above 5%)");
            }
        }

        public static string Summary(IDictionary<string, double> values)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "radius={0}, sea floor={1}, rivers={2}, lakes={3}, swamp {4}-{5}%, plains {6}-{7}%, mistlands from {8}%",
                Value(values, World, "WorldRadius"), Value(values, World, "SeaFloorDepth"), Value(values, Rivers, "OriginDensity"),
                Value(values, Lakes, "Budget"), Value(values, Bands, "SwampStart"), Value(values, Bands, "SwampEnd"),
                Value(values, Bands, "PlainsStart"), Value(values, Bands, "PlainsEnd"), Value(values, Bands, "MistlandsStart"));
        }

        private static double Value(IDictionary<string, double> values, string section, string key)
        {
            double value;
            if (values.TryGetValue(section + "|" + key, out value))
            {
                return value;
            }
            foreach (Setting setting in All)
            {
                if (setting.Section == section && setting.Key == key)
                {
                    return setting.Default;
                }
            }
            throw new ArgumentException("Unknown setting " + section + "|" + key);
        }

        private static double Round(double value)
        {
            return Math.Round(value, 6);
        }

        private static object ConvertValue(double value, Type fieldType)
        {
            if (fieldType == typeof(int))
            {
                return (int)Math.Round(value);
            }
            if (fieldType == typeof(float))
            {
                return (float)value;
            }
            if (fieldType == typeof(double))
            {
                return value;
            }
            return Convert.ChangeType(value, fieldType, CultureInfo.InvariantCulture);
        }

        private static void SetCurve(object config, string path, List<string> failed, params double[] xy)
        {
            Assign(config, path, failed, fieldType =>
            {
                // Riverheim's StaticArray<Rpoint2>: a struct with a count and fixed slots p0..p7.
                Type pointType = fieldType.GetGenericArguments()[0];
                object curve = Activator.CreateInstance(fieldType);
                int count = xy.Length / 2;
                fieldType.GetField("count").SetValue(curve, count);
                for (int i = 0; i < count; i++)
                {
                    fieldType.GetField("p" + i).SetValue(curve, Activator.CreateInstance(pointType, xy[2 * i], xy[2 * i + 1]));
                }
                return curve;
            });
        }

        // Walks a field path through nested structs and classes, sets the last field, and writes each
        // modified struct back into its parent so boxed value types keep the change.
        private static void Assign(object root, string path, List<string> failed, Func<Type, object> makeValue)
        {
            string[] parts = path.Split('.');
            object current = root;
            object[] parents = new object[parts.Length - 1];
            FieldInfo[] fields = new FieldInfo[parts.Length - 1];

            for (int i = 0; i < parts.Length - 1; i++)
            {
                FieldInfo field = FindField(current.GetType(), parts[i]);
                object child = field == null ? null : field.GetValue(current);
                if (child == null)
                {
                    failed.Add(path);
                    return;
                }

                parents[i] = current;
                fields[i] = field;
                current = child;
            }

            FieldInfo leaf = FindField(current.GetType(), parts[parts.Length - 1]);
            if (leaf == null)
            {
                failed.Add(path);
                return;
            }

            try
            {
                leaf.SetValue(current, makeValue(leaf.FieldType));
                for (int i = fields.Length - 1; i >= 0; i--)
                {
                    fields[i].SetValue(parents[i], current);
                    current = parents[i];
                }
            }
            catch (Exception exception)
            {
                failed.Add(path + " (" + exception.GetType().Name + ")");
            }
        }

        private static FieldInfo FindField(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
