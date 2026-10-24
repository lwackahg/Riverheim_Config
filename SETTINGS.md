# Riverheim Config settings

Generated from `RiverheimSettings.cs` by `rhconfig-test md`; do not edit by hand. Defaults are Riverheim 1.2.1's own values.
A setting left at its default is not written, so Riverheim keeps its own value for it. Section names are the headings in
`BepInEx/config/com.valheim.riverheim.config.cfg`.

## [1. World] World (to change the world size, use World size at the bottom)

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `WorldRadius` (World radius (m)) | 10500 | 1000 to 100000 | Radius of the world Riverheim generates, in metres. Keep 10500 and use Expand World Size stretching for a bigger or smaller world: biome bands scale with this value, but the poles, the landmass swirl and the rainfall pattern do not, so changing it distorts the layout. The polar belts begin 8000 m from the centre by default (see Polar belt radius) and must stay well inside this radius, or Riverheim fails to generate the world. |
| `TileSpacing` (Tile spacing (m)) | 60 | 10 to 1000 | Distance between generation points in metres. Lower values add detail but take much longer, and river and lake counts grow with the number of points. |
| `SeaFloorDepth` (Sea floor depth) | -40 | -1000 to 1000 | Height the sea floor settles to at the world edge and in the polar trenches, also used as the floor when shaping terrain. More negative values mean deeper edges and trenches. |
| `OceanBiomeDepth` (Ocean biome depth) | -20 | -1000 to 1000 | Water must be deeper than this, and far enough from the coast, to count as the Ocean biome. More negative values mean fewer waters count as Ocean. |
| `MountainHeight` (Mountain height) | 50 | -1000 to 1000 | Height Riverheim treats as mountain level when shaping terrain and scoring which high ground becomes the Mountain biome. Lower values make more mountains. |
| `StartingAreaRadius` (Starting area (m)) | 870 | 0 to 10000 | Size of the gentle area around spawn in metres, where hills are shaped like flatland. Larger values make a bigger gentle start. |

## [2. Rivers] Rivers

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `OriginDensity` (River origin density) | 0.25 | 0 to 1 | Share of suitable coastline tiles that become river mouths. Higher values make more rivers; 0 turns rivers off. |
| `MinLandNeighbors` (Min land neighbours) | 2 | 0 to 8, whole number | Land tiles a coastline tile needs next to it to start a river. Higher values keep river mouths off thin peninsulas and small islands. |
| `WidthScale` (Width scale) | 140 | 0 to 10000 | River width = WidthScale x flow ^ WidthPower + WidthOffset, never below zero. Scales every river's width. |
| `WidthPower` (Width power) | 0.4 | 0 to 10 | How strongly river width grows with water flow. Higher values make big rivers much wider than small streams. |
| `WidthOffset` (Width offset) | -6 | -1000 to 1000 | Added to every river width. Negative values thin out small streams. |
| `MinStrahler` (Min Strahler order) | 2 | 1 to 20, whole number | River branches below this Strahler order are removed. Higher values keep only the bigger branches. |
| `MinWidth` (Min river width (m)) | 14.5 | 0 to 1000 | River segments narrower than this are removed. Higher values drop thin streams. |
| `MaxWidthDifference` (Max tributary ratio) | 2.2 | 0 to 1000 | Where rivers join, a side branch is removed if the widest incoming branch is more than this many times wider. Higher values keep more small tributaries; 0 keeps them all. |
| `MeanderPeriod` (Meander period) | 3.4 | 0 to 100 | Length of river bends, measured in river widths. Higher values make longer, gentler bends. |
| `MeanderAmplitude` (Meander amplitude) | 1.35 | 0 to 100 | Strength of river bends. Higher values make rivers wind more. |

## [3. Lakes] Lakes

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `Budget` (Lake budget) | 0.0055 | 0 to 1 | Share of generation points picked as lake starting points, which then grow into lakes. Higher values make more lake area. |
| `NoiseScale` (Lake noise scale) | 600 | 1 to 100000 | Size of the random pattern that decides where lakes prefer to form. Larger values group lakes into bigger regions. |
| `LowlandContribution` (Lowland pull) | 0.35 | -100 to 100 | How strongly lakes prefer flat lowland. |
| `CuriosityContribution` (Terrain interest pull) | 0.8 | -100 to 100 | How strongly lakes prefer notable spots along river systems. |

