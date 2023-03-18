using System.Runtime.InteropServices;
using NiceIO;

namespace BuildDriver;

public class Program
{
    public static bool RunningOnYamato() => Environment.GetEnvironmentVariable("YAMATO_PROJECT_ID") != null;
    public static async Task<int> Main(string[] args)
    {
        string architecture = GetArchitectures()[0]; // Get default arch for current system
        string configuration = "Release";
        bool silent = false;
        bool skipBuild = false;
        bool test = false;
        bool zip = RunningOnYamato();
        Task<NPath>? zipTask = null;
        foreach (string arg in args)
        {
            if ((arg.StartsWith("--arch") || arg.StartsWith("--architecture="))
                && !TryParseArgument(GetArchitectures(true), arg, out architecture))
                return 1;
            else if ((arg.StartsWith("--config=") || arg.StartsWith("--configuration="))
                     && !TryParseArgument(new[] {"Release", "Debug"}, arg, out configuration))
                return 1;
            else if (arg.Equals("--silent") || arg.Equals("--s"))
                silent = true;
            else if (arg.Equals("--zip") || arg.Equals("--z"))
                zip = true;
            else if (arg.Equals("--skip-build"))
                skipBuild = true;
            else if (arg.Equals("--test"))
            {
                skipBuild = true; // Assume we've already built
                test = true;
            }
        }

        if (RunningOnYamato())
        {
            zipTask = SevenZip.DownloadAndUnzip7Zip();
        }

        Console.WriteLine("*****************************");
        Console.WriteLine("Unity: Starting CoreCLR build");
        Console.WriteLine($"\tPlatform:\t\t{RuntimeInformation.OSDescription}");
        Console.WriteLine($"\tArchitecture:\t\t{architecture}");
        Console.WriteLine($"\tConfiguration:\t\t{configuration}");
        Console.WriteLine("*****************************");

        GlobalConfig gConfig = new (){ Architecture = architecture, Configuration = configuration, Silent = silent};
        EmbeddingHost.Build(gConfig);
        if (!skipBuild)
        {
            NullGC.Build(gConfig);
            CoreCLR.Build(gConfig);

            // TODO: Switch to using Embedding Host build to perform the copy instead of this once that lands.
            NPath artifacts = ConsolidateArtifacts(gConfig);

            NPath zipExe = new("7z");
            if (zipTask != null)
            {
                zipExe = await zipTask;
                if (zipTask.Exception != null)
                    throw zipTask.Exception;
            }

            if (zip)
                SevenZip.Zip(zipExe, artifacts, gConfig);
        }

        if (test)
        {
            EmbeddingHost.Test(gConfig);
            CoreCLR.Test(gConfig);
            Console.WriteLine("******************************");
            Console.WriteLine("Unity: Tested CoreCLR successfully");
            Console.WriteLine("******************************");
        }

        return 0;
    }

    static NPath ConsolidateArtifacts(GlobalConfig gConfig)
    {
        string osAbbrev = "win";
        string unityGCLib = "unitygc.dll";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            osAbbrev = "osx";
            unityGCLib = "libunitygc.dylib";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            osAbbrev = "linux";
            unityGCLib = "libunitygc.so";
        }

        NPath destDir = Paths.RepoRoot.Combine("artifacts", "bin",
            $"microsoft.netcore.app.runtime.{osAbbrev}-{gConfig.Architecture}", gConfig.Configuration, "runtimes",
            $"{osAbbrev}-{gConfig.Architecture}");

        Paths.UnityGC.Combine(gConfig.Configuration, unityGCLib).Copy(destDir.Combine("native"));

        NPath tfmDir = Paths.UnityEmbedHost.Combine("bin", gConfig.Configuration).Directories().Single();
        tfmDir.Files("unity-embed-host.*").Copy(destDir.Combine("lib", tfmDir.FileName));
        Paths.RepoRoot.Combine("LICENSE.md").Copy(destDir);

        return destDir;
    }

    static bool TryParseArgument(string[] validArgs, string arg, out string value)
    {
        string[] args = arg.Split('=');
        if (string.IsNullOrEmpty(args[1]) || !validArgs.Contains(args[1]))
        {
            value = string.Empty;
            Console.WriteLine($"Invalid option: {arg}  Example : `--{args[0]}={validArgs[0]}`");
            return false;
        }

        value = args[1];

        return true;
    }

    private static string[] GetArchitectures(bool allArchitectures = false)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (allArchitectures)
            {
                return new[] { "x64", "x86", "arm64" };
            }

            if (RuntimeInformation.OSArchitecture == Architecture.X86)
                return new[] { "x86" };
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                return new[] { "arm64" };
            return new[] { "x64" };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new[] { "x64" };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (allArchitectures)
                return new[] { "x64", "arm64" };

            if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                return new[] { "arm64" };
            return new[] { "x64" };
        }

        throw new ArgumentException($"Unsupported build platform {RuntimeInformation.OSDescription}");
    }
}
