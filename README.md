# Riverheim Config

> WARNING: BETA — HIGH RISK. BACK UP YOUR WORLDS AND SAVES.
> - Experimental build. Functionality is not guaranteed.
> - May cause unintended consequences, including corrupted worlds or saves.
> - Back up existing worlds and characters before installing or changing settings.
> - Recommended: Test on a fresh world first. Do not use on critical servers without backups.
> - Use at your own risk. Report findings on the Riverheim Discord.

Server-enforced configuration for the [Riverheim Rivers & Lakes](https://thunderstore.io/c/valheim/p/Riverheim_Dev/Riverheim/) mod. Allows server admins to control river and lake generation with automatic client synchronization.

## Features

- Server-enforced config using ServerSync
- 26+ parameters across spawning, size, filtering, appearance, valleys, lakes, world size
- Harmony patches to override Riverheim defaults
- Version sync and descriptive logging

## Installation (Thunderstore)

- Install BepInExPack for Valheim
- Install Riverheim "Rivers & Lakes"
- Install this mod from Thunderstore (DLL is placed under `BepInEx/plugins/` in the package)

## Configuration

Edit: `BepInEx/config/com.valheim.riverheim.config.cfg`

When joining a server, server values override client values in memory (client file remains on disk).

## Important Notes

- Create a NEW world after changing config values
- Extreme values may cause performance issues or crashes
- Use at your own risk; back up worlds/saves

## Build

```powershell
dotnet msbuild RiverheimConfigTest.csproj /t:Rebuild /p:Configuration=Release
```

Output: `bin/Release/RiverheimConfigTest.dll`

## Packaging

Run:
```powershell
.\package.ps1
```
This produces `Riverheim_Config-<version>.zip` with `BepInEx/plugins/RiverheimConfigTest.dll` inside.

## Quick Start

1. Install BepInEx and Riverheim "Rivers & Lakes".
2. Install this mod from Thunderstore or manually copy the DLL to `BepInEx/plugins/`.
3. Edit `BepInEx/config/com.valheim.riverheim.config.cfg` on the server.
4. Start a NEW world to test changes.

## Install via Mod Manager

- Use the Thunderstore page Install button or protocol:

```
thunderstore://install/valheim/Wackah/Riverheim_Config/1.0.2
```

## Manual Install

1. Download the Thunderstore zip.
2. Extract the `BepInEx/` folder into your Valheim directory, merging folders.
3. Verify `BepInEx/plugins/RiverheimConfigTest.dll` exists.

## Troubleshooting

- **Two config files appear**: Ensure only this mod (GUID `com.valheim.riverheim.config`) is installed; remove any old DLLs using `com.valheim.riverheim.rivers`.
- **Changes not applying**: Restart the game/server; create a NEW world; check logs for "ALL PATCHES APPLIED".
- **Manager can’t find version**: Wait a few minutes after publish and refresh mod list; use protocol install.

## Logging

- BepInEx logs path: `BepInEx/LogOutput.log`.
- Look for prefixes `[Riverheim Config]` during startup indicating patches and applied values.

## Known Limitations

- Experimental; values outside practical ranges may hurt performance or stability.
- World generation is deterministic per world seed; existing worlds may not reflect config changes made after creation.

## Support / Feedback

- Riverheim Discord (preferred for quick feedback).
- Thunderstore mod page discussions.
- GitHub repository: https://github.com/lwackahg/Riverheim_Config

## Roadmap

- Safer defaults and validation for extreme values.
- Optional reduced logging mode once stable.
- Preset profiles (e.g., lake-heavy, mega-rivers) in separate config files.

## Credits

- ServerSync: blaxxun-boop
- Riverheim: Riverheim_Dev

## License

Use at your own risk. No warranty. See repository for details if a license is later added.