## [4. Biomes] Biome placement bias

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `MeadowsBias` (Meadows bias) | 0 | -1000 to 1000 | Flat bonus to the Meadows score wherever biomes compete for land. Higher values let it win more land. |
| `ForestBias` (Black Forest bias) | -10 | -1000 to 1000 | Flat bonus to the Black Forest score wherever biomes compete for land. Higher values let it win more land. |
| `SwampBias` (Swamp bias) | 8 | -1000 to 1000 | Flat bonus to the Swamp score wherever biomes compete for land. Higher values let it win more land. |
| `PlainsBias` (Plains bias) | 5 | -1000 to 1000 | Flat bonus to the Plains score wherever biomes compete for land. Higher values let it win more land. |
| `MistlandsBias` (Mistlands bias) | 0 | -1000 to 1000 | Flat bonus to the Mistlands score wherever biomes compete for land. Higher values let it win more land. |

## [5. Biome Amounts] Biome amounts (share of contested land)

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `MeadowsWeight` (Meadows amount) | 69.5 | 0 to 100000 | How much of the contested land Meadows takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands. |
| `ForestWeight` (Black Forest amount) | 258.5 | 0 to 100000 | How much of the contested land Black Forest takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands. Riverheim 1.1.1 moved 2.5 of this to the lowland Black Forest variant. |
| `SwampWeight` (Swamp amount) | 76 | 0 to 100000 | How much of the contested land Swamp takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands. |
| `PlainsWeight` (Plains amount) | 283 | 0 to 100000 | How much of the contested land Plains takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands. |
| `MistlandsWeight` (Mistlands amount) | 305 | 0 to 100000 | How much of the contested land Mistlands takes. Biomes claim land in proportion to these amounts, so raising one gives that biome more land within its bands. |
| `MarshWeight` (Marsh amount) | 5.5 | 0 to 100000 | How much of the contested land Marsh takes. Marsh is the rare, mostly submerged sub-biome of Meadows found next to water; the game treats it as Meadows. Raising this makes more marsh patches. |
| `ForestLowlandsWeight` (Lowland Black Forest amount) | 2.5 | 0 to 100000 | How much of the contested land the lowland Black Forest variant takes. It only appears near spawn and prefers low ground close to water, to put tin within early reach. 0 turns it off. |

## [6. Biome Bands] Biome bands (% of the way from spawn to the world edge)

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `MeadowsFadeEnd` (Meadows fades by (%)) | 50 | 1 to 150 | Meadows becomes less likely the further you travel from spawn and reaches its full penalty at this percentage of the world radius. Lower values keep Meadows closer to spawn. |
| `SwampStart` (Swamp starts (%)) | 20 | 3 to 150 | Swamp can appear from this percentage of the way from spawn to the world edge (travel distance), fading in over the 2% before it. |
| `SwampEnd` (Swamp ends (%)) | 58 | 3 to 150 | Swamp stops appearing past this percentage of the way from spawn to the world edge, fading out over the next 2%. |
| `PlainsStart` (Plains starts (%)) | 29 | 3 to 150 | Plains can appear from this percentage of the way from spawn to the world edge (travel distance), fading in over the 2% before it. |
| `PlainsEnd` (Plains ends (%)) | 66 | 3 to 150 | Plains stops appearing past this percentage of the way from spawn to the world edge, fading out over the next 2%. |
| `MistlandsStart` (Mistlands starts (%)) | 60 | 6 to 150 | Mistlands can appear from this percentage of the way from spawn to the world edge (travel distance) onwards, fading in over the 5% before it. |

