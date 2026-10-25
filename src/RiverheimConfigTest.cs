using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace RiverheimConfig
{
    // The BepInEx plugin. The settings themselves live in RiverheimSettings.cs, which is shared with the
    // AtlasBuilder launcher's startup hook and the test tool.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class RiverheimConfigPlugin : BaseUnityPlugin
    {
        private const string PluginGuid = "com.valheim.riverheim.config";
        private const string PluginName = "Riverheim Config";
        private const string PluginVersion = "3.0.0";
        private const string RiverheimConfigMethod = "Riverheim.Configuration.ConfigManager:GetConfig";

        private static ManualLogSource log;
        private static ConfigSync configSync;
        private static bool patchApplied;
        private static readonly Dictionary<string, ConfigEntryBase> entries = new Dictionary<string, ConfigEntryBase>();
        private FileSystemWatcher watcher;

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
            WatchConfigFile();
        }

        // BepInEx only reads the config file at start-up. Watching it means a change saved in a text editor while the
        // game runs takes effect for the next world that is created or loaded, without restarting the game.
        // (Changes made in an in-game configuration manager already apply directly.)
        private void WatchConfigFile()
        {
            try
            {
                watcher = new FileSystemWatcher(Path.GetDirectoryName(Config.ConfigFilePath), Path.GetFileName(Config.ConfigFilePath));
                watcher.Changed += OnConfigFileChanged;
                watcher.Created += OnConfigFileChanged;
                watcher.Renamed += OnConfigFileChanged;
                watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception exception)
            {
                log.LogWarning("Could not watch the config file for changes; restart the game after editing it. " + exception.Message);
            }
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs args)
        {
            if (!File.Exists(Config.ConfigFilePath))
            {
                return;
            }
            try
            {
                Config.Reload();
                log.LogInfo("Config file reloaded. The new values apply to the next world you create or load.");
            }
            catch (Exception exception)
            {
                log.LogError("Could not reload the config file: " + exception.Message);
            }
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
                string text = setting.Description + " Riverheim default: " + setting.DefaultText + " (leave it there to keep Riverheim's own value).";
                if (setting.PreviewNote != null)
                {
                    text += " " + setting.PreviewNote;
                }
                if (setting.IsInteger)
                {
                    ConfigDescription description = new ConfigDescription(text, new AcceptableValueRange<int>((int)setting.Min, (int)setting.Max));
                    ConfigEntry<int> entry = Config.Bind(setting.Section, setting.Key, (int)setting.Default, description);
                    configSync.AddConfigEntry(entry).SynchronizedConfig = true;
                    entries[setting.Id] = entry;
                }
                else
                {
                    ConfigDescription description = new ConfigDescription(text, new AcceptableValueRange<double>(setting.Min, setting.Max));
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
                log.LogError("Riverheim's ConfigManager.GetConfig was not found. Riverheim Config " + PluginVersion + " is disabled; install Gurebu-Riverheim 1.2.x.");
                return;
            }

            Harmony harmony = new Harmony(PluginGuid);
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(RiverheimConfigPlugin), nameof(PatchGenerationConfig)));
            patchApplied = true;
            log.LogInfo("Riverheim configuration patch applied. Server configuration is synchronized to clients.");

            try
            {
                ExpandWorldSizeCompatibility.Apply(harmony, log);
            }
            catch (Exception exception)
            {
                log.LogError("Could not apply the Expand World Size stretch fix: " + exception);
            }
        }

        // Riverheim calls this immediately before computing its generation pipeline. The result is a boxed
        // WorldGenerationConfig, so field-path updates work without taking a compile-time dependency on its internals.
        // Settings left at their default are not written, so an untouched config generates stock Riverheim worlds.
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
            List<string> skipped = new List<string>();
            RiverheimSettings.Apply(__result, values, false, failed, skipped);
            if (skipped.Count > 0)
            {
                log.LogWarning("This Riverheim build has no field for: " + string.Join(", ", skipped.ToArray()) + ". Those settings were left out.");
            }
            if (failed.Count > 0)
            {
                log.LogError("Riverheim's configuration layout changed or a value is invalid; these settings were not applied: " + string.Join(", ", failed.ToArray()));
                return;
            }

            log.LogInfo("Applied Riverheim config: " + RiverheimSettings.Summary(values) + ".");
        }

        private void OnDestroy()
        {
            if (watcher != null)
            {
                watcher.Dispose();
                watcher = null;
            }
            if (patchApplied)
            {
                new Harmony(PluginGuid).UnpatchSelf();
                patchApplied = false;
            }
        }
    }
}
