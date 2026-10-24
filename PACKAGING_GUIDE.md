# Packaging Guide

## Folder structure

```
Riverheim_Config/
├── RiverheimConfigTest.cs       BepInEx plugin
├── RiverheimSettings.cs         the settings table, shared with the AtlasBuilder UI launcher
├── RiverheimConfigTest.csproj
├── ThirdParty/ServerSync/        ServerSync v1.20 source and licence, compiled into the plugin
├── settings.json, SETTINGS.md   generated from RiverheimSettings.cs (rhconfig-test json / md)
├── presets/BEST.cfg
├── ThunderstorePackage/         manifest.json, README.md, CHANGELOG.md, icon.png
├── SourceCode/README.md         README for the source files shipped in the package
├── tests/                       DLL check and AtlasBuilder render tests (see tests/README.md)
├── docs/                        design notes
└── package.ps1                  builds the Thunderstore zip into Releases/
```

## Steps to release

1. Edit `RiverheimSettings.cs`, then regenerate the derived files and check them against Riverheim:
   ```powershell
   dotnet build tests\RiverheimConfig.Tests -c Release
   $tool = "tests\RiverheimConfig.Tests\bin\Release\net8.0\rhconfig-test.dll"
   dotnet $tool check Gurebu-Riverheim\Riverheim.dll
   dotnet $tool json settings.json
   dotnet $tool md SETTINGS.md
   .\tests\Run-AtlasTests.ps1
   ```
2. Copy `RiverheimSettings.cs` and `settings.json` to the AtlasBuilder UI launcher repository, which ships both.
3. Bump the version in `ThunderstorePackage/manifest.json` and `PluginVersion` in `RiverheimConfigTest.cs` (package.ps1 refuses to package if they differ), and add a `CHANGELOG.md` entry. Keep `ThunderstorePackage/README.md` and the root `README.md` in step.
4. Build and package:
   ```powershell
   dotnet msbuild RiverheimConfigTest.csproj -t:Rebuild -p:Configuration=Release
   .\package.ps1
   ```
5. Test the zip from `Releases/` on a local server and client (config sync, a new world, `BepInEx/LogOutput.log`).
6. Upload at https://thunderstore.io/c/valheim/create/.

## Package contents

```
Riverheim_Config-<version>.zip
├── manifest.json, README.md, CHANGELOG.md, icon.png
├── BepInEx/plugins/RiverheimConfigTest.dll
└── SourceCode/  (RiverheimConfigTest.cs, RiverheimSettings.cs, RiverheimConfigTest.csproj, ThirdParty/ServerSync/, README.md)
```

## Manifest

- **Name**: `Riverheim_Config`
- **Dependencies**: `denikson-BepInExPack_Valheim`, `Gurebu-Riverheim` (the version the release was tested against)
