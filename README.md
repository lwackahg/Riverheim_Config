# Riverheim Config

Server-synchronized world-generation controls for [Gurebu's Riverheim](https://thunderstore.io/c/valheim/p/Gurebu/Riverheim/) 1.2.x.

Riverheim itself has no player-facing options. This companion mod adds a config file for the parts of Riverheim's world generation that are safe to turn: world size and depths, rivers, lakes, biome placement and amounts, biome bands, landmass shape, mountains, river features, sub-biomes and terrain feel. Settings are applied at Riverheim's `ConfigManager.GetConfig` entry point, immediately before a world is generated.

**Preview before you commit:** every setting here is the same setting, with the same name and default, as in the [Riverheim AtlasBuilder UI](https://github.com/lwackahg/atlasbuilder-ui-repo), a launcher for Gurebu's [Riverheim AtlasBuilder](https://thunderstore.io/c/valheim/p/Gurebu/Riverheim_AtlasBuilder/). Draw maps of your seed with your settings, then **Export config** writes the config file for your server.

## Controls

Every default is Riverheim 1.2.1's own value, and **a setting left at its default is not written at all**, so an untouched config generates stock Riverheim worlds even if a Riverheim update changes a default. The full list with defaults, ranges and descriptions is in [SETTINGS.md](https://github.com/lwackahg/Riverheim_Config/blob/main/SETTINGS.md); the config file carries the same descriptions.

- **1. World**: world radius, tile spacing, sea floor depth, ocean biome depth, mountain height, starting area radius
- **2. Rivers**: origin density and land-neighbour requirement, width formula, pruning, meanders
- **3. Lakes**: budget and terrain-affinity settings
- **4. Biomes**: placement bias for Meadows, Black Forest, Swamp, Plains and Mistlands
- **5. Biome Amounts**: how much contested land each biome takes, now including Marsh and the lowland Black Forest variant from Riverheim 1.1.1
- **6. Biome Bands**: how far from spawn Meadows fades out, where Swamp and Plains start and end, and where Mistlands starts (percent of the way to the world edge)
- **7. Landmass**: how land clumps into continents or scatters into islands, the swirl, the central harbor, small-island removal, coastal trimming
- **8. Mountains**: how much high ground becomes Mountain, how rejected mountains are cut down, minimum mountain size, selection randomness
- **9. River Features**: channels through landmasses, the rainfall pattern that feeds rivers, how notable river sources, confluences and bends are
- **10. Sub-biomes**: Mistglade rate and patch size, marsh and swamp base height, lowland Black Forest shaping
- **11. Terrain**: cliffs, terracing, flatland amount, land slope, erosion, Meadows and Plains shaping

Every setting is enforced and synchronized by the server through ServerSync.

**Bigger or smaller worlds:** keep `WorldRadius` at 10500 and use Expand World Size stretching (this mod fixes Riverheim's forests, vegetation and forest-bound locations in stretched worlds, which were missing beyond about 10 km; tested in game with Expand World Size 1.45.0 at stretch 1.5) (Stretch world = Stretch biomes, and EWS World radius + World edge size = 10500 x stretch). Changing `WorldRadius` scales the biome bands but not the poles, the landmass swirl or the rainfall pattern, so it distorts the layout. One thing no mod setting can change: Valheim's terrain and vegetation shaders draw snow on everything more than about 8,000 m north of the centre, whatever the stretch, so in a stretched world the normal land between there and the Deep North looks snow-covered. It is only the look: biomes, weather, vegetation and locations are correct.

## Installation

1. Install `denikson-BepInExPack_Valheim` 5.4.2202 or newer.
2. Install [Riverheim](https://thunderstore.io/c/valheim/p/Gurebu/Riverheim/) (Gurebu-Riverheim) 1.2.x on the server and every client.
3. Install this mod on the server and every client.
4. Start once to create `BepInEx/config/com.valheim.riverheim.config.cfg`, then edit that file on the server, or write it with the [AtlasBuilder UI](https://github.com/lwackahg/atlasbuilder-ui-repo)'s **Export config**.

**Changing settings without restarting:** the mod notices when the config file is saved, on a server or a client, and uses the new values for the next world that is created or loaded. A world that is already running keeps its terrain until it is loaded again. In single player you can also edit every setting from inside the game with a configuration manager (the F1 menu, for example *Azu's UnOfficial ConfigManager*); it shows each setting's description and allowed range. On a server, the server's file wins: it is sent to every player who joins.

## Recommended presets

Ready-made settings in the `presets` folder of the [repository](https://github.com/lwackahg/Riverheim_Config/tree/main/presets). Copy one over `BepInEx/config/com.valheim.riverheim.config.cfg`, or pick it from the AtlasBuilder UI's **Import config** menu to see your seed first. Every one was generated on several seeds with Riverheim 1.2.1 and keeps all progression biomes.

**Gentle**, close to normal Riverheim with one clear theme:

| Preset | What you get |
| --- | --- |
| [Pangaea](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/Pangaea.cfg) | one huge supercontinent, long overland journeys |
| [Archipelago](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/Archipelago.cfg) | scattered islands, a sailing world |
| [RiverKingdom](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/RiverKingdom.cfg) | many wide, winding rivers and channels |
| [Lakelands](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/Lakelands.cfg) | lakes everywhere, in lake districts |
| [Highlands](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/Highlands.cfg) | big mountain ranges and steeper land |
| [GentleLowlands](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/GentleLowlands.cfg) | broad flat lowlands and a big gentle start |
| [LongJourney](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/LongJourney.cfg) | biome bands pushed outwards, a slow and gentle start |
| [ShortJourney](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/ShortJourney.cfg) | biome bands pulled in, harder biomes come early |

**Wild**, far from stock Riverheim:

| Preset | What you get |
| --- | --- |
| [GreatRivers](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/wild/GreatRivers.cfg) | a supercontinent cut by a few huge, looping Amazon-style rivers |
| [InlandSea](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/wild/InlandSea.cfg) | a supercontinent around a vast central sea, laced with channels |
| [GrandHarbor](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/wild/GrandHarbor.cfg) | a huge round sea at the centre, land wrapped around it |
| [Atoll](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/wild/Atoll.cfg) | one central island in open sea with scattered islets |
| [Canyonlands](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/wild/Canyonlands.cfg) | swamp-heavy land slashed by wide channels |
| [TheAlps](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/wild/TheAlps.cfg) | about a third of all land is Mountain |
| [MeadowEmpire](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/wild/MeadowEmpire.cfg) | Meadows cover a third of the land |
| [Stardust](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/wild/Stardust.cfg) | countless thin spiral arms of small islands |
| [BiomeChaos](https://github.com/lwackahg/Riverheim_Config/blob/main/presets/wild/BiomeChaos.cfg) | every biome anywhere, even next to spawn (hard start) |

`presets/Defaults.cfg` lists every setting at Riverheim's own value.

## Important

- Riverheim terrain generation must match between server and clients. Keep Riverheim and this mod at identical versions across the group.
- Changes apply whenever Riverheim generates terrain, to every world. Treat a configuration change as a new-world decision; do not change it for an established world without a tested backup. (Riverheim stores only its preset name and version in a world, not these values, so the mod cannot keep per-world settings yet. See `docs/preset-handler-notes.md` in the repository.)
- Coming from 1.0.4, the previous release: it was made for Riverheim 0.12, whose settings no longer exist, so 3.0 writes a fresh set of sections and the old 1.0.x entries do nothing. Start from `presets/Defaults.cfg` or one of the presets below.
- AtlasBuilder 1.0.0 bundles a Riverheim from before 1.1.1. The AtlasBuilder UI's **Installed** Riverheim choice draws with your own Riverheim instead, so its previews match the game, including the lowland Black Forest settings (`ForestLowlandsWeight`, `ForestLowlandsShaping*`); with its **Bundled** choice those settings do nothing.
- This mod only exposes settings that map to real Riverheim 1.2 configuration fields, and each one was checked to visibly change a map of a test seed drawn by Riverheim 1.2.1 itself (run inside AtlasBuilder). Descriptions say what the field does as far as it is understood; the AtlasBuilder UI is the way to see what a value does to your seed.
- Some values make Riverheim throw an error instead of generating a world, so the mod refuses them: `RainfallMinimum` at exactly 0 (the lowest allowed value is 0.001), `MinWidth` below 2 (at 0 Riverheim runs out of memory; the lowest allowed value is 3), and a `WorldRadius` from 7800 to 9000 m, where the world edge runs into the Ashlands and Deep North belts (the mod keeps Riverheim's own radius and logs why). The descriptions say so. What the game shows when that happens was not tested; AtlasBuilder aborts.

## Troubleshooting

- Check `BepInEx/LogOutput.log` for `Riverheim configuration patch applied` and `Applied Riverheim config`.
- If the log says `ConfigManager.GetConfig was not found`, Riverheim 1.2.x is not installed or an incompatible build is loaded.
- If it lists settings that were not applied, do not generate a production world; the upstream configuration layout has changed and this companion needs an update.
- A warning `This Riverheim build has no field for: ...` means an older Riverheim is installed; those settings are left out.

## Build and test

```powershell
dotnet msbuild src\RiverheimConfigTest.csproj -t:Rebuild -p:Configuration=Release
dotnet build tests\RiverheimConfig.Tests -c Release
dotnet tests\RiverheimConfig.Tests\bin\Release\net8.0\rhconfig-test.dll check Gurebu-Riverheim\Riverheim.dll
.\tests\Run-AtlasTests.ps1
```

`tests/README.md` describes the checks: every field path against the real `Riverheim.dll`, and AtlasBuilder renders proving that stock values reproduce `--preset Default` byte for byte and that every setting changes the map. The renders run the installed Riverheim 1.2.1 inside AtlasBuilder (which needs a Valheim install with BepInEx; `-Engine Bundled` uses AtlasBuilder's own older Riverheim instead). Run `./package.ps1` after a successful build to create the Thunderstore archive.
