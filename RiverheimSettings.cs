using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RiverheimConfig
{
    // Riverheim Config's settings and how they map onto Riverheim's WorldGenerationConfig.
    //
    // This file is the single definition of every setting: section, key, label, default, range,
    // description and the field path it writes. Three things compile it:
    //   - the BepInEx plugin (RiverheimConfigTest.cs), which binds each setting to the config file,
    //   - the AtlasBuilder launcher's startup hook (RiverheimConfigHook.cs in the atlasbuilder-ui repo),
    //     built with the C# 5 compiler that ships with Windows (.NET Framework 4), so no newer syntax here,
    //   - the test tool (tests/), which checks every path against a Riverheim.dll and exports settings.json
    //     for the launcher.
    // Deliberately free of BepInEx, Harmony and Unity so it can be tested against Riverheim.dll outside the game.
    //
    // A setting left at its default is not written at all: "default" means "leave Riverheim's own value alone",
    // so an untouched config always generates stock Riverheim worlds, even after Riverheim changes a default.
    // The defaults listed here are Riverheim 1.2.1's (default preset version 2) and are checked by the test tool.
    public static class RiverheimSettings
    {
        public const string World = "1. World";
        public const string Rivers = "2. Rivers";
        public const string Lakes = "3. Lakes";
        public const string Biomes = "4. Biomes";
        public const string Amounts = "5. Biome Amounts";
        public const string Bands = "6. Biome Bands";
        public const string Landmass = "7. Landmass";
        public const string Mountains = "8. Mountains";
        public const string RiverFeatures = "9. River Features";
        public const string SubBiomes = "10. Sub-biomes";
        public const string Terrain = "11. Terrain";
        public const string Poles = "12. Poles";

        public sealed class Section
        {
            public string Name;
            public string Title;
        }

        public sealed class Setting
        {
            public string Section;
            public string Key;
            public string Label;
            public double Default;
            public bool IsInteger;
            public double Min;
            public double Max;
            public double Step;
            public int Decimals;
            public string Path;
            public string Description;
            // The field only exists in newer Riverheim builds (1.1.1+). Older builds, including the Riverheim
            // inside AtlasBuilder 1.0.0, skip it instead of failing.
            public bool Optional;
            // Shown with the setting when AtlasBuilder cannot preview it.
            public string PreviewNote;

            public string Id { get { return Section + "|" + Key; } }

            public string DefaultText { get { return Default.ToString(CultureInfo.InvariantCulture); } }
        }

        // Section titles as the launcher shows them.
        public static readonly Section[] Sections =
        {
            new Section { Name = World, Title = "World (to change the world size, use World size at the bottom)" },
            new Section { Name = Rivers, Title = "Rivers" },
            new Section { Name = Lakes, Title = "Lakes" },
            new Section { Name = Biomes, Title = "Biome placement bias" },
            new Section { Name = Amounts, Title = "Biome amounts (share of contested land)" },
            new Section { Name = Bands, Title = "Biome bands (% of the way from spawn to the world edge)" },
            new Section { Name = Landmass, Title = "Landmass" },
            new Section { Name = Mountains, Title = "Mountains" },
            new Section { Name = RiverFeatures, Title = "River features" },
            new Section { Name = SubBiomes, Title = "Sub-biomes" },
            new Section { Name = Terrain, Title = "Terrain feel" },
            new Section { Name = Poles, Title = "Poles (Ashlands and Deep North belts)" }
        };

        private static Setting Number(string section, string key, string label, double value, double min, double max, double step, int decimals, string path, string description)
        {
            return new Setting { Section = section, Key = key, Label = label, Default = value, Min = min, Max = max, Step = step, Decimals = decimals, Path = path, Description = description };
        }

        private static Setting Whole(string section, string key, string label, int value, int min, int max, string path, string description)
        {
            return new Setting { Section = section, Key = key, Label = label, Default = value, IsInteger = true, Min = min, Max = max, Step = 1, Decimals = 0, Path = path, Description = description };
        }

        private static Setting Newer(Setting setting, string previewNote)
        {
            setting.Optional = true;
            setting.PreviewNote = previewNote;
            return setting;
        }

        private const string Affinity = "world.biomes.affinity.main.";
        private const string Placement = "world.biomes.competitivePlacement.main.biomes.";
        private const string NotPreviewed = "Added in Riverheim 1.1.1. AtlasBuilder 1.0.0 bundles an older Riverheim without this feature, so map previews ignore it.";

        // Defaults are Riverheim 1.2.1's own values (DefaultConfig plus the default preset's version-2 overrides).
        // Settings without a path are biome bands, applied as curves in Apply.
        public static readonly Setting[] All =
        {
            Number(World, "WorldRadius", "World radius (m)", 10500, 1000, 100000, 500, 0, "common.worldSize",
                "Radius of the world Riverheim generates, in metres. Keep 10500 and use Expand World Size stretching for a bigger or smaller world: biome bands scale with this value, but the poles, the landmass swirl and the rainfall pattern do not, so changing it distorts the layout."),
            Number(World, "TileSpacing", "Tile spacing (m)", 60, 10, 1000, 5, 0, "common.tileSpacing",
                "Distance between generation points in metres. Lower values add detail but take much longer, and river and lake counts grow with the number of points."),
            Number(World, "SeaFloorDepth", "Sea floor depth", -40, -1000, 1000, 1, 1, "common.oceanDepth",
                "Height the sea floor settles to at the world edge and in the polar trenches, also used as the floor when shaping terrain. More negative values mean deeper edges and trenches."),
            Number(World, "OceanBiomeDepth", "Ocean biome depth", -20, -1000, 1000, 1, 1, "world.biomes.conditionalPlacement.main.oceanDepth",
                "Water must be deeper than this, and far enough from the coast, to count as the Ocean biome. More negative values mean fewer waters count as Ocean."),
            Number(World, "MountainHeight", "Mountain height", 50, -1000, 1000, 1, 1, "common.mountainHeight",
                "Height Riverheim treats as mountain level when shaping terrain and scoring which high ground becomes the Mountain biome. Lower values make more mountains."),
            Number(World, "StartingAreaRadius", "Starting area (m)", 870, 0, 10000, 50, 0, "common.startingAreaRadius",
                "Size of the gentle area around spawn in metres, where hills are shaped like flatland. Larger values make a bigger gentle start."),

            Number(Rivers, "OriginDensity", "River origin density", 0.25, 0, 1, 0.05, 2, "world.rivers.origins.main.density",
                "Share of suitable coastline tiles that become river mouths. Higher values make more rivers; 0 turns rivers off."),
            Whole(Rivers, "MinLandNeighbors", "Min land neighbours", 2, 0, 8, "world.rivers.origins.main.minLandNeighbors",
                "Land tiles a coastline tile needs next to it to start a river. Higher values keep river mouths off thin peninsulas and small islands."),
            Number(Rivers, "WidthScale", "Width scale", 140, 0, 10000, 10, 1, "world.rivers.width.main.scale",
                "River width = WidthScale x flow ^ WidthPower + WidthOffset, never below zero. Scales every river's width."),
            Number(Rivers, "WidthPower", "Width power", 0.4, 0, 10, 0.05, 2, "world.rivers.width.main.power",
                "How strongly river width grows with water flow. Higher values make big rivers much wider than small streams."),
            Number(Rivers, "WidthOffset", "Width offset", -6, -1000, 1000, 1, 1, "world.rivers.width.main.offset",
                "Added to every river width. Negative values thin out small streams."),
            Whole(Rivers, "MinStrahler", "Min Strahler order", 2, 1, 20, "world.rivers.prune.main.minStrahler",
                "River branches below this Strahler order are removed. Higher values keep only the bigger branches."),
            Number(Rivers, "MinWidth", "Min river width (m)", 14.5, 0, 1000, 0.5, 1, "world.rivers.prune.main.minWidth",
                "River segments narrower than this are removed. Higher values drop thin streams."),
            Number(Rivers, "MaxWidthDifference", "Max tributary ratio", 2.2, 0, 1000, 0.5, 1, "world.rivers.prune.main.maxWidthDiff",
                "Where rivers join, a side branch is removed if the widest incoming branch is more than this many times wider. Higher values keep more small tributaries; 0 keeps them all."),
            Number(Rivers, "MeanderPeriod", "Meander period", 3.4, 0, 100, 0.1, 2, "world.rivers.meander.main.period",
                "Length of river bends, measured in river widths. Higher values make longer, gentler bends."),
            Number(Rivers, "MeanderAmplitude", "Meander amplitude", 1.35, 0, 100, 0.05, 2, "world.rivers.meander.main.riverAmplitude",
                "Strength of river bends. Higher values make rivers wind more."),

            Number(Lakes, "Budget", "Lake budget", 0.0055, 0, 1, 0.0005, 4, "world.height.lakes.main.budget",
                "Share of generation points picked as lake starting points, which then grow into lakes. Higher values make more lake area."),
            Number(Lakes, "NoiseScale", "Lake noise scale", 600, 1, 100000, 50, 0, "world.height.lakes.main.affinity.noiseScale",
                "Size of the random pattern that decides where lakes prefer to form. Larger values group lakes into bigger regions."),
            Number(Lakes, "LowlandContribution", "Lowland pull", 0.35, -100, 100, 0.05, 2, "world.height.lakes.main.affinity.kFlatland",
                "How strongly lakes prefer flat lowland."),
            Number(Lakes, "CuriosityContribution", "Terrain interest pull", 0.8, -100, 100, 0.05, 2, "world.height.lakes.main.affinity.kCuriosity",
                "How strongly lakes prefer notable spots along river systems."),

            Number(Biomes, "MeadowsBias", "Meadows bias", 0, -1000, 1000, 1, 1, Affinity + "meadows.bias",
                "Flat bonus to the Meadows score wherever biomes compete for land. Higher values let it win more land."),
            Number(Biomes, "ForestBias", "Black Forest bias", -10, -1000, 1000, 1, 1, Affinity + "forest.bias",
                "Flat bonus to the Black Forest score wherever biomes compete for land. Higher values let it win more land."),
            Number(Biomes, "SwampBias", "Swamp bias", 8, -1000, 1000, 1, 1, Affinity + "swamp.bias",
                "Flat bonus to the Swamp score wherever biomes compete for land. Higher values let it win more land."),
            Number(Biomes, "PlainsBias", "Plains bias", 5, -1000, 1000, 1, 1, Affinity + "plains.bias",
                "Flat bonus to the Plains score wherever biomes compete for land. Higher values let it win more land."),
            Number(Biomes, "MistlandsBias", "Mistlands bias", 0, -1000, 1000, 1, 1, Affinity + "mistlands.bias",
                "Flat bonus to the Mistlands score wherever biomes compete for land. Higher values let it win more land."),

            Number(Amounts, "MeadowsWeight", "Meadows amount", 69.5, 0, 100000, 10, 1, Placement + "meadows.weight",
                "How much of the contested land Meadows takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands."),
            Number(Amounts, "ForestWeight", "Black Forest amount", 258.5, 0, 100000, 10, 1, Placement + "forest.weight",
                "How much of the contested land Black Forest takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands. Riverheim 1.1.1 moved 2.5 of this to the lowland Black Forest variant."),
            Number(Amounts, "SwampWeight", "Swamp amount", 76, 0, 100000, 10, 1, Placement + "swamp.weight",
                "How much of the contested land Swamp takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands."),
            Number(Amounts, "PlainsWeight", "Plains amount", 283, 0, 100000, 10, 1, Placement + "plains.weight",
                "How much of the contested land Plains takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands."),
            Number(Amounts, "MistlandsWeight", "Mistlands amount", 305, 0, 100000, 10, 1, Placement + "mistlands.weight",
                "How much of the contested land Mistlands takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands."),
            Number(Amounts, "MarshWeight", "Marsh amount", 5.5, 0, 100000, 1, 1, Placement + "marsh.weight",
                "How much of the contested land Marsh takes. Marsh is the rare, mostly submerged sub-biome of Meadows found next to water; the game treats it as Meadows. Raising this makes more marsh patches."),
            Newer(Number(Amounts, "ForestLowlandsWeight", "Lowland Black Forest amount", 2.5, 0, 100000, 1, 1, Placement + "forestLowlands.weight",
                "How much of the contested land the lowland Black Forest variant takes. It only appears near spawn and prefers low ground close to water, to put tin within early reach. 0 turns it off."), NotPreviewed),

            Number(Bands, "MeadowsFadeEnd", "Meadows fades by (%)", 50, 1, 150, 1, 1, null,
                "Meadows becomes less likely the further you travel from spawn and reaches its full penalty at this percentage of the world radius. Lower values keep Meadows closer to spawn."),
            Number(Bands, "SwampStart", "Swamp starts (%)", 20, 3, 150, 1, 1, null,
                "Swamp can appear from this percentage of the way from spawn to the world edge (travel distance), fading in over the 2% before it."),
            Number(Bands, "SwampEnd", "Swamp ends (%)", 58, 3, 150, 1, 1, null,
                "Swamp stops appearing past this percentage of the way from spawn to the world edge, fading out over the next 2%."),
            Number(Bands, "PlainsStart", "Plains starts (%)", 29, 3, 150, 1, 1, null,
                "Plains can appear from this percentage of the way from spawn to the world edge (travel distance), fading in over the 2% before it."),
            Number(Bands, "PlainsEnd", "Plains ends (%)", 66, 3, 150, 1, 1, null,
                "Plains stops appearing past this percentage of the way from spawn to the world edge, fading out over the next 2%."),
            Number(Bands, "MistlandsStart", "Mistlands starts (%)", 60, 6, 150, 1, 1, null,
                "Mistlands can appear from this percentage of the way from spawn to the world edge (travel distance) onwards, fading in over the 5% before it."),

            Number(Landmass, "LandCohesion", "Land cohesion", 0.4, 0, 100, 0.1, 2, "world.landmass.painter.main.layerPainter.cohesionWeighting",
                "How strongly new land sticks to land already painted. Higher values gather the land into fewer, larger continents; lower values scatter it into more islands. AtlasBuilder's single-landmass Token preset uses 20."),
            Number(Landmass, "LandDistanceWeighting", "Land distance weighting", 0.5, 0, 100, 0.1, 2, "world.landmass.painter.main.layerPainter.distanceWeighting",
                "How much distance from already painted land counts when Riverheim decides where to paint more. Higher values keep landmasses more compact."),
            Whole(Landmass, "SwirlRays", "Swirl arms", 8, 0, 32, "world.landmass.restrictions.main.swirl.rays",
                "Number of arms in the spiral pattern that keeps landmasses apart and gives Riverheim worlds their swirl. 0 turns the swirl off (AtlasBuilder's Mini preset does this)."),
            Number(Landmass, "SwirlExponent", "Swirl twist", 1.4, 0, 10, 0.1, 2, "world.landmass.restrictions.main.swirl.exponent",
                "How quickly the swirl arms wind round as they go outwards. Higher values twist the arms more."),
            Number(Landmass, "SwirlStartRadius", "Swirl start radius (m)", 5800, 0, 100000, 100, 0, "world.landmass.restrictions.main.swirl.startRadius",
                "Distance from the centre, in metres, where the swirl arms begin to shape the land. Smaller values bring the swirl closer to spawn."),
            Number(Landmass, "HarborRadius", "Harbor radius (m)", 1700, 0, 100000, 100, 0, "world.landmass.restrictions.main.harbor.radius",
                "Size of the sheltered water area Riverheim keeps near the centre of the world, in metres. 0 removes it (AtlasBuilder's Mini preset does this)."),
            Number(Landmass, "HarborCircularity", "Harbor circularity", 0.045, 0, 10, 0.005, 3, "world.landmass.restrictions.main.harbor.circularity",
                "How round the central harbor area is kept. Higher values make it rounder."),
            Number(Landmass, "SmallIslandSizeThreshold", "Remove islands below (m)", 0, 0, 100000, 10, 0, "world.landmass.postprocessing.main.smallIslandSizeThreshold",
                "Landmasses smaller than about this many metres across are removed after painting. 0 keeps every island; Riverheim's own Deep North region uses 220 to clear small ones."),
            Number(Landmass, "CoastalTrimDistance", "Coastal trim distance (m)", 85, 0, 10000, 5, 0, "world.landmass.postprocessing.main.coastalTrimDistance",
                "How far the coastline is trimmed back from the painted land edge, in metres. Higher values make landmasses a bit smaller with more irregular coasts."),
            Number(Landmass, "IslandMergeThreshold", "Island merge threshold", 0.25, 0, 10, 0.05, 2, "world.landmass.partitionMerging.main.mergeThreshold",
                "How readily neighbouring land partitions merge into one landmass. Lower values merge more."),

            Number(Mountains, "MountainScoreThreshold", "Mountain biome threshold", 0.85, 0, 10, 0.05, 2, "world.biomes.conditionalPlacement.main.mountainScoreThreshold",
                "How well high ground must score as mountain terrain before it becomes the Mountain biome. Lower values turn more high ground into Mountain."),
            Number(Mountains, "MountainCutdownRatio", "Mountain cut-down ratio", 0.88, 0, 1, 0.02, 2, "world.height.settlement.main.mountainCutdownRatio",
                "Share of height taken off the mountain candidates Riverheim does not keep as mountains when it settles the terrain. Higher values flatten rejected mountains more; 0 leaves them standing."),
            Number(Mountains, "MountainMinPatchSize", "Mountain min patch size", 180, 0, 100000, 10, 0, "world.height.settlement.main.mountainMinPatchSize",
                "Mountain patches smaller than this are culled after settling. Higher values leave only the big ranges."),
            Number(Mountains, "MountainSelectionRandomness", "Mountain selection randomness", 4, 0, 1000, 0.5, 1, "world.height.mountainQuality.main.temperature",
                "Randomness in which mountain candidates are chosen. 0 always picks the best-scoring ones; higher values pick more freely."),
            Number(Mountains, "MountainAreaScore", "Mountain area score", 0.02, -10, 10, 0.005, 3, "world.height.mountainQuality.main.partitionScoring.area",
                "How much a mountain candidate's area counts in its score. Higher values favour bigger mountain regions."),
            Number(Mountains, "MountainEdgeTrim", "Mountain edge trim", 20, 0, 1000, 5, 0, "world.height.mountainEdgeProximity.main.trim",
                "How far mountain edges are trimmed back, in metres. Higher values make mountains smaller and steeper-sided."),

            Number(RiverFeatures, "ChannelWidth", "Channel width (m)", 40, 0, 10000, 5, 0, "world.rivers.channels.main.width",
                "Width of channels, the straits Riverheim cuts through large landmasses to connect distant waters. 0 turns channels off."),
            Number(RiverFeatures, "ChannelMeanderAmplitude", "Channel meander amplitude", 0.75, 0, 100, 0.05, 2, "world.rivers.meander.main.channelAmplitude",
                "Strength of bends in channels, like Meander amplitude for rivers."),
            Number(RiverFeatures, "RainfallScale", "Rainfall pattern size (m)", 1050, 1, 100000, 50, 0, "world.rivers.precipitation.main.noiseScale",
                "Size of the rainfall pattern that feeds rivers, in metres. Larger values make bigger wet and dry regions."),
            Number(RiverFeatures, "RainfallVariability", "Rainfall variability", 1.9, 0, 100, 0.1, 2, "world.rivers.precipitation.main.variability",
                "Contrast between wet and dry regions. Higher values make river sizes vary more across the world."),
            Number(RiverFeatures, "RainfallMinimum", "Rainfall minimum", 0.05, 0, 10, 0.01, 3, "world.rivers.precipitation.main.minimum",
                "Lowest rainfall any tile gets. Higher values give dry regions more water, so more small rivers survive."),
            Number(RiverFeatures, "RainfallBasinContribution", "Basin contribution", 0.7, 0, 10, 0.05, 2, "world.rivers.precipitation.main.basinContribution",
                "How much a river's drainage basin adds to its flow. Higher values make long rivers bigger."),
            Number(RiverFeatures, "SourceCuriosity", "River source features", 0.1, 0, 100, 0.05, 2, "world.rivers.curiosities.main.sourceMult",
                "How notable river sources are. Notable spots attract lakes (Terrain interest pull) and local terrain features such as crags and ponds."),
            Number(RiverFeatures, "ConfluenceCuriosity", "River confluence features", 1, 0, 100, 0.1, 2, "world.rivers.curiosities.main.proximityMult",
                "How notable places where rivers run close together are. Notable spots attract lakes and local terrain features."),
            Number(RiverFeatures, "BendCuriosity", "River bend features", 1, 0, 100, 0.1, 2, "world.rivers.curiosities.main.angleMult",
                "How notable sharp river bends are. Notable spots attract lakes and local terrain features."),

            Number(SubBiomes, "MistgladeRate", "Mistglade rate", 0.03, 0, 1, 0.01, 3, "world.biomes.replacement.main.biomes.mistglade.rate",
                "Share of Mistlands turned into Mistglade, the rare mist-free sub-biome with mellow terrain. 0 turns Mistglade off."),
            Number(SubBiomes, "MistgladePatchSize", "Mistglade patch size", 117, 0, 100000, 10, 0, "world.biomes.replacement.main.biomes.mistglade.placement.targetPatchSize",
                "Size Mistglade patches aim for, in generation tiles. Larger values make fewer, bigger glades."),
            Number(SubBiomes, "MarshBaseHeight", "Marsh base height", -0.25, -50, 50, 0.25, 2, "world.biomes.postprocessing.main.marshBaseHeight",
                "Height marsh ground is pulled towards, relative to sea level. Lower values put more of the marsh under water."),
            Number(SubBiomes, "SwampBaseHeight", "Swamp base height", -0.25, -50, 50, 0.25, 2, "world.biomes.postprocessing.main.swampBaseHeight",
                "Height swamp ground is pulled towards, relative to sea level. Lower values put more of the swamp under water."),
            Newer(Number(SubBiomes, "ForestLowlandsShapingOffset", "Lowland forest shaping offset", 2, -100, 100, 0.5, 2, "world.biomes.postprocessing.main.forestLowlandsShapingOffset",
                "Height offset used when shaping the lowland Black Forest variant's terrain."), NotPreviewed),
            Newer(Number(SubBiomes, "ForestLowlandsShapingIntensity", "Lowland forest shaping intensity", 1.25, 0, 100, 0.25, 2, "world.biomes.postprocessing.main.forestLowlandsShapingIntensity",
                "How strongly the lowland Black Forest variant's terrain is reshaped. Higher values make it stand out more from normal Black Forest."), NotPreviewed),

            Number(Terrain, "CliffIntensity", "Cliff intensity", 5, 0, 100, 0.5, 2, "rendering.renderer.cliffs.gradientMagnitude",
                "How strongly steep slopes are sharpened into cliffs. Higher values make cliffs taller and more frequent; 0 leaves slopes smooth."),
            Number(Terrain, "CliffTerraceHeight", "Cliff terrace height", 30, 0, 1000, 5, 1, "rendering.renderer.cliffs.terracing.height",
                "Height between the terraces that cliffs are broken into."),
            Number(Terrain, "TerracingStep", "Terracing step", 2, 0, 100, 0.5, 2, "rendering.renderer.shaping.terracingStep",
                "Height step of the gentle terracing applied to terrain. Larger values make more pronounced steps; 0 removes them."),
            Number(Terrain, "FlatlandThreshold", "Flatland amount", 0.275, 0, 1, 0.025, 3, "world.height.flatlands.main.noiseThreshold",
                "Threshold of the noise pattern that marks land as flat lowland. Higher values make more flatland; lower values make more rolling terrain."),
            Number(Terrain, "LandSlope", "Land slope", 1.06, 0, 100, 0.05, 2, "world.height.incline.main.land.mult",
                "Overall steepness of land rising away from water. Higher values make higher, steeper terrain inland."),
            Number(Terrain, "ErosionRate", "Erosion rate", 0.12, 0, 10, 0.02, 2, "world.height.settlement.main.hydraulicErosionRate",
                "Strength of the water erosion pass that settles the terrain. Higher values wear terrain down and smooth it more."),
            Number(Terrain, "MeadowsShapingIntensity", "Meadows shaping intensity", 0.75, 0, 100, 0.25, 2, "world.biomes.postprocessing.main.meadowsShapingIntensity",
                "How strongly Meadows terrain is reshaped after biome placement. Higher values make Meadows terrain more distinct from its surroundings."),
            Number(Terrain, "PlainsShapingIntensity", "Plains shaping intensity", 0.5, 0, 100, 0.25, 2, "world.biomes.postprocessing.main.plainsShapingIntensity",
                "How strongly Plains terrain is reshaped after biome placement. Higher values make Plains terrain more distinct from its surroundings."),

            Number(Poles, "PolarOffset", "Polar belt offset (m)", 4000, 0, 100000, 250, 0, "world.polar.offset",
                "Distance, in metres, that the centre of each polar belt (Ashlands south, Deep North north) sits beyond the world centre. Larger values push the poles further out."),
            Number(Poles, "PolarRadius", "Polar belt radius (m)", 12000, 0, 100000, 250, 0, "world.polar.radius",
                "Radius, in metres, of the circle each polar belt is cut from. Together with Polar belt offset this sets where Ashlands and Deep North begin; AtlasBuilder's HugeWorld preset rescales both."),
            Number(Poles, "PolarNoiseAmplitude", "Polar edge waviness (m)", 400, 0, 100000, 50, 0, "world.polar.noiseAmplitude",
                "How far the polar belt boundary wanders from a clean circle, in metres. 0 gives a smooth boundary."),
            Number(Poles, "PolarGapWidth", "Polar gap width (m)", 120, 0, 100000, 10, 0, "world.landmass.restrictions.main.polarGapWidth",
                "Width of the water gap kept between the main world's land and the polar belts, in metres."),
            Number(Poles, "PolarTrenchWidth", "Polar trench width (m)", 220, 0, 100000, 10, 0, "world.height.settlement.main.polarTrenchWidth",
                "Width of the deep trench along the polar belts, in metres, which drops to Sea floor depth.")
        };

        public static Setting Find(string section, string key)
        {
            foreach (Setting setting in All)
            {
                if (setting.Section == section && setting.Key == key)
                {
                    return setting;
                }
            }
            return null;
        }

        // Reads a .cfg in the BepInEx format the plugin writes and the launcher mimics. Only known settings are
        // kept, so values are keyed by Setting.Id; unknown or unreadable entries are ignored and keep the default.
        public static Dictionary<string, double> Parse(IEnumerable<string> lines)
        {
            Dictionary<string, double> values = new Dictionary<string, double>();
            string section = "";
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }
                int equals = line.IndexOf('=');
                if (equals < 0)
                {
                    continue;
                }
                Setting setting = Find(section, line.Substring(0, equals).Trim());
                double parsed;
                if (setting != null && double.TryParse(line.Substring(equals + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    values[setting.Id] = parsed;
                }
            }
            return values;
        }

        // Writes every setting to config, including those at their default value. Used by the tests.
        public static void Apply(object config, IDictionary<string, double> values, List<string> failed)
        {
            Apply(config, values, true, failed, new List<string>());
        }

        // Writes the settings that differ from their default to config (all of them when force is set). Settings whose
        // field is missing go to failed, or to skipped when the field is optional (older Riverheim builds).
        public static void Apply(object config, IDictionary<string, double> values, bool force, List<string> failed, List<string> skipped)
        {
            foreach (Setting setting in All)
            {
                if (setting.Path == null)
                {
                    continue;
                }
                double value = Value(values, setting.Section, setting.Key);
                if (!force && value == setting.Default)
                {
                    continue;
                }
                if (setting.Optional && !Exists(config, setting.Path))
                {
                    skipped.Add(setting.Key);
                    continue;
                }
                Assign(config, setting.Path, failed, delegate(Type fieldType) { return ConvertValue(value, fieldType); });
            }

            // Biome bands are Riverheim's relativeTravelDistance curves: (share of the world radius, score) points.
            // With the default percentages these rebuild exactly Riverheim's default curves.
            double meadowsFade = Value(values, Bands, "MeadowsFadeEnd") / 100.0;
            if (force || Changed(values, "MeadowsFadeEnd"))
            {
                if (meadowsFade > 0)
                {
                    SetCurve(config, Affinity + "meadows.relativeTravelDistance", failed, 0, 0, meadowsFade, -40);
                }
                else
                {
                    failed.Add("MeadowsFadeEnd (must be above 0)");
                }
            }

            double swampStart = Value(values, Bands, "SwampStart") / 100.0;
            double swampEnd = Value(values, Bands, "SwampEnd") / 100.0;
            if (force || Changed(values, "SwampStart") || Changed(values, "SwampEnd"))
            {
                if (swampStart - 0.02 > 0 && swampEnd > swampStart)
                {
                    SetCurve(config, Affinity + "swamp.relativeTravelDistance", failed,
                        0, -1000, Round(swampStart - 0.02), -100, swampStart, 0, swampEnd, 0, Round(swampEnd + 0.02), -100);
                }
                else
                {
                    failed.Add("SwampStart/SwampEnd (start must be above 2% and below the end)");
                }
            }

            double plainsStart = Value(values, Bands, "PlainsStart") / 100.0;
            double plainsEnd = Value(values, Bands, "PlainsEnd") / 100.0;
            if (force || Changed(values, "PlainsStart") || Changed(values, "PlainsEnd"))
            {
                if (plainsStart - 0.02 > 0 && plainsEnd > plainsStart)
                {
                    SetCurve(config, Affinity + "plains.relativeTravelDistance", failed,
                        0, -1000, Round(plainsStart - 0.02), -100, plainsStart, 0, plainsEnd, 0, Round(plainsEnd + 0.02), -40);
                }
                else
                {
                    failed.Add("PlainsStart/PlainsEnd (start must be above 2% and below the end)");
                }
            }

            double mistlandsStart = Value(values, Bands, "MistlandsStart") / 100.0;
            if (force || Changed(values, "MistlandsStart"))
            {
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
        }

        // Reads the value a setting's field currently holds, or null when the path does not resolve. Used by the tests
        // to compare the defaults above with Riverheim's own.
        public static object Read(object config, string path)
        {
            object current = config;
            foreach (string part in path.Split('.'))
            {
                if (current == null)
                {
                    return null;
                }
                FieldInfo field = FindField(current.GetType(), part);
                if (field == null)
                {
                    return null;
                }
                current = field.GetValue(current);
            }
            return current;
        }

        public static bool Exists(object config, string path)
        {
            object current = config;
            foreach (string part in path.Split('.'))
            {
                if (current == null)
                {
                    return false;
                }
                FieldInfo field = FindField(current.GetType(), part);
                if (field == null)
                {
                    return false;
                }
                current = field.GetValue(current);
            }
            return true;
        }

        // Lists the settings that differ from their default, as "Key=value".
        public static List<string> Changes(IDictionary<string, double> values)
        {
            List<string> changes = new List<string>();
            foreach (Setting setting in All)
            {
                double value = Value(values, setting.Section, setting.Key);
                if (value != setting.Default)
                {
                    changes.Add(setting.Key + "=" + value.ToString(CultureInfo.InvariantCulture));
                }
            }
            return changes;
        }

        public static string Summary(IDictionary<string, double> values)
        {
            List<string> changes = Changes(values);
            if (changes.Count == 0)
            {
                return "all settings at Riverheim defaults";
            }
            return changes.Count + " changed: " + string.Join(", ", changes.ToArray());
        }

        private static bool Changed(IDictionary<string, double> values, string bandKey)
        {
            return Value(values, Bands, bandKey) != Find(Bands, bandKey).Default;
        }

        private static double Value(IDictionary<string, double> values, string section, string key)
        {
            double value;
            if (values.TryGetValue(section + "|" + key, out value))
            {
                return value;
            }
            Setting setting = Find(section, key);
            if (setting == null)
            {
                throw new ArgumentException("Unknown setting " + section + "|" + key);
            }
            return setting.Default;
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
            Assign(config, path, failed, delegate(Type fieldType)
            {
                // Riverheim's StaticArray<Rpoint2>: a struct with a count and fixed slots p0..p7.
                Type pointType = fieldType.GetGenericArguments()[0];
                object curve = Activator.CreateInstance(fieldType);
                int count = xy.Length / 2;
                FindField(fieldType, "count").SetValue(curve, count);
                for (int i = 0; i < count; i++)
                {
                    FindField(fieldType, "p" + i).SetValue(curve, Activator.CreateInstance(pointType, xy[2 * i], xy[2 * i + 1]));
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
