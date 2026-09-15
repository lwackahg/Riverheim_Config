# Riverheim Config 2.0.0 Source

This source targets Gurebu-Riverheim 1.1.x. It patches `Riverheim.Configuration.ConfigManager.GetConfig` and modifies the returned generation configuration immediately before Riverheim runs its pipeline.

Build with a Valheim install path supplied when needed:

```powershell
dotnet msbuild RiverheimConfigTest.csproj /t:Rebuild /p:Configuration=Release /p:ValheimDir="C:\\Program Files (x86)\\Steam\\steamapps\\common\\Valheim"
```

The output is `bin/Release/RiverheimConfigTest.dll`.
