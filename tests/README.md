# Tests

Riverheim Config cannot be tested inside Valheim from a script, so these tests check the mod's settings table
(`RiverheimSettings.cs`) against real Riverheim code outside the game.

## 1. DLL check (seconds)

`tests/RiverheimConfig.Tests` is a small .NET 8 console tool that compiles `RiverheimSettings.cs` and loads a
Riverheim assembly by reflection. Riverheim's configuration types do not touch Unity, so the BepInEx plugin
`Riverheim.dll` loads fine on .NET 8.

```powershell
dotnet build tests\RiverheimConfig.Tests -c Release
dotnet tests\RiverheimConfig.Tests\bin\Release\net8.0\rhconfig-test.dll check Gurebu-Riverheim\Riverheim.dll
```

`check` verifies, for the default preset version the DLL reports:

- every field path resolves,
- every listed default equals Riverheim's own value,
- forcing every default through `Apply` reproduces the stock config exactly (this covers the biome band curves),
- applying an all-default config changes nothing,
- each setting, moved away from its default, changes exactly its own field.

Run it against `Riverheim.Core.dll` from AtlasBuilder (the harness below unpacks it to `tests/out/bundle-*`) to see
which defaults differ in that older build; `ForestWeight` 261 vs 258.5 is expected, Riverheim 1.1.1 moved 2.5 of it
to the lowland Black Forest variant, and the three `ForestLowlands*` settings are skipped because the field does not
exist there.

Other commands: `dump <dll> [version|master]` prints every leaf of the default config (handy for picking new
settings), `apply <dll> <cfg>` shows what a cfg changes, `stock-cfg <dll> <out>` writes a cfg with the DLL's own
values, `cfg <out>` writes an all-default cfg, `json <out>` exports `settings.json` for the AtlasBuilder launcher,
`md <out>` writes the settings reference table used in the README, and `retarget` prepares Riverheim for AtlasBuilder
(below). The tool references `Mono.Cecil.dll` from a BepInEx install for `retarget`; pass
`-p:MonoCecilDir=<folder>` or `-p:ValheimDir=<Valheim folder>` to `dotnet build` if it is not found.

## 2. AtlasBuilder renders (minutes)

`Run-AtlasTests.ps1` drives Gurebu's Riverheim AtlasBuilder headless, the same way the
[AtlasBuilder UI launcher](https://github.com/lwackahg/atlasbuilder-ui-repo) does: its startup hook
`RiverheimConfigHook.cs` is compiled together with this repository's `RiverheimSettings.cs` using the C# 5 compiler
that ships with Windows, and AtlasBuilder is asked for the `Experimental` preset, which the hook has replaced with
Default plus the cfg named in `RIVERHEIM_CONFIG_PATH`. Renders are deterministic, so images are compared by hash.

```powershell
.\tests\Run-AtlasTests.ps1                                   # Stock, Settings and Reference
.\tests\Run-AtlasTests.ps1 -Tests Stock -Seeds 400,1337 -Modes Biomes,Rich
.\tests\Run-AtlasTests.ps1 -Tests Settings -Only LandCohesion,SwirlRays
.\tests\Run-AtlasTests.ps1 -Tests Reference -UpdateReference  # after an intended mapping change
.\tests\Run-AtlasTests.ps1 -Engine Bundled -Tests Stock       # with the Riverheim inside AtlasBuilder 1.0.0
```

- **Stock**: a cfg holding Riverheim's own default values, with every value forced through the mapping
  (`RIVERHEIM_CONFIG_FORCE=1`), renders byte-identical to `--preset Default`; so does an all-default cfg, and so do
  the defaults listed in `RiverheimSettings.cs`. This is the critical check that the mod does not change anything a
  player did not ask for.
- **Settings**: every setting rendered at a low and a high value (default minus and plus ten steps, clamped, with a
  few hand-picked ranges) must change the `Rich` render of seed 400. A setting that changes nothing is reported as a
  failure: its mapping is broken or the field has no effect. A value that makes Riverheim throw is reported as a
  crash, also a failure: narrow the range or the step so that the probes stay inside what Riverheim accepts, and say
  so in the description.
- **Reference**: `presets/BEST.cfg` renders identically to the image in `tests/reference`. Rerun with
  `-UpdateReference` when the mapping is changed on purpose (the `.sha256` file records which Riverheim rendered it).

### Which Riverheim renders

AtlasBuilder 1.0.0 is a single-file app that bundles its own, older Riverheim (`Riverheim.Core.dll`, from before
1.1.1), and its maps differ measurably from what Riverheim 1.2.1 generates (5 to 15 percent of the pixels of seed
1337). So by default (`-Engine Installed`) the harness renders with the installed Riverheim instead, without changing
AtlasBuilder:

1. `rhconfig-test retarget` writes `Gurebu-Riverheim\Riverheim.dll` (the BepInEx plugin; `-RiverheimDll` for another
   path) as `Riverheim.Core.dll`, the assembly name AtlasBuilder references, and patches AtlasBuilder's
   `Riverheim.Dev.dll` so that its hard-coded Default preset (`builtin.amur`, version 1) uses Riverheim's own
   `PRESET_DEFAULT_VERSION` (2 in 1.2.1), which is what a new world gets in game. Both are written under `tests/out`.
2. AtlasBuilder's normal self-extraction is copied to `tests/out/installed-*/extract` with those two files swapped in,
   and the unmodified exe is started with `DOTNET_BUNDLE_EXTRACT_BASE_DIR` pointing there, so it loads them.
3. `Riverheim.dll` references BepInEx, Harmony and Unity assemblies, and its pipeline registry scans the whole
   assembly at start-up, so a second startup hook, `tests/DepsHook.cs`, resolves those from
   `tests/out/installed-*/deps`: copies of `BepInEx\core` (`-BepInExCoreDir`) and of `valheim_Data\Managed` from the
   Valheim install (`-ValheimDir`, or the `VALHEIM_DIR` environment variable). The copies stay under `tests/out`,
   which git ignores; nothing in the game or the profile is touched, and none of these DLLs belong in the repository.

Both hooks are compiled against the retargeted `Riverheim.Core.dll`, so the config hook's compiled-in
`PRESET_DEFAULT_VERSION` matches. `-Engine Bundled` renders with AtlasBuilder's own Riverheim; there the
`ForestLowlands*` settings are reported as not previewable and the listed 1.2.1 defaults are expected to differ.

Needs Windows PowerShell 5.1, the .NET 8 SDK, `Riverheim.AtlasBuilder.exe` (default location
`Gurebu-Riverheim_AtlasBuilder\` next to this repository, or `-AtlasExe`), a checkout of the launcher repository
(`..\atlasbuilder-ui-repo`, `C:\tmp\abui` or `-LauncherRepo`) and, for the default engine, a Valheim install with
BepInEx. AtlasBuilder 1.0.0 crashes when its stdout is redirected, so every render opens a minimised console window.
Outputs go to `tests/out` (ignored by git).
