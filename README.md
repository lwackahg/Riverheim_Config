# Riverheim Config

Server-synchronized world-generation controls for **Gurebu-Riverheim 1.2.x**.

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

**Bigger or smaller worlds:** keep `WorldRadius` at 10500 and use Expand World Size stretching (this mod fixes Riverheim's forests, vegetation and forest-bound locations in stretched worlds, which were missing beyond about 10 km) (Stretch world = Stretch biomes, and EWS World radius + World edge size = 10500 x stretch). Changing `WorldRadius` scales the biome bands but not the poles, the landmass swirl or the rainfall pattern, so it distorts the layout. One thing no mod setting can change: Valheim's terrain and vegetation shaders draw snow on everything more than about 8,000 m north of the centre, whatever the stretch, so in a stretched world the normal land between there and the Deep North looks snow-covered. It is only the look: biomes, weather, vegetation and locations are correct.

## Installation

1. Install `denikson-BepInExPack_Valheim` 5.4.2202 or newer.
2. Install `Gurebu-Riverheim` 1.2.x on the server and every client.
3. Install this mod on the server and every client.
4. Start once to create `BepInEx/config/com.valheim.riverheim.config.cfg`, then edit that file on the server, or write it with the AtlasBuilder UI's **Export config**.

**Changing settings without restarting:** the mod notices when the config file is saved, on a server or a client, and uses the new values for the next world that is created or loaded. A world that is already running keeps its terrain until it is loaded again. In single player you can also edit every setting from inside the game with a configuration manager (the F1 menu, for example *Azu's UnOfficial ConfigManager*); it shows each setting's description and allowed range. On a server, the server's file wins: it is sent to every player who joins.

## Important

- Riverheim terrain generation must match between server and clients. Keep Riverheim and this mod at identical versions across the group.
- Changes apply whenever Riverheim generates terrain, to every world. Treat a configuration change as a new-world decision; do not change it for an established world without a tested backup. (Riverheim stores only its preset name and version in a world, not these values, so the mod cannot keep per-world settings yet. See `docs/preset-handler-notes.md` in the repository.)
- Config files from Riverheim Config 2.0.0 keep working; the new sections are added with their defaults. `ForestWeight`'s default moved from 261 to 258.5 because Riverheim 1.1.1 gave 2.5 of it to the new lowland Black Forest variant; a 2.0.0 file that still says 261 will now force 261.
- AtlasBuilder 1.0.0 bundles a Riverheim from before 1.1.1. The AtlasBuilder UI's **Installed** Riverheim choice draws with your own Riverheim instead, so its previews match the game, including the lowland Black Forest settings (`ForestLowlandsWeight`, `ForestLowlandsShaping*`); with its **Bundled** choice those settings do nothing.
- This mod only exposes settings that map to real Riverheim 1.2 configuration fields, and each one was checked to visibly change a map of a test seed drawn by Riverheim 1.2.1 itself (run inside AtlasBuilder). Descriptions say what the field does as far as it is understood; the AtlasBuilder UI is the way to see what a value does to your seed.
- `RainfallMinimum` at exactly 0 makes Riverheim throw an error instead of generating a world, so the lowest allowed value is 0.001. The descriptions say so. What the game shows when that happens was not tested; AtlasBuilder aborts.

## Troubleshooting

- Check `BepInEx/LogOutput.log` for `Riverheim configuration patch applied` and `Applied Riverheim config`.
- If the log says `ConfigManager.GetConfig was not found`, Riverheim 1.2.x is not installed or an incompatible build is loaded.
- If it lists settings that were not applied, do not generate a production world; the upstream configuration layout has changed and this companion needs an update.
- A warning `This Riverheim build has no field for: ...` means an older Riverheim is installed; those settings are left out.

## Build and test

```powershell
dotnet msbuild RiverheimConfigTest.csproj /t:Rebuild /p:Configuration=Release
dotnet build tests\RiverheimConfig.Tests -c Release
dotnet tests\RiverheimConfig.Tests\bin\Release\net8.0\rhconfig-test.dll check Gurebu-Riverheim\Riverheim.dll
.\tests\Run-AtlasTests.ps1
```

`tests/README.md` describes the checks: every field path against the real `Riverheim.dll`, and AtlasBuilder renders proving that stock values reproduce `--preset Default` byte for byte and that every setting changes the map. The renders run the installed Riverheim 1.2.1 inside AtlasBuilder (which needs a Valheim install with BepInEx; `-Engine Bundled` uses AtlasBuilder's own older Riverheim instead). Run `./package.ps1` after a successful build to create the Thunderstore archive.
