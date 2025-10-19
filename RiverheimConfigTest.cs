using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using System;
using System.Collections;
using UnityEngine;

namespace RiverheimConfig
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class RiverheimConfigPlugin : BaseUnityPlugin
    {
        private const string PluginGUID = "com.valheim.riverheim.config";
        private const string PluginName = "Riverheim Config";
        private const string PluginVersion = "1.0.2";

        private static ManualLogSource logger;
        private static RiverheimConfigPlugin instance;

        // === RIVER SPAWNING ===
        private static ConfigEntry<float> riverDensity;
        private static ConfigEntry<float> riverStartingVariability;
        private static ConfigEntry<float> riverWeightRecovery;
        private static ConfigEntry<int> minRiverMouthLandNeighbors;
        private static ConfigEntry<float> minRiverMouthPointerMagnitude;

        // === RIVER SIZE ===
        private static ConfigEntry<float> riverWidthScale;
        private static ConfigEntry<float> riverWidthPower;
        private static ConfigEntry<float> riverWidthOffset;
        
        // === RIVER FILTERING (what gets kept) ===
        private static ConfigEntry<int> minRiverStrahler;
        private static ConfigEntry<float> minRiverWidth;
        private static ConfigEntry<float> maxCatchmentDiff;

        // === RIVER APPEARANCE ===
        private static ConfigEntry<float> riverMeanderAmplitude;
        private static ConfigEntry<float> riverMeanderPeriod;
        private static ConfigEntry<float> riverMinDepth;
        private static ConfigEntry<float> riverSteepness;
        private static ConfigEntry<float> riverMaxBankHeight;

        // === RIVER VALLEYS ===
        private static ConfigEntry<float> riverValleyOffset;
        private static ConfigEntry<float> riverValleyMagnitude;
        private static ConfigEntry<float> riverValleyExponent;

        // === LAKES ===
        private static ConfigEntry<float> lakeThreshold;
        private static ConfigEntry<float> lakeNoiseScale;
        private static ConfigEntry<float> lakeLowlandContribution;
        private static ConfigEntry<float> lakeCuriosityContribution;
        // === WORLD SIZE ===
        private static ConfigEntry<float> worldSize;
        private static ConfigEntry<float> tileSpacing;

        private static ConfigSync configSync;

        // Helper method to bind and sync config entries
        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);
            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;
            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) 
            => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        void Awake()
        {
            try
            {
                instance = this;
                logger = Logger;
                logger.LogWarning("=== RIVERHEIM RIVERS & LAKES CONFIG ===");

                // Initialize ServerSync for config synchronization
                configSync = new ConfigSync(PluginGUID) 
                { 
                    DisplayName = PluginName, 
                    CurrentVersion = PluginVersion,
                    MinimumRequiredVersion = "1.0.2"
                };
                logger.LogInfo("ServerSync initialized - clients will use server config");

            // ========================================
            // RIVER SPAWNING - Controls how many rivers spawn
            // ========================================
            riverDensity = config("1. River Spawning", "Density", 0.25f,
                new ConfigDescription(
                    "How many river mouth spawn points\n" +
                    "0.25 = DEFAULT (vanilla Riverheim)\n" +
                    "0.4 = ~570 rivers (60% more)\n" +
                    "0.5 = ~710 rivers (100% more)\n" +
                    "0.6 = ~850 rivers (140% more)\n" +
                    "Limits: True 0..1 | Practical 0.15..0.7 | Default 0.25",
                    new AcceptableValueRange<float>(0.15f, 0.7f)));

            riverStartingVariability = config("1. River Spawning", "StartingVariability", 0.9f,
                new ConfigDescription(
                    "Randomness in river spawn locations\n" +
                    "0.9 = DEFAULT\n" +
                    "Limits: True 0..1 | Practical 0.4..1.0 | Default 0.9",
                    new AcceptableValueRange<float>(0.4f, 1.5f)));

            riverWeightRecovery = config("1. River Spawning", "WeightRecovery", 0.05f,
                new ConfigDescription(
                    "How quickly river density recovers after spawning\n" +
                    "0.05 = DEFAULT\n" +
                    "Limits: True 0..1 | Practical 0.02..0.1 | Default 0.05",
                    new AcceptableValueRange<float>(0.02f, 0.1f)));

            // Minimum land neighbors at river mouth (inlet stability)
            minRiverMouthLandNeighbors = config("1. River Spawning", "MinLandNeighbors", 2,
                new ConfigDescription(
                    "Min land tiles adjacent to river mouth\n" +
                    "2 = DEFAULT (stable rivers)\n" +
                    "1 = More rivers possible (may look weird)",
                    new AcceptableValueRange<int>(1, 3)));

            minRiverMouthPointerMagnitude = config("1. River Spawning", "MinPointerMagnitude", 0.8f,
                new ConfigDescription(
                    "Minimum flow direction strength for river mouths\n" +
                    "0.8 = DEFAULT (strong flow requirement)\n" +
                    "0.6 = More mouths allowed (weaker flow)\n" +
                    "0.4 = MANY more mouths (may create stagnant rivers)\n" +
                    "Lower = more rivers but worse quality\n" +
                    "Limits: True any | Practical 0..1 | Default 0.8",
                    new AcceptableValueRange<float>(0.3f, 1.0f)));

            // ========================================
            // RIVER SIZE - Makes rivers wider/longer
            // ========================================
            riverWidthScale = config("2. River Size", "WidthScale", 140f,
                new ConfigDescription(
                    "Base river width multiplier\n" +
                    "140 = DEFAULT (vanilla Riverheim)\n" +
                    "180 = 30% wider\n" +
                    "220 = 60% wider\n" +
                    "280 = 100% wider (Amazon-like)\n" +
                    "Limits: True any | Practical 100..350 | Default 140",
                    new AcceptableValueRange<float>(100f, 350f)));

            riverWidthPower = config("2. River Size", "WidthPower", 0.4f,
                new ConfigDescription(
                    "How width scales with river depth/catchment\n" +
                    "0.4 = DEFAULT\n" +
                    "Limits: True any | Practical 0.25..0.65 | Default 0.4",
                    new AcceptableValueRange<float>(0.25f, 0.65f)));

            riverWidthOffset = config("2. River Size", "WidthOffset", -5f,
                new ConfigDescription(
                    "Width adjustment offset\n" +
                    "-5 = DEFAULT\n" +
                    "Limits: True any | Practical -15..5 | Default -5",
                    new AcceptableValueRange<float>(-15f, 5f)));

            // ========================================
            // RIVER FILTERING - What rivers get removed
            // ========================================
            minRiverStrahler = config("3. River Filtering", "MinStrahler", 2,
                new ConfigDescription(
                    "Minimum Strahler order to keep\n" +
                    "2 = DEFAULT (no tiny tributaries)\n" +
                    "1 = Keep ALL streams (500+ rivers)\n" +
                    "Limits: True any | Practical 1..5 | Default 2",
                    new AcceptableValueRange<int>(1, 5)));

            minRiverWidth = config("3. River Filtering", "MinWidth", 12.5f,
                new ConfigDescription(
                    "Minimum river width to keep (meters)\n" +
                    "12.5 = DEFAULT\n" +
                    "8 = Keep smaller streams\n" +
                    "Limits: True any | Practical 4..30 | Default 12.5",
                    new AcceptableValueRange<float>(4f, 30f)));

            maxCatchmentDiff = config("3. River Filtering", "MaxCatchmentDiff", 5.5f,
                new ConfigDescription(
                    "Maximum drainage area difference\n" +
                    "5.5 = DEFAULT\n" +
                    "Limits: True any | Practical 3..10 | Default 5.5",
                    new AcceptableValueRange<float>(3f, 10f)));

            // ========================================
            // RIVER APPEARANCE - Visual characteristics
            // ========================================
            riverMeanderAmplitude = config("4. River Appearance", "MeanderAmplitude", 0.55f,
                new ConfigDescription(
                    "How much rivers curve and wind\n" +
                    "0.55 = DEFAULT\n" +
                    "Limits: True any | Practical 0..1.2 | Default 0.55",
                    new AcceptableValueRange<float>(0f, 1.2f)));

            riverMeanderPeriod = config("4. River Appearance", "MeanderPeriod", 2.4f,
                new ConfigDescription(
                    "Frequency of river curves\n" +
                    "2.4 = DEFAULT\n" +
                    "Limits: True >0 | Practical 1..5 | Default 2.4",
                    new AcceptableValueRange<float>(1f, 5f)));

            riverMinDepth = config("4. River Appearance", "MinDepth", 2.5f,
                new ConfigDescription(
                    "Minimum river depth (visual)\n" +
                    "2.5 = DEFAULT\n" +
                    "Limits: True >=0 | Practical 1..5 | Default 2.5",
                    new AcceptableValueRange<float>(1f, 5f)));

            riverSteepness = config("4. River Appearance", "BankSteepness", 0.25f,
                new ConfigDescription(
                    "How steep river banks are\n" +
                    "0.25 = DEFAULT\n" +
                    "Limits: True >0 | Practical 0.1..0.6 | Default 0.25",
                    new AcceptableValueRange<float>(0.1f, 0.6f)));

            riverMaxBankHeight = config("4. River Appearance", "MaxBankHeight", 70f,
                new ConfigDescription(
                    "Maximum height of river banks\n" +
                    "70 = DEFAULT\n" +
                    "Limits: True >0 | Practical 30..120 | Default 70",
                    new AcceptableValueRange<float>(30f, 120f)));

            // ========================================
            // RIVER VALLEYS - Terrain around rivers
            // ========================================
            riverValleyOffset = config("5. River Valleys", "ValleyOffset", -160f,
                new ConfigDescription(
                    "Base valley depth\n" +
                    "-160 = Default\n" +
                    "-220 = Deeper valleys (more dramatic)\n" +
                    "-100 = Shallower valleys (subtle)\n" +
                    "Limits: True any | Practical -300..-50 | Default -160",
                    new AcceptableValueRange<float>(-300f, -50f)));

            riverValleyMagnitude = config("5. River Valleys", "ValleyMagnitude", 13f,
                new ConfigDescription(
                    "Valley depth multiplier\n" +
                    "13 = Default\n" +
                    "18 = More pronounced valleys\n" +
                    "8 = Subtle valleys\n" +
                    "Limits: True >0 | Practical 6..25 | Default 13",
                    new AcceptableValueRange<float>(6f, 25f)));

            riverValleyExponent = config("5. River Valleys", "ValleyExponent", 0.5f,
                new ConfigDescription(
                    "Valley shape curve\n" +
                    "0.5 = Default (natural V-shape)\n" +
                    "0.7 = Wider valleys\n" +
                    "0.3 = Narrower, sharper valleys\n" +
                    "Limits: True >0 | Practical 0.3..0.8 | Default 0.5",
                    new AcceptableValueRange<float>(0.3f, 0.8f)));

            // ========================================
            // LAKES
            // ========================================
            lakeThreshold = config("6. Lakes", "SpawnThreshold", 0.9f,
                new ConfigDescription(
                    "Lake spawn probability threshold\n" +
                    "0.9 = DEFAULT (vanilla Riverheim)\n" +
                    "0.85 = More lakes\n" +
                    "0.8 = Many lakes\n" +
                    "Limits: True any | Practical 0.7..0.98 | Default 0.9",
                    new AcceptableValueRange<float>(0.7f, 0.98f)));

            lakeNoiseScale = config("6. Lakes", "NoiseScale", 600f,
                new ConfigDescription(
                    "Lake distribution pattern scale\n" +
                    "600 = Default\n" +
                    "400 = Smaller, more clustered lakes\n" +
                    "900 = Larger, more spread out lakes\n" +
                    "Limits: True >0 | Practical 300..1200 | Default 600",
                    new AcceptableValueRange<float>(300f, 1200f)));

            lakeLowlandContribution = config("6. Lakes", "LowlandContribution", 0.35f,
                new ConfigDescription(
                    "How much lowlands favor lakes\n" +
                    "0.35 = DEFAULT\n" +
                    "Limits: True any | Practical 0..0.8 | Default 0.35",
                    new AcceptableValueRange<float>(0f, 0.8f)));

            lakeCuriosityContribution = config("6. Lakes", "CuriosityContribution", 0.8f,
                new ConfigDescription(
                    "Lakes near interesting terrain features\n" +
                    "0.8 = Default\n" +
                    "1.0 = More lakes near mountains/rivers\n" +
                    "0.5 = Random lake placement\n" +
                    "Limits: True any | Practical 0.3..1.2 | Default 0.8",
                    new AcceptableValueRange<float>(0.3f, 1.2f)));

            // ========================================
            // WORLD SIZE
            // ========================================
            worldSize = config("7. World Size", "WorldRadius", 10500f,
                new ConfigDescription(
                    "World radius in meters (affects coastline = max rivers)\n" +
                    "10500 = DEFAULT (21km diameter, ~3,310 river capacity)\n" +
                    "12000 = 14% larger (24km diameter, ~3,800 river capacity)\n" +
                    "13500 = 29% larger (27km diameter, ~4,300 river capacity)\n" +
                    "15000 = 43% larger (30km diameter, ~4,800 river capacity)\n" +
                    "WARNING: Larger = MUCH slower world generation!\n" +
                    "WARNING: Conflicts with ExpandWorldSize mod!\n" +
                    "Limits: True >0 | Practical 8000..18000 | Default 10500",
                    new AcceptableValueRange<float>(8000f, 18000f)));

            tileSpacing = config("7. World Size", "TileSpacing", 70f,
                new ConfigDescription(
                    "Distance between generation points\n" +
                    "70 = DEFAULT\n" +
                    "60 = More detail (SLOWER gen, more memory)\n" +
                    "80 = Less detail (faster, less memory)\n" +
                    "WARNING: <60 can cause crashes on large worlds!\n" +
                    "Limits: True >0 | Practical 50..100 | Default 70",
                    new AcceptableValueRange<float>(50f, 100f)));

            Config.Save();
            
            logger.LogWarning("Config created - Current values:");
            logger.LogInfo($"  River Density: {riverDensity.Value}");
            logger.LogInfo($"  River Width: {riverWidthScale.Value}");
            logger.LogInfo($"  Min Strahler: {minRiverStrahler.Value}");
            logger.LogInfo($"  Lake Threshold: {lakeThreshold.Value}");
            logger.LogWarning("⚠ SERVER CONFIG WILL BE ENFORCED ON CLIENTS");
            logger.LogWarning("⚠ Create a NEW world to apply changes!");

            StartCoroutine(DelayedPatch());
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to initialize Riverheim Config: {ex}");
                throw;
            }
        }

        IEnumerator DelayedPatch()
        {
            yield return new WaitForSeconds(2f);

            try
            {
                logger.LogInfo("Applying patches...");

                var harmony = new Harmony(PluginGUID);

                // Patch WorldStateGenerator config (prefix to reload config, postfix to apply overrides)
                var configMethod = AccessTools.Method("Riverheim.DefaultConfig:Config");
                if (configMethod != null)
                {
                    harmony.Patch(
                        configMethod,
                        prefix: new HarmonyMethod(typeof(RiverheimConfigPlugin), nameof(ReloadConfigPrefix)),
                        postfix: new HarmonyMethod(typeof(RiverheimConfigPlugin), nameof(PatchWorldConfig))
                    );
                    logger.LogInfo("✓ Patched WorldStateGenerator config (with reload prefix)");
                }

                // Patch WorldRenderer config
                var rendererMethod = AccessTools.Method("Riverheim.DefaultConfig:RendererConfig");
                if (rendererMethod != null)
                {
                    harmony.Patch(rendererMethod, postfix: new HarmonyMethod(typeof(RiverheimConfigPlugin), nameof(PatchRendererConfig)));
                    logger.LogInfo("✓ Patched WorldRenderer config");
                }

                logger.LogWarning("✓✓✓ ALL PATCHES APPLIED ✓✓✓");
            }
            catch (Exception ex)
            {
                logger.LogError($"Patch failed: {ex}");
            }
        }

        // Runs BEFORE Riverheim reads its default config; forces fresh values from disk
        static void ReloadConfigPrefix()
        {
            try
            {
                instance.Config.Reload();
                logger.LogWarning($"⟳ Config reloaded - Density: {riverDensity.Value}, WidthScale: {riverWidthScale.Value}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Config reload failed: {ex}");
            }
        }


        static void PatchWorldConfig(ref object __result)
        {
            try
            {
                logger.LogWarning(">>> PATCHING RIVER & LAKE CONFIG <<<");

                var configType = __result.GetType();

                // === COMMON CONFIG (WORLD SIZE) ===
                var commonField = AccessTools.Field(configType, "common");
                if (commonField != null)
                {
                    var commonConfig = commonField.GetValue(__result);
                    var commonConfigType = commonConfig.GetType();

                    var sizeField = AccessTools.Field(commonConfigType, "WorldSize");
                    if (sizeField != null)
                    {
                        var oldValue = sizeField.GetValue(commonConfig);
                        sizeField.SetValue(commonConfig, worldSize.Value);
                        logger.LogWarning($"  WorldSize: {oldValue} → {worldSize.Value}");
                    }

                    var spacingField = AccessTools.Field(commonConfigType, "TileSpacing");
                    if (spacingField != null)
                    {
                        var oldValue = spacingField.GetValue(commonConfig);
                        spacingField.SetValue(commonConfig, tileSpacing.Value);
                        logger.LogWarning($"  TileSpacing: {oldValue} → {tileSpacing.Value}");
                    }

                    commonField.SetValue(__result, commonConfig);
                    logger.LogInfo($"✓ World: size={worldSize.Value}m, spacing={tileSpacing.Value}m");
                }
                else
                {
                    logger.LogError("✗ common field not found");
                }

                // === RIVERS CONFIG ===
                var riversField = AccessTools.Field(configType, "rivers");
                if (riversField != null)
                {
                    var riverConfig = riversField.GetValue(__result);
                    var riverConfigType = riverConfig.GetType();

                    // Spawning
                    AccessTools.Field(riverConfigType, "startingDensity")?.SetValue(riverConfig, riverDensity.Value);
                    AccessTools.Field(riverConfigType, "startingVariability")?.SetValue(riverConfig, riverStartingVariability.Value);
                    AccessTools.Field(riverConfigType, "weightRecovery")?.SetValue(riverConfig, riverWeightRecovery.Value);

                    // River mouth requirements (directly on RiverConfig)
                    var landNeighborsField = AccessTools.Field(riverConfigType, "minRiverMouthLandNeighbors");
                    if (landNeighborsField != null)
                    {
                        var oldLand = landNeighborsField.GetValue(riverConfig);
                        landNeighborsField.SetValue(riverConfig, minRiverMouthLandNeighbors.Value);
                        logger.LogWarning($"  minRiverMouthLandNeighbors: {oldLand} → {minRiverMouthLandNeighbors.Value}");
                    }
                    else
                    {
                        logger.LogError("  ✗ minRiverMouthLandNeighbors field not found in RiverConfig");
                    }

                    var pointerField = AccessTools.Field(riverConfigType, "minRiverMouthPointerMagnitude");
                    if (pointerField != null)
                    {
                        var oldPtr = pointerField.GetValue(riverConfig);
                        pointerField.SetValue(riverConfig, minRiverMouthPointerMagnitude.Value);
                        logger.LogWarning($"  minRiverMouthPointerMagnitude: {oldPtr} → {minRiverMouthPointerMagnitude.Value}");
                    }
                    else
                    {
                        logger.LogError("  ✗ minRiverMouthPointerMagnitude field not found in RiverConfig");
                    }

                    // Width
                    var widthField = AccessTools.Field(riverConfigType, "width");
                    if (widthField != null)
                    {
                        var widthConfig = widthField.GetValue(riverConfig);
                        AccessTools.Field(widthConfig.GetType(), "scale")?.SetValue(widthConfig, riverWidthScale.Value);
                        AccessTools.Field(widthConfig.GetType(), "power")?.SetValue(widthConfig, riverWidthPower.Value);
                        AccessTools.Field(widthConfig.GetType(), "offset")?.SetValue(widthConfig, riverWidthOffset.Value);
                    }

                    // Pruning
                    var pruneField = AccessTools.Field(riverConfigType, "prune");
                    if (pruneField != null)
                    {
                        var pruneConfig = pruneField.GetValue(riverConfig);
                        AccessTools.Field(pruneConfig.GetType(), "minStrahler")?.SetValue(pruneConfig, minRiverStrahler.Value);
                        AccessTools.Field(pruneConfig.GetType(), "minWidth")?.SetValue(pruneConfig, minRiverWidth.Value);
                        AccessTools.Field(pruneConfig.GetType(), "maxCatchmentDiff")?.SetValue(pruneConfig, maxCatchmentDiff.Value);
                    }

                    riversField.SetValue(__result, riverConfig);
                    logger.LogInfo($"✓ Rivers: density={riverDensity.Value}, width={riverWidthScale.Value}, minStrahler={minRiverStrahler.Value}");
                }

                // === RIVER VALLEYS CONFIG ===
                var valleysField = AccessTools.Field(configType, "riverValleys");
                if (valleysField != null)
                {
                    var valleyConfig = valleysField.GetValue(__result);
                    AccessTools.Field(valleyConfig.GetType(), "offset")?.SetValue(valleyConfig, riverValleyOffset.Value);
                    AccessTools.Field(valleyConfig.GetType(), "magnitude")?.SetValue(valleyConfig, riverValleyMagnitude.Value);
                    AccessTools.Field(valleyConfig.GetType(), "exponent")?.SetValue(valleyConfig, riverValleyExponent.Value);
                    valleysField.SetValue(__result, valleyConfig);
                    logger.LogInfo($"✓ Valleys: offset={riverValleyOffset.Value}, magnitude={riverValleyMagnitude.Value}");
                }

                // === LAKES CONFIG ===
                var lakesField = AccessTools.Field(configType, "lakes");
                if (lakesField != null)
                {
                    var lakeConfig = lakesField.GetValue(__result);
                    AccessTools.Field(lakeConfig.GetType(), "threshold")?.SetValue(lakeConfig, lakeThreshold.Value);
                    AccessTools.Field(lakeConfig.GetType(), "noiseScale")?.SetValue(lakeConfig, lakeNoiseScale.Value);
                    AccessTools.Field(lakeConfig.GetType(), "lowlandContribution")?.SetValue(lakeConfig, lakeLowlandContribution.Value);
                    AccessTools.Field(lakeConfig.GetType(), "curiosityContribution")?.SetValue(lakeConfig, lakeCuriosityContribution.Value);
                    lakesField.SetValue(__result, lakeConfig);
                    logger.LogInfo($"✓ Lakes: threshold={lakeThreshold.Value}, lowland={lakeLowlandContribution.Value}");
                }

                logger.LogWarning("✓✓✓ WORLD CONFIG APPLIED ✓✓✓");
            }
            catch (Exception ex)
            {
                logger.LogError($"PatchWorldConfig error: {ex}");
            }
        }

        static void PatchRendererConfig(ref object __result)
        {
            try
            {
                logger.LogWarning(">>> PATCHING RIVER RENDERER <<<");

                var configType = __result.GetType();

                // === RIVER RENDERER CONFIG ===
                var riversField = AccessTools.Field(configType, "rivers");
                if (riversField != null)
                {
                    var riverConfig = riversField.GetValue(__result);
                    var riverConfigType = riverConfig.GetType();

                    // Meander
                    AccessTools.Field(riverConfigType, "meanderAmplitude")?.SetValue(riverConfig, riverMeanderAmplitude.Value);
                    AccessTools.Field(riverConfigType, "meanderPeriod")?.SetValue(riverConfig, riverMeanderPeriod.Value);

                    // Profile
                    var profileField = AccessTools.Field(riverConfigType, "profile");
                    if (profileField != null)
                    {
                        var profileConfig = profileField.GetValue(riverConfig);
                        AccessTools.Field(profileConfig.GetType(), "minDepth")?.SetValue(profileConfig, riverMinDepth.Value);
                        AccessTools.Field(profileConfig.GetType(), "steepness")?.SetValue(profileConfig, riverSteepness.Value);
                        AccessTools.Field(profileConfig.GetType(), "maxBankHeight")?.SetValue(profileConfig, riverMaxBankHeight.Value);
                    }

                    riversField.SetValue(__result, riverConfig);
                    logger.LogInfo($"✓ Renderer: meander={riverMeanderAmplitude.Value}, depth={riverMinDepth.Value}, banks={riverMaxBankHeight.Value}");
                }

                logger.LogWarning("✓✓✓ RENDERER CONFIG APPLIED ✓✓✓");
            }
            catch (Exception ex)
            {
                logger.LogError($"PatchRendererConfig error: {ex}");
            }
        }

    }
}