# Riverheim Config - Source Code

> WARNING: BETA — Untested. Back up your worlds and saves.
> Changes may have unintended consequences. Use at your own risk.

Complete source code for the Riverheim Config mod.

## Build Command
```
dotnet msbuild RiverheimConfigTest.csproj /t:Rebuild /p:Configuration=Release
```

Output: bin\Release\RiverheimConfigTest.dll

## Files
- RiverheimConfigTest.cs - Main plugin code
- RiverheimConfigTest.csproj - Project file
- ILRepack.targets - Merges ServerSync into output DLL
- ServerSync.dll - Config synchronization library
