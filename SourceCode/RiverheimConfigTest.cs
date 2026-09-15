using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using System;
using System.Collections;
using System.Collections.Generic;
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

        private static ConfigEntry<double> worldRadius;
        private static ConfigEntry<double> tileSpacing;
        private static ConfigEntry<double> oceanDepth;
        private static ConfigEntry<double> mountainHeight;

        private static ConfigEntry<double> riverDensity;
        private static ConfigEntry<int> minLandNeighbors;
        private static ConfigEntry<double> riverWidthScale;
        private static ConfigEntry<double> riverWidthPower;
        private static ConfigEntry<double> riverWidthOffset;
        private static ConfigEntry<int> minRiverStrahler;
        private static ConfigEntry<double> minRiverWidth;
        private static ConfigEntry<double> maxRiverWidthDifference;
        private static ConfigEntry<double> riverMeanderPeriod;
        private static ConfigEntry<double> riverMeanderAmplitude;

        private static ConfigEntry<double> lakeBudget;
        private static ConfigEntry<double> lakeNoiseScale;
        private static ConfigEntry<double> lakeLowlandContribution;
        private static ConfigEntry<double> lakeCuriosityContribution;

        private static ConfigEntry<double> meadowsBias;
        private static ConfigEntry<double> forestBias;
        private static ConfigEntry<double> swampBias;
        private static ConfigEntry<double> plainsBias;
        private static ConfigEntry<double> mistlandsBias;

        private ConfigEntry<T> Bind<T>(string section, string key, T value, string description, bool sync = true)
        {
            ConfigEntry<T> entry = Config.Bind(section, key, value, new ConfigDescription(description));
            SyncedConfigEntry<T> syncedEntry = configSync.AddConfigEntry(entry);
            syncedEntry.SynchronizedConfig = sync;
            return entry;
        }

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
            worldRadius = Bind("1. World", "WorldRadius", 10500d,
                "World radius in metres. Riverheim 1.1 default: 10500. Changes require a newly generated world.");
            tileSpacing = Bind("1. World", "TileSpacing", 60d,
                "Distance between generation points in metres. Riverheim 1.1 default: 60. Lower values increase generation cost.");
            oceanDepth = Bind("1. World", "OceanDepth", -20d,
                "Biome ocean threshold. More negative values produce more ocean. Riverheim 1.1 default: -20.");
            mountainHeight = Bind("1. World", "MountainHeight", 50d,
                "Mountain threshold. Lower values produce more mountains. Riverheim 1.1 default: 50.");

            riverDensity = Bind("2. Rivers", "OriginDensity", 0.25d,
                "Fraction of suitable coastal tiles selected as river origins. Riverheim 1.1 default: 0.25.");
            minLandNeighbors = Bind("2. Rivers", "MinLandNeighbors", 2,
                "Minimum land neighbours required for a river origin. Riverheim 1.1 default: 2.");
            riverWidthScale = Bind("2. Rivers", "WidthScale", 140d,
                "Base river width multiplier. Riverheim 1.1 default: 140.");
            riverWidthPower = Bind("2. Rivers", "WidthPower", 0.4d,
                "How width scales with discharge. Riverheim 1.1 default: 0.4.");
            riverWidthOffset = Bind("2. Rivers", "WidthOffset", -5d,
                "River width offset. Riverheim 1.1 default: -5.");
            minRiverStrahler = Bind("2. Rivers", "MinStrahler", 2,
                "Minimum Strahler order retained after pruning. Riverheim 1.1 default: 2.");
            minRiverWidth = Bind("2. Rivers", "MinWidth", 12.5d,
                "Minimum retained river width in metres. Riverheim 1.1 default: 12.5.");
            maxRiverWidthDifference = Bind("2. Rivers", "MaxWidthDifference", 5.5d,
                "Maximum permitted width difference while pruning. Riverheim 1.1 default: 5.5.");
            riverMeanderPeriod = Bind("2. Rivers", "MeanderPeriod", 3.4d,
                "River meander period. Riverheim 1.1 default: 3.4.");
            riverMeanderAmplitude = Bind("2. Rivers", "MeanderAmplitude", 1.35d,
                "River meander amplitude. Riverheim 1.1 default preset: 1.35.");

            lakeBudget = Bind("3. Lakes", "Budget", 0.0055d,
                "Lake generation budget for the main region. Riverheim 1.1 default: 0.0055.");
            lakeNoiseScale = Bind("3. Lakes", "NoiseScale", 600d,
                "Lake affinity noise scale. Riverheim 1.1 default: 600.");
            lakeLowlandContribution = Bind("3. Lakes", "LowlandContribution", 0.35d,
                "How strongly lowlands favour lakes. Riverheim 1.1 default: 0.35.");
            lakeCuriosityContribution = Bind("3. Lakes", "CuriosityContribution", 0.8d,
                "How strongly interesting terrain favours lakes. Riverheim 1.1 default: 0.8.");

            meadowsBias = Bind("4. Biomes", "MeadowsBias", 0d,
                "Competitive placement bias for Meadows. Riverheim 1.1 default: 0.");
            forestBias = Bind("4. Biomes", "ForestBias", -10d,
                "Competitive placement bias for Black Forest (named Forest internally). Riverheim 1.1 default: -10.");
            swampBias = Bind("4. Biomes", "SwampBias", 8d,
                "Competitive placement bias for Swamp. Riverheim 1.1 default: 8.");
            plainsBias = Bind("4. Biomes", "PlainsBias", 5d,
                "Competitive placement bias for Plains. Riverheim 1.1 default: 5.");
            mistlandsBias = Bind("4. Biomes", "MistlandsBias", 0d,
                "Competitive placement bias for Mistlands. Riverheim 1.1 default: 0.");
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

            List<string> failed = new List<string>();
            Set(__result, "common.worldSize", worldRadius.Value, failed);
            Set(__result, "common.tileSpacing", tileSpacing.Value, failed);
            Set(__result, "common.oceanDepth", oceanDepth.Value, failed);
            Set(__result, "common.mountainHeight", mountainHeight.Value, failed);
            Set(__result, "world.biomes.conditionalPlacement.main.oceanDepth", oceanDepth.Value, failed);

            Set(__result, "world.rivers.origins.main.density", riverDensity.Value, failed);
            Set(__result, "world.rivers.origins.main.minLandNeighbors", minLandNeighbors.Value, failed);
            Set(__result, "world.rivers.width.main.scale", riverWidthScale.Value, failed);
            Set(__result, "world.rivers.width.main.power", riverWidthPower.Value, failed);
            Set(__result, "world.rivers.width.main.offset", riverWidthOffset.Value, failed);
            Set(__result, "world.rivers.prune.main.minStrahler", minRiverStrahler.Value, failed);
            Set(__result, "world.rivers.prune.main.minWidth", minRiverWidth.Value, failed);
            Set(__result, "world.rivers.prune.main.maxWidthDiff", maxRiverWidthDifference.Value, failed);
            Set(__result, "world.rivers.meander.main.period", riverMeanderPeriod.Value, failed);
            Set(__result, "world.rivers.meander.main.riverAmplitude", riverMeanderAmplitude.Value, failed);

            Set(__result, "world.height.lakes.main.budget", lakeBudget.Value, failed);
            Set(__result, "world.height.lakes.main.affinity.noiseScale", lakeNoiseScale.Value, failed);
            Set(__result, "world.height.lakes.main.affinity.kFlatland", lakeLowlandContribution.Value, failed);
            Set(__result, "world.height.lakes.main.affinity.kCuriosity", lakeCuriosityContribution.Value, failed);

            Set(__result, "world.biomes.affinity.main.meadows.bias", meadowsBias.Value, failed);
            Set(__result, "world.biomes.affinity.main.forest.bias", forestBias.Value, failed);
            Set(__result, "world.biomes.affinity.main.swamp.bias", swampBias.Value, failed);
            Set(__result, "world.biomes.affinity.main.plains.bias", plainsBias.Value, failed);
            Set(__result, "world.biomes.affinity.main.mistlands.bias", mistlandsBias.Value, failed);

            if (failed.Count > 0)
            {
                log.LogError("Riverheim 1.1 configuration layout changed; the following settings were not applied: " + string.Join(", ", failed.ToArray()));
                return;
            }

            log.LogInfo("Applied Riverheim config: radius=" + worldRadius.Value + ", rivers=" + riverDensity.Value + ", lakes=" + lakeBudget.Value + ", swamp bias=" + swampBias.Value + ".");
        }

        private static void Set(object root, string path, object value, List<string> failed)
        {
            string[] parts = path.Split('.');
            object current = root;
            object[] parents = new object[parts.Length - 1];
            FieldInfo[] fields = new FieldInfo[parts.Length - 1];

            for (int i = 0; i < parts.Length - 1; i++)
            {
                FieldInfo field = AccessTools.Field(current.GetType(), parts[i]);
                if (field == null)
                {
                    failed.Add(path);
                    return;
                }

                object child = field.GetValue(current);
                if (child == null)
                {
                    failed.Add(path);
                    return;
                }

                parents[i] = current;
                fields[i] = field;
                current = child;
            }

            FieldInfo leaf = AccessTools.Field(current.GetType(), parts[parts.Length - 1]);
            if (leaf == null)
            {
                failed.Add(path);
                return;
            }

            try
            {
                leaf.SetValue(current, ConvertTo(value, leaf.FieldType));
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

        private static object ConvertTo(object value, Type type)
        {
            Type target = Nullable.GetUnderlyingType(type) ?? type;
            if (target.IsInstanceOfType(value))
            {
                return value;
            }
            return Convert.ChangeType(value, target);
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
}
