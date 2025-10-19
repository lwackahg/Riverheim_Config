# Riverheim Config

> WARNING: BETA — HIGH RISK. BACK UP YOUR WORLDS AND SAVES.
> - Experimental build. Functionality is not guaranteed.
> - May cause unintended consequences, including corrupted worlds or saves.
> - Back up existing worlds and characters before installing or changing settings.
> - Recommended: Test on a fresh world first. Do not use on critical servers without backups.
> - Use at your own risk. Report findings on the Riverheim Discord.

Server-enforced configuration mod for [Riverheim Rivers & Lakes](https://thunderstore.io/c/valheim/p/Riverheim_Dev/Riverheim/). Allows server administrators to customize river and lake generation with automatic client synchronization.

## Features

- **Server-Enforced Config**: All clients automatically use the server's configuration
- **26 Configurable Parameters**: Control every aspect of river and lake generation
- **Version Checking**: Ensures all players have compatible mod versions
- **No Client Setup Required**: Clients just need the mod installed - config syncs automatically

## Configuration Categories

### 1. River Spawning
- **Density**: Control how many rivers spawn (0.25 = default, 0.7 = ~2000 rivers)
- **Starting Variability**: Randomness in river spawn locations
- **Weight Recovery**: How quickly river density recovers after spawning
- **Min Land Neighbors**: Minimum land tiles adjacent to river mouths
- **Min Pointer Magnitude**: Minimum flow direction strength

### 2. River Size
- **Width Scale**: Base river width multiplier (140 = default, 280 = Amazon-like)
- **Width Power**: How width scales with river depth
- **Width Offset**: Width adjustment offset

### 3. River Filtering
- **Min Strahler**: Minimum river order to keep (2 = default, 1 = keep all streams)
- **Min Width**: Minimum river width in meters
- **Max Catchment Diff**: Maximum drainage area difference

### 4. River Appearance
- **Meander Amplitude**: How much rivers curve and wind
- **Meander Period**: Frequency of river curves
- **Min Depth**: Minimum river depth (visual)
- **Bank Steepness**: How steep river banks are
- **Max Bank Height**: Maximum height of river banks

### 5. River Valleys
- **Valley Offset**: Base valley depth (-160 = default)
- **Valley Magnitude**: Valley depth multiplier
- **Valley Exponent**: Valley shape curve

### 6. Lakes
- **Spawn Threshold**: Lake spawn probability (0.9 = default, 0.8 = many lakes)
- **Noise Scale**: Lake distribution pattern scale
- **Lowland Contribution**: How much lowlands favor lakes
- **Curiosity Contribution**: Lakes near interesting terrain features

### 7. World Size
- **World Radius**: World radius in meters (10500 = default 21km diameter)
- **Tile Spacing**: Distance between generation points (70 = default)

## Installation

### Server
1. Install BepInEx
2. Install Riverheim Rivers & Lakes
3. Install this mod
4. Configure settings in `BepInEx/config/com.valheim.riverheim.config.cfg`
5. Start server

### Client
1. Install BepInEx
2. Install Riverheim Rivers & Lakes
3. Install this mod
4. Connect to server - config will sync automatically!

## How It Works

When a client connects to a server:
1. ServerSync sends all configuration values from server to client
2. Client temporarily uses server's values (local config file unchanged)
3. World generation uses server settings
4. Both server and client generate identical terrain

## Important Notes

- **Create a NEW world** after changing config values
- All players must have both Riverheim and this mod installed
- Config values have "True Limits" (hard-coded) and "Practical Ranges" (recommended)
- Extreme values may cause performance issues or crashes

## Example Presets

### Cross-Continent Rivers
```ini
Density = 0.7
WidthScale = 280
MinStrahler = 3
```

### Lake-Heavy World
```ini
LakeSpawnThreshold = 0.8
LakeLowlandContribution = 0.6
```

### Minimal Rivers
```ini
Density = 0.15
MinStrahler = 4
MinWidth = 25
```

## Compatibility

- **Requires**: Riverheim Rivers & Lakes 0.12.0+
- **Compatible with**: Most Valheim mods
- **Conflicts with**: ExpandWorldSize (if you modify WorldRadius)

## Support

Please report feedback and issues on the Riverheim Discord (preferred) or the Thunderstore mod page. Include logs and your config file when possible.

## Credits

- **ServerSync**: [blaxxun-boop](https://github.com/blaxxun-boop/ServerSync)
- **Riverheim**: [Riverheim_Dev](https://thunderstore.io/c/valheim/p/Riverheim_Dev/Riverheim/)

## Changelog

See [CHANGELOG.md](CHANGELOG.md)
