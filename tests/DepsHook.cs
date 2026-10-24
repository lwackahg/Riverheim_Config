// .NET startup hook that lets Gurebu's Riverheim AtlasBuilder run an installed Riverheim.dll (the BepInEx plugin)
// in place of the older Riverheim it bundles. Riverheim.dll references BepInEx, Harmony and Unity assemblies, and
// its pipeline registry scans the whole assembly at start-up, so those references must resolve even though
// AtlasBuilder never calls into them. This hook resolves them from the folder named in RIVERHEIM_DEPS_DIR (copies
// of the BepInEx core and Valheim's managed assemblies, made by tests/Run-AtlasTests.ps1 under tests/out).
//
// Used together with RiverheimConfigHook.cs from the AtlasBuilder UI launcher: DOTNET_STARTUP_HOOKS takes several
// hooks separated by ';'. Written for the C# 5 compiler that ships with Windows (.NET Framework 4.x).
using System;
using System.IO;
using System.Reflection;

public static class StartupHook
{
    public static void Initialize()
    {
        string directory = Environment.GetEnvironmentVariable("RIVERHEIM_DEPS_DIR");
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            Console.Error.WriteLine("[Installed Riverheim] RIVERHEIM_DEPS_DIR is not set or does not exist; dependencies may fail to load.");
            return;
        }
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
        {
            string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
    }
}
