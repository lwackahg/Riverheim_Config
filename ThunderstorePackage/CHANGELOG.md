# Riverheim Config - Changelog

## [3.0.0] - unreleased

### For Gurebu-Riverheim 1.2.x, with many more controls

- Dependency moved to `Gurebu-Riverheim-1.2.1`. Riverheim 1.1.0 to 1.2.1 kept every field this mod writes; 1.1.1's lowland Black Forest variant is now covered too.
- **Defaults now mean "leave Riverheim's value alone."** A setting at its default is not written, so an untouched config generates stock Riverheim worlds even after Riverheim changes a default, and Riverheim's per-version defaults (which it pins in each world since 1.2.0) stay in control.
- `ForestWeight` default corrected from 261 to 258.5 (Riverheim 1.1.1 moved 2.5 to the lowland variant). New `MarshWeight` (5.5) and `ForestLowlandsWeight` (2.5) in `5. Biome Amounts`.
- New sections: `7. Landmass` (land cohesion and distance weighting, swirl arms/twist/start radius, harbor radius and circularity, small-island removal, coastal trim, island merge threshold), `8. Mountains` (biome threshold, cut-down ratio, minimum patch size, selection randomness, area score, edge trim), `9. River Features` (channel width and meander, rainfall pattern size/variability/minimum/basin contribution, source/confluence/bend features), `10. Sub-biomes` (Mistglade rate and patch size, marsh and swamp base height, lowland Black Forest shaping), `11. Terrain` (cliff intensity and terrace height, terracing step, flatland amount, land slope, erosion rate, Meadows and Plains shaping intensity), `12. Poles` (polar belt offset, radius and edge waviness, polar gap and trench width).
- Every setting now has a range (BepInEx clamps values outside it) and a plain-language description; the full table is in `SETTINGS.md`.
- Fixed: config changes while the game or server ran threw `MissingFieldException: ZRoutedRpc.Everybody` and were not sent to connected players (the bundled ServerSync was built for an older Valheim). ServerSync is updated to v1.20 and now compiled into `RiverheimConfigTest.dll`, so the package no longer ships `ServerSync.dll` and another mod's outdated copy can no longer take its place. If you installed by hand, delete the old `ServerSync.dll` this mod put in `BepInEx/plugins`.
- The config file is watched: saving it while the game or server runs reloads it, and the new values apply to the next world created or loaded. Together with the ranges, this makes in-game configuration managers (F1) a convenient way to try settings in single player.
- The settings table (`RiverheimSettings.cs`) is shared verbatim with the Riverheim AtlasBuilder UI launcher, which compiles it into its AtlasBuilder hook and builds its fields from the exported `settings.json`. Mod and launcher can no longer drift apart.
- New `tests/`: a DLL check that every path resolves and every default equals Riverheim's own value, and an AtlasBuilder harness that runs the installed Riverheim 1.2.1 inside AtlasBuilder and proves that stock values render byte-identical to `--preset Default`, that each of the 82 settings visibly changes the map, and that `presets/BEST.cfg` still renders as before.
- Settings for the lowland Black Forest variant are skipped with a warning on Riverheim builds that lack the field (including the Riverheim bundled in AtlasBuilder 1.0.0, so the AtlasBuilder UI's previews ignore them for now).
- Two values make Riverheim throw instead of generating a world: `RainfallMinimum` at exactly 0 (its minimum is now 0.001) and polar belts that only just reach into the world (`PolarRadius - PolarOffset` within about 1000 m of `WorldRadius`). The descriptions warn about both; `PolarOffset` and `PolarRadius` now step by 100 m.

### Migration

- 2.0.0 config files keep working. New sections are added with their defaults the first time the game starts.
- A 2.0.0 file that still contains `ForestWeight = 261` now forces 261 instead of leaving Riverheim's 258.5; set it to 258.5 to get the stock amount.
- As before, these settings apply to every world Riverheim generates while the mod is installed, so a changed config changes existing worlds. See `docs/preset-handler-notes.md` for why per-world settings are not possible with Riverheim 1.2.1.

## [2.0.0] - 2026-09-14

### Breaking: rebuilt for Gurebu-Riverheim 1.1.x

- Replaced the removed Riverheim 0.12 `DefaultConfig` patches with a patch of Riverheim 1.1's `ConfigManager.GetConfig` generation entry point.
- Updated the dependency from `Riverheim_Dev-Riverheim-0.12.0` to `Gurebu-Riverheim-1.1.0`.
- Rebuilt the configuration around actual Riverheim 1.1 fields: world, river origins/width/pruning/meanders, lake budget/affinity, and biome placement biases.
- Removed obsolete 0.12-only settings rather than allowing a config file to report success while silently doing nothing.
- Added explicit startup and field-layout failures to prevent false "all patches applied" reporting.
- Every default now matches Riverheim 1.1 exactly, so an untouched config generates stock Riverheim worlds (river `WidthOffset` -6, `MinWidth` 14.5, `MaxWidthDifference` 2.2).
- Split the single ocean setting into `SeaFloorDepth` (default -40, the world edge and trench floor) and `OceanBiomeDepth` (default -20, how deep water must be to count as Ocean). The old `OceanDepth` set both.
- Added `StartingAreaRadius`, a `5. Biome Amounts` section (how much contested land each biome takes) and a `6. Biome Bands` section (where Swamp, Plains and Mistlands appear and how far Meadows reaches, as percent of the way from spawn to the world edge).

### Migration

- This is not compatible with Riverheim Config 1.x or Riverheim 0.12.
- Version 2 writes new sections. Legacy 1.x entries remain in existing config files but are ignored.
- Back up worlds and configure before generating terrain; every client and the server must use the same Riverheim and Riverheim Config versions.

## [1.0.4] - 2025-10-21

### 🎉 MAJOR UPDATE: Complete Biome Control System

#### Added - Ocean & Landmass Control
- **OceanDepth** - Control continent size vs ocean coverage
  - `-20` = Vanilla (standard continents)
  - `-25` = More ocean (smaller continents, archipelago)
  - `-15` = Less ocean (larger continents, Pangaea)
  - Range: -40 to -5
- **MountainHeight** - Control mountain biome threshold
  - `40` = Vanilla (standard mountains)
  - `35` = More mountains (lower threshold)
  - `45` = Fewer mountains (higher threshold)
  - Range: 30 to 55

#### Added - Complete Biome Configuration for ALL Biomes

**Meadows (3 parameters)**
- `FixedBias` - Base competitiveness (default: 0)
- `EnableRiverBonus` - Toggle river preference (default: true)
- `RiverBonus` - River proximity bonus (default: 15)

**Black Forest (1 parameter)**
- `FixedBias` - Base competitiveness (default: -5)

**Swamp (9 parameters)** - FULLY CONFIGURABLE
- `FixedBias` - Base competitiveness (default: 8, recommended: -2)
- `RiverBonus_AtRiver` - Bonus at rivers (default: -25, **recommended: +25**)
- `RiverBonus_Near140m` - Bonus near rivers (default: -15, recommended: +15)
- `RiverBonus_EndDistance` - River influence range (default: 170m, recommended: 300m)
- `CoastalPenalty` - Coastal penalty (default: -90, recommended: -40)
- `CoastalPenalty_EndDistance` - Coastal range (default: 300m, recommended: 200m)
- `TravelDistance_Start` - Min spawn distance (default: 1350m, recommended: 800m)
- `TravelDistance_Peak` - Full strength distance (default: 2200m, recommended: 1500m)
- `TravelDistance_End` - Max spawn distance (default: 7000m, recommended: 8000m)

**Plains (3 parameters)**
- `FixedBias` - Base competitiveness (default: 5, recommended: 0)
- `TravelDistance_Start` - Min spawn distance (default: 2800m)
- `TravelDistance_End` - Max spawn distance (default: 7200m)

**Mistlands (3 parameters)**
- `FixedBias` - Base competitiveness (default: 0)
- `TravelDistance_Start` - Min spawn distance (default: 5500m)
- `TravelDistance_Peak` - Full strength distance (default: 6500m)

#### Key Features

✅ **"Rivers End at Swamps"** - Implemented community suggestion!
- Swamps now PREFER rivers instead of avoiding them
- Reversing river bias creates natural swamp clusters at river mouths

✅ **Easy Biome Balancing**
- Adjust any biome's frequency with simple FixedBias values
- Control spawn distances for progression tuning
- Enable/disable biome preferences

✅ **Ocean/Continent Control**
- Create archipelago worlds (many small islands)
- Create Pangaea worlds (one large continent)
- Fine-tune land/ocean ratio

✅ **Full Logging**
- All changes logged for debugging
- Shows vanilla → modified values
- Easy troubleshooting

#### Changed
- Updated version to 1.0.4
- Updated MinimumRequiredVersion to 1.0.4
- Reorganized config sections (section 8 = Ocean/Landmass, section 9 = Biomes)
- Enhanced logging output with all biome values

#### Documentation
- Created `com.valheim.riverheim.config.cfg` - Complete config template with recommended values
- Updated `biome_analysis_and_fixes.md` - Technical analysis
- Updated `BIOME_CONFIG_GUIDE.md` - User guide
- Updated `SWAMP_FIX_QUICK_REFERENCE.txt` - Quick reference card
- Updated `CHANGELOG_BIOMES.md` - Detailed biome changes

---

## [1.0.2] - Previous Version

### Added - Swamp Biome Configuration (Initial)
- Basic swamp biome parameters
- River distance bias controls
- Coastal and travel distance settings

### Added - Plains Biome Configuration
- Plains FixedBias control

---

## [1.0.0] - Initial Release

### Added - River & Lake Configuration
- River spawning controls (density, variability, recovery)
- River size controls (width scale, power, offset)
- River filtering (Strahler order, min width, catchment)
- River appearance (meander, depth, bank steepness)
- River valleys (offset, magnitude, exponent)
- Lake controls (threshold, noise scale, contributions)
- World size controls (radius, tile spacing)

### Features
- ServerSync integration for multiplayer
- Harmony patching of Riverheim internals
- Comprehensive logging
- Config file generation

---

## Migration Guide: 1.0.2 → 1.0.4

### Automatic Migration
Your existing config will automatically gain new sections. Default values match vanilla behavior.

### Recommended Actions

1. **Fix Underrepresented Swamps**
   ```ini
   [9. Biomes - Swamp]
   FixedBias = -2
   RiverBonus_AtRiver = 25
   RiverBonus_Near140m = 15
   RiverBonus_EndDistance = 300
   CoastalPenalty = -40
   CoastalPenalty_EndDistance = 200
   TravelDistance_Start = 800
   TravelDistance_Peak = 1500
   TravelDistance_End = 8000
   ```

2. **Balance Plains (Optional)**
   ```ini
   [9. Biomes - Plains]
   FixedBias = 0
   ```

3. **Create Archipelago World (Optional)**
   ```ini
   [8. Ocean & Landmass]
   OceanDepth = -30
   ```

4. **Create Pangaea World (Optional)**
   ```ini
   [8. Ocean & Landmass]
   OceanDepth = -10
   ```

### Breaking Changes
- None! All new features use vanilla defaults

---

## Community Requests Fulfilled

### ✅ "Rivers end at swamps"
> "a little bit more Swamps would be nice, feels like the most under-represented area. One thing I thought would have been a nice way to include more swamp biome would be to have most rivers end at a swamp"

**Implemented in 1.0.4:**
- Swamps now get +25 bonus near rivers (was -25 penalty)
- Swamps naturally cluster at river mouths
- 2-3x more swamps overall with recommended settings

### ✅ "Easy way to edit any/all biomes"
**Implemented in 1.0.4:**
- All 5 major biomes now configurable
- Simple FixedBias values for frequency control
- Distance-based spawn controls
- River/coastal preference toggles

### ✅ "Ocean levels / more or less continent"
**Implemented in 1.0.4:**
- OceanDepth parameter controls land/ocean ratio
- Create archipelagos or Pangaea worlds
- MountainHeight for mountain frequency

---

## Technical Details

### Version 1.0.4 Changes

**New Config Parameters:** 19
- Ocean/Landmass: 2
- Meadows: 3
- Black Forest: 1
- Swamp: 9 (expanded from basic implementation)
- Plains: 3 (expanded from 1)
- Mistlands: 3

**Total Config Parameters:** 60+

**Code Changes:**
- Added ocean depth patching
- Added mountain height patching
- Added Meadows biome patching
- Added Black Forest biome patching
- Expanded Plains biome patching (travel distances)
- Added Mistlands biome patching
- Enhanced logging for all biomes
- Conditional logging (only show changed values)

**Patching Strategy:**
- Uses Harmony reflection to modify Riverheim's internal config
- Patches `BiomeCalculationConfig.OceanDepth`
- Patches `CommonConfig.MountainHeight`
- Patches individual `BiomeConfig` structs for each biome
- Reconstructs `StaticArray<Rfloat2>` for distance-based biases

---

## Known Issues

### None Currently

### Compatibility
- ✅ Riverheim 0.12.0+
- ✅ BepInEx 5.4.x
- ✅ Valheim 0.217.x+
- ⚠️ May conflict with other biome mods
- ⚠️ May conflict with ExpandWorldSize mod (use WorldRadius instead)

---

## Future Plans

### Potential 1.0.5 Features
- Ashlands biome configuration
- Deep North biome configuration
- Height bias controls for all biomes
- Roughness bias controls
- Biome noise scale adjustments
- Preset system (Archipelago, Pangaea, Swamp World, etc.)
- In-game config UI

### Community Requests
Have a suggestion? Open an issue or discussion!

---

## Credits

- **Riverheim Mod** - Original terrain generator by gurebu
- **BepInEx** - Modding framework
- **Harmony** - Runtime patching library
- **ServerSync** - Config synchronization
- **Community** - Feature requests and testing

---

## Support

### Getting Help
1. Check `BIOME_CONFIG_GUIDE.md` for detailed parameter explanations
2. Check `SWAMP_FIX_QUICK_REFERENCE.txt` for quick fixes
3. Check BepInEx logs for error messages
4. Verify you created a NEW world (changes don't apply to existing worlds)

### Reporting Issues
Include:
- Riverheim Config version
- Riverheim version
- BepInEx log file
- Config file
- Description of issue

---

## License

Same as Riverheim mod - check original mod page for details.
