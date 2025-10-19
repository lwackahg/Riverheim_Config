# Thunderstore Packaging Guide

## Folder Structure

```
a:\Valheim Fix\
├── ThunderstorePackage/     (Package files for distribution)
│   ├── manifest.json
│   ├── README.md
│   ├── CHANGELOG.md
│   └── icon.png (YOU CREATE THIS)
├── SourceCode/              (Source code for modders)
│   ├── RiverheimConfigTest.cs
│   ├── RiverheimConfigTest.csproj
│   ├── ILRepack.targets
│   ├── ServerSync.dll
│   └── README.md
├── setup_package.ps1        (Run once to organize files)
└── package.ps1              (Run to create zip)
```

## Steps to Package

### 1. Setup Folder Structure (First Time Only)
```powershell
.\setup_package.ps1
```

This organizes all files into proper folders.

### 2. Create Icon
Create a 256x256 PNG image named `icon.png` in the `ThunderstorePackage` folder.
- Use a river/water theme
- Keep it simple and recognizable
- Delete `icon_instructions.txt` after creating icon

### 3. Build the Mod
```powershell
dotnet msbuild RiverheimConfigTest.csproj /t:Rebuild /p:Configuration=Release
```

### 4. Run Package Script
```powershell
.\package.ps1
```

This will create: `Riverheim_Config-1.0.0.zip`

### 5. Test Locally
1. Extract the zip to your BepInEx plugins folder
2. Verify the DLL loads correctly
3. Test config synchronization

### 6. Upload to Thunderstore
1. Go to https://thunderstore.io/c/valheim/create/
2. Upload `Riverheim_Config-1.0.0.zip`
3. Fill in any additional metadata
4. Publish!

## Package Structure

```
Riverheim_Config-1.0.0.zip
├── manifest.json
├── README.md
├── CHANGELOG.md
├── icon.png
├── RiverheimConfigTest.dll
└── SourceCode/
    ├── RiverheimConfigTest.cs
    ├── RiverheimConfigTest.csproj
    ├── ILRepack.targets
    ├── ServerSync.dll
    └── README.md
```

Users get the compiled DLL, modders get the full source code!

## Manifest Details

- **Name**: `Riverheim_Config` (underscores become spaces in display)
- **Version**: `1.0.0` (semantic versioning)
- **Dependencies**: 
  - BepInExPack_Valheim 5.4.2202
  - Riverheim 0.12.0

## Before Publishing

- [ ] Icon created (256x256 PNG)
- [ ] Mod built successfully
- [ ] Tested on local server
- [ ] Tested client sync
- [ ] README reviewed
- [ ] Version number correct in manifest.json
- [ ] Dependencies correct

## Updating Version

When releasing updates:
1. Update version in `manifest.json`
2. Update version in `RiverheimConfigTest.cs` (PluginVersion)
3. Add entry to `CHANGELOG.md`
4. Rebuild and repackage
5. Upload new version to Thunderstore
