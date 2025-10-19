# Changelog

## [1.0.3] - 2025-10-18

### Changed
- Packaging script now reads version from `manifest.json` (single source of truth).
- Package zips now output to `Releases/` folder for cleaner repo organization.
- Added `.gitignore` to exclude build artifacts, releases, and temp files.
- Expanded repo README with install/troubleshooting/support/roadmap sections.

### Notes
- No gameplay or config behavior changes.

## [1.0.2] - 2025-10-18

### Changed
- Added prominent BETA warning to README advising backups and potential unintended consequences.
- Synchronized all version references to `1.0.2` (plugin, ServerSync min version, manifest, package script).

## [1.0.1] - 2025-10-18

### Changed
- Package layout updated to place the DLL under `BepInEx/plugins/` inside the zip so Thunderstore app can auto-install.
- Manifest version bumped to `1.0.1`.

### Notes
- No gameplay or config behavior changes.

## [1.0.0] - 2025-10-18

### Added
- Initial release
- Server-enforced configuration for Riverheim Rivers & Lakes
- 26 configurable parameters across 7 categories:
  - River Spawning (5 parameters)
  - River Size (3 parameters)
  - River Filtering (3 parameters)
  - River Appearance (5 parameters)
  - River Valleys (3 parameters)
  - Lakes (4 parameters)
  - World Size (2 parameters)
- Automatic client synchronization using ServerSync
- Version checking and compatibility enforcement
- Detailed parameter descriptions with true limits, practical ranges, and defaults
- Config helper methods for cleaner code
- Comprehensive error handling and logging

### Technical Details
- Built with BepInEx 5.4.2202
- Uses ServerSync for config synchronization
- ILRepack integration for single-DLL distribution
- Harmony patches for Riverheim integration
- Target framework: .NET Framework 4.8