## [7. Landmass] Landmass

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `LandCohesion` (Land cohesion) | 0.4 | 0 to 100 | How strongly new land sticks to land already painted. Higher values gather the land into fewer, larger continents; lower values scatter it into more islands. AtlasBuilder's single-landmass Token preset uses 20. |
| `LandDistanceWeighting` (Land distance weighting) | 0.5 | 0 to 100 | How much distance from already painted land counts when Riverheim decides where to paint more. Higher values keep landmasses more compact. |
| `SwirlRays` (Swirl arms) | 8 | 0 to 32, whole number | Number of arms in the spiral pattern that keeps landmasses apart and gives Riverheim worlds their swirl. 0 turns the swirl off (AtlasBuilder's Mini preset does this). |
| `SwirlExponent` (Swirl twist) | 1.4 | 0 to 10 | How quickly the swirl arms wind round as they go outwards. Higher values twist the arms more. |
| `SwirlStartRadius` (Swirl start radius (m)) | 5800 | 0 to 100000 | Distance from the centre, in metres, where the swirl arms begin to shape the land. Smaller values bring the swirl closer to spawn. |
| `HarborRadius` (Harbor radius (m)) | 1700 | 0 to 100000 | Size of the sheltered water area Riverheim keeps near the centre of the world, in metres. 0 removes it (AtlasBuilder's Mini preset does this). |
| `HarborCircularity` (Harbor circularity) | 0.045 | 0 to 10 | How round the central harbor area is kept. Higher values make it rounder. |
| `SmallIslandSizeThreshold` (Remove islands below (m)) | 0 | 0 to 100000 | Landmasses smaller than about this many metres across are removed after painting. 0 keeps every island; Riverheim's own Deep North region uses 220 to clear small ones. |
| `CoastalTrimDistance` (Coastal trim distance (m)) | 85 | 0 to 10000 | How far the coastline is trimmed back from the painted land edge, in metres. Higher values make landmasses a bit smaller with more irregular coasts. |
| `IslandMergeThreshold` (Island merge threshold) | 0.25 | 0 to 10 | How readily neighbouring land partitions merge into one landmass. Lower values merge more. |

## [8. Mountains] Mountains

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `MountainScoreThreshold` (Mountain biome threshold) | 0.85 | 0 to 10 | How well high ground must score as mountain terrain before it becomes the Mountain biome. Lower values turn more high ground into Mountain. |
| `MountainCutdownRatio` (Mountain cut-down ratio) | 0.88 | 0 to 1 | Share of height taken off the mountain candidates Riverheim does not keep as mountains when it settles the terrain. Higher values flatten rejected mountains more; 0 leaves them standing. |
| `MountainMinPatchSize` (Mountain min patch size) | 180 | 0 to 100000 | Mountain patches smaller than this are culled after settling. Higher values leave only the big ranges. |
| `MountainSelectionRandomness` (Mountain selection randomness) | 4 | 0 to 1000 | Randomness in which mountain candidates are chosen. 0 always picks the best-scoring ones; higher values pick more freely. |
| `MountainAreaScore` (Mountain area score) | 0.02 | -10 to 10 | How much a mountain candidate's area counts in its score. Higher values favour bigger mountain regions. |
| `MountainEdgeTrim` (Mountain edge trim) | 20 | 0 to 1000 | How far mountain edges are trimmed back, in metres. Higher values make mountains smaller and steeper-sided. |

## [9. River Features] River features

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `ChannelWidth` (Channel width (m)) | 40 | 0 to 10000 | Width of channels, the straits Riverheim cuts through large landmasses to connect distant waters. 0 turns channels off. |
| `ChannelMeanderAmplitude` (Channel meander amplitude) | 0.75 | 0 to 100 | Strength of bends in channels, like Meander amplitude for rivers. |
| `RainfallScale` (Rainfall pattern size (m)) | 1050 | 1 to 100000 | Size of the rainfall pattern that feeds rivers, in metres. Larger values make bigger wet and dry regions. |
| `RainfallVariability` (Rainfall variability) | 1.9 | 0 to 100 | Contrast between wet and dry regions. Higher values make river sizes vary more across the world. |
| `RainfallMinimum` (Rainfall minimum) | 0.05 | 0.001 to 10 | Lowest rainfall any tile gets. Higher values give dry regions more water, so more small rivers survive. Must stay above 0: at exactly 0 Riverheim fails to generate the world. |
| `RainfallBasinContribution` (Basin contribution) | 0.7 | 0 to 10 | How much a river's drainage basin adds to its flow. Higher values make long rivers bigger. |
| `SourceCuriosity` (River source features) | 0.1 | 0 to 100 | How notable river sources are. Notable spots attract lakes (Terrain interest pull) and local terrain features such as crags and ponds. |
| `ConfluenceCuriosity` (River confluence features) | 1 | 0 to 100 | How notable places where rivers run close together are. Notable spots attract lakes and local terrain features. |
| `BendCuriosity` (River bend features) | 1 | 0 to 100 | How notable sharp river bends are. Notable spots attract lakes and local terrain features. |

## [10. Sub-biomes] Sub-biomes

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `MistgladeRate` (Mistglade rate) | 0.03 | 0 to 1 | Share of Mistlands turned into Mistglade, the rare mist-free sub-biome with mellow terrain. 0 turns Mistglade off. |
| `MistgladePatchSize` (Mistglade patch size) | 117 | 0 to 100000 | Size Mistglade patches aim for, in generation tiles. Larger values make fewer, bigger glades. |
| `MarshBaseHeight` (Marsh base height) | -0.25 | -50 to 50 | Height marsh ground is pulled towards, relative to sea level. Lower values put more of the marsh under water. |
| `SwampBaseHeight` (Swamp base height) | -0.25 | -50 to 50 | Height swamp ground is pulled towards, relative to sea level. Lower values put more of the swamp under water. |
| `ForestLowlandsShapingOffset` (Lowland forest shaping offset) | 2 | -100 to 100 | Height offset used when shaping the lowland Black Forest variant's terrain. |
| `ForestLowlandsShapingIntensity` (Lowland forest shaping intensity) | 1.25 | 0 to 100 | How strongly the lowland Black Forest variant's terrain is reshaped. Higher values make it stand out more from normal Black Forest. |

## [11. Terrain] Terrain feel

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `CliffIntensity` (Cliff intensity) | 5 | 0 to 100 | How strongly steep slopes are sharpened into cliffs. Higher values make cliffs taller and more frequent; 0 leaves slopes smooth. |
| `CliffTerraceHeight` (Cliff terrace height) | 30 | 0 to 1000 | Height between the terraces that cliffs are broken into. |
| `TerracingStep` (Terracing step) | 2 | 0 to 100 | Height step of the gentle terracing applied to terrain. Larger values make more pronounced steps; 0 removes them. |
| `FlatlandThreshold` (Flatland amount) | 0.275 | 0 to 1 | Threshold of the noise pattern that marks land as flat lowland. Higher values make more flatland; lower values make more rolling terrain. |
| `LandSlope` (Land slope) | 1.06 | 0 to 100 | Overall steepness of land rising away from water. Higher values make higher, steeper terrain inland. |
| `ErosionRate` (Erosion rate) | 0.12 | 0 to 10 | Strength of the water erosion pass that settles the terrain. Higher values wear terrain down and smooth it more. |
| `MeadowsShapingIntensity` (Meadows shaping intensity) | 0.75 | 0 to 100 | How strongly Meadows terrain is reshaped after biome placement. Higher values make Meadows terrain more distinct from its surroundings. |
| `PlainsShapingIntensity` (Plains shaping intensity) | 0.5 | 0 to 100 | How strongly Plains terrain is reshaped after biome placement. Higher values make Plains terrain more distinct from its surroundings. |

## [12. Poles] Poles (Ashlands and Deep North belts)

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `PolarOffset` (Polar belt offset (m)) | 4000 | 0 to 100000 | Offset, in metres, of the circle each polar belt (Ashlands south, Deep North north) is cut from. The belts begin Polar belt radius minus this offset from the world centre, 8000 m by default, so larger values pull the poles inwards. Keep that distance at least 1000 m inside the world radius: a belt that only just reaches the world makes Riverheim fail to generate it. Moving it 1000 m or more beyond the world radius (0 with the default radius) removes both belts. |
| `PolarRadius` (Polar belt radius (m)) | 12000 | 0 to 100000 | Radius, in metres, of the circle each polar belt is cut from. The belts begin Polar belt radius minus Polar belt offset from the world centre, 8000 m by default, so larger values push Ashlands and Deep North further out. Keep that distance at least 1000 m inside the world radius: a belt that only just reaches the world makes Riverheim fail to generate it, and 1000 m or more beyond the world radius removes both belts. AtlasBuilder's HugeWorld preset rescales both. |
| `PolarNoiseAmplitude` (Polar edge waviness (m)) | 400 | 0 to 100000 | How far the polar belt boundary wanders from a clean circle, in metres. 0 gives a smooth boundary. |
| `PolarGapWidth` (Polar gap width (m)) | 120 | 0 to 100000 | Width of the water gap kept between the main world's land and the polar belts, in metres. |
| `PolarTrenchWidth` (Polar trench width (m)) | 220 | 0 to 100000 | Width of the deep trench along the polar belts, in metres, which drops to Sea floor depth. |
