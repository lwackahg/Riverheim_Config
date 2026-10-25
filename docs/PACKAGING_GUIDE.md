# Packaging Guide

## Folder structure

```
Riverheim_Config/
├── src/
│   ├── RiverheimConfigTest.cs          BepInEx plugin
│   ├── RiverheimSettings.cs            the settings table, shared with the AtlasBuilder UI launcher
│   ├── ExpandWorldSizeCompatibility.cs fixes forests and vegetation in EWS-stretched worlds
│   ├── RiverheimConfigTest.csproj
│   ├── ThirdParty/ServerSync/          ServerSync v1.20 source and licence, compiled into the plugin
│   ├── settings.json                   generated from RiverheimSettings.cs for the launcher (rhconfig-test json)
│   └── README.md                       README for the source files shipped in the package
├── docs/                        SETTINGS.md (generated: rhconfig-test md), this guide, design notes
├── presets/                     Defaults.cfg (every setting at its default) and themed presets
├── ThunderstorePackage/         manifest.json, README.md, CHANGELOG.md, icon.png
├── tests/                       DLL check and AtlasBuilder render tests (see tests/README.md)
└── package.ps1                  builds the Thunderstore zip into Releases/
```

## Steps to release

1. Edit `src/RiverheimSettings.cs`, then regenerate the derived files and check them against Riverheim:
   ```powershell
   dotnet build tests\RiverheimConfig.Tests -c Release
   $tool = "tests\RiverheimConfig.Tests\bin\Release\net8.0\rhconfig-test.dll"
   dotnet $tool check Gurebu-Riverheim\Riverheim.dll
   dotnet $tool json src\settings.json
   dotnet $tool md docs\SETTINGS.md
   .\tests\Run-AtlasTests.ps1
   ```
2. Copy `src/RiverheimSettings.cs` and `src/settings.json` to the AtlasBuilder UI launcher repository, which ships both.
3. Bump the version in `ThunderstorePackage/manifest.json` and `PluginVersion` in `src/RiverheimConfigTest.cs` (package.ps1 refuses to package if they differ), and add a `CHANGELOG.md` entry. Keep `ThunderstorePackage/README.md` and the root `README.md` in step.
4. Build and package:
   ```powershell
   dotnet msbuild src\RiverheimConfigTest.csproj -t:Rebuild -p:Configuration=Release
   .\package.ps1
   ```
5. Test the zip from `Releases/` on a local server and client (config sync, a new world, `BepInEx/LogOutput.log`).
6. Upload at https://thunderstore.io/c/valheim/create/.

## Package contents

```
Riverheim_Config-<version>.zip
├── manifest.json, README.md, CHANGELOG.md, icon.png
├── BepInEx/plugins/RiverheimConfigTest.dll
└── SourceCode/  (the src/ files: plugin, settings table, EWS fix, .csproj, ThirdParty/ServerSync/, README.md)
```

## Manifest

- **Name**: `Riverheim_Config`
- **Dependencies**: `denikson-BepInExPack_Valheim`, `Gurebu-Riverheim` (the version the release was tested against)
