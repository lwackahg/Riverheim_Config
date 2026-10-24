# Riverheim Config 3.0.0 Source

This source targets Gurebu-Riverheim 1.2.x. It patches `Riverheim.Configuration.ConfigManager.GetConfig` and modifies the returned generation configuration immediately before Riverheim runs its pipeline. Settings left at their default are not written, so Riverheim keeps its own value for them.

- `RiverheimSettings.cs`: every setting (section, key, default, range, description, field path) and the reflection code that applies it. Shared, unchanged, with the AtlasBuilder UI launcher (https://github.com/lwackahg/atlasbuilder-ui-repo), where the C# 5 compiler that ships with Windows compiles it into a startup hook for Riverheim AtlasBuilder.
- `RiverheimConfigTest.cs`: the BepInEx plugin (config binding, ServerSync, the Harmony postfix).

Build with a Valheim install path supplied when needed:

```powershell
dotnet msbuild RiverheimConfigTest.csproj /t:Rebuild /p:Configuration=Release /p:ValheimDir="C:\\Program Files (x86)\\Steam\\steamapps\\common\\Valheim"
```

The output is `bin/Release/RiverheimConfigTest.dll`. Tests live in the repository at https://github.com/lwackahg/Riverheim_Config (`tests/`).
