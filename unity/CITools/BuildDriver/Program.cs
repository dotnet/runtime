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
        TestTargets testTargets = TestTargets.None;
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
                testTargets = TestTargets.All;
            }
            else if (arg.StartsWith("--test="))
            {
                if (!TryParseTestTargets(arg, out var testTarget))
                    return 1;
                skipBuild = true; // Assume we've already built
                testTargets = Enum.Parse<TestTargets>(testTarget, ignoreCase: true);
            }
        }

        // This gives a way to test and build in the same command
        if (args.Any(a => a == "--build"))
            skipBuild = false;

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

        GlobalConfig gConfig = new ()
        {
            Architecture = architecture,
            Configuration = configuration,
            Silent = silent,
            DotNetVerbosity = "quiet"
        };

        // We need to build even when `skipBuild` is false because on CI we have the build and tests split into separate jobs.  And the way we have artifacts setup,
        // we don't retain anything built under `unity`.  And therefore we need to rebuild it so that tests that depend on something in managed.sln can find what they need
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

        if (testTargets != TestTargets.None)
        {
            if (testTargets.HasFlag(TestTargets.Embedding))
                EmbeddingHost.Test(gConfig);

            if (testTargets.HasFlag(TestTargets.CoreClr))
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
        Paths.RepoRoot.Combine("LICENSE.TXT").Copy(destDir);

        return destDir;
    }

    static bool TryParseTestTargets(string arg, out string value)
    {
        var values = Enum.GetValues(typeof(TestTargets));
        var valid = new List<string>();
        foreach(var v in values)
            valid.Add(v.ToString().ToLower());

        return TryParseArgument(valid.ToArray(), arg, out value);
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
