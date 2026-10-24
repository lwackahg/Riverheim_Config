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
to the lowland Black Forest variant.

Other commands: `dump <dll> [version|master]` prints every leaf of the default config (handy for picking new
settings), `apply <dll> <cfg>` shows what a cfg changes, `stock-cfg <dll> <out>` writes a cfg with the DLL's own
values, `cfg <out>` writes an all-default cfg, `json <out>` exports `settings.json` for the AtlasBuilder launcher
and `md <out>` writes the settings reference table used in the README.

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
```

- **Stock**: a cfg holding AtlasBuilder's own default values, with every value forced through the mapping
  (`RIVERHEIM_CONFIG_FORCE=1`), renders byte-identical to `--preset Default`; so does an all-default cfg. This is
  the critical check that the mod does not change anything a player did not ask for.
- **Settings**: every setting rendered at a low and a high value (default minus and plus ten steps, clamped, with a
  few hand-picked ranges) must change the `Rich` render of seed 400. A setting that changes nothing is reported as a
  failure: its mapping is broken or the field has no effect. Settings whose field does not exist in AtlasBuilder's
  older Riverheim (the lowland Black Forest ones) are reported as not previewable.
- **Reference**: `presets/BEST.cfg` renders identically to the image in `tests/reference`. Rerun with
  `-UpdateReference` when the mapping is changed on purpose.

Needs Windows PowerShell 5.1, the .NET 8 SDK, `Riverheim.AtlasBuilder.exe` (default location
`Gurebu-Riverheim_AtlasBuilder\` next to this repository, or `-AtlasExe`) and a checkout of the launcher repository
(`..\atlasbuilder-ui-repo`, `C:\tmp\abui` or `-LauncherRepo`). AtlasBuilder 1.0.0 crashes when its stdout is
redirected, so every render opens a minimised console window. Outputs go to `tests/out` (ignored by git).
