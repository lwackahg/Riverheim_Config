# Riverheim Config

Server-synchronized world-generation controls for **Gurebu-Riverheim 1.1.x**.

Riverheim 1.1 replaced the old `DefaultConfig` API with its `ConfigManager` generation pipeline. This release applies settings at that pipeline entry point, immediately before a world is generated. It is a breaking rewrite of Riverheim Config 1.x and is not compatible with Riverheim 0.12.

## Controls

- World radius, tile spacing, ocean threshold, and mountain threshold
- River origin density and land-neighbour requirement
- River width, pruning, and meander settings
- Lake budget and terrain-affinity settings
- Competitive-placement biases for Meadows, Forest (Black Forest), Swamp, Plains, and Mistlands

Every setting is enforced and synchronized by the server through ServerSync.

## Installation

1. Install `denikson-BepInExPack_Valheim` 5.4.2202 or newer.
2. Install `Gurebu-Riverheim` 1.1.x on the server and every client.
3. Install this mod on the server and every client.
4. Start once to create `BepInEx/config/com.valheim.riverheim.config.cfg`, then edit that file on the server.

## Important

- Riverheim terrain generation must match between server and clients. Keep Riverheim and this mod at identical versions across the group.
- Changes apply whenever Riverheim generates terrain. Treat a configuration change as a new-world decision; do not change it for an established world without a tested backup.
- The 1.x config file is not migrated. Version 2 creates new, accurately named sections and leaves legacy entries harmlessly unused.
- This mod intentionally exposes only settings that map to real Riverheim 1.1 configuration fields. Removed 0.12-era controls, such as river valleys and the old per-biome distance curves, are not advertised as working.

## Troubleshooting

- Check `BepInEx/LogOutput.log` for `Riverheim 1.1 configuration patch applied` and `Applied Riverheim config`.
- If the log says `ConfigManager.GetConfig was not found`, Riverheim 1.1.x is not installed or an incompatible build is loaded.
- If it lists settings that were not applied, do not generate a production world; the upstream configuration layout has changed and this companion needs an update.

## Build

```powershell
dotnet msbuild RiverheimConfigTest.csproj /t:Rebuild /p:Configuration=Release
```

Run `./package.ps1` after a successful build to create the Thunderstore archive.
