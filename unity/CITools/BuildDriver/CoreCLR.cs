// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using NiceIO;

namespace BuildDriver;
public static class CoreCLR
{
    private static NPath BuildScript => Paths.RepoRoot.Combine(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "build.cmd" : "build.sh");

    public static string ToSubSetString(this BuildTargets targets)
    {
        var subsets = new List<string>();
        if (targets.HasFlag(BuildTargets.Runtime))
            subsets.Add("clr");

        if (targets.HasFlag(BuildTargets.ClassLibs))
            subsets.Add("libs");

        return subsets.AggregateWith("+");
    }

    public static void Build(GlobalConfig gConfig, BuildTargets buildTargets)
    {
        Console.WriteLine("******************************");
        Console.WriteLine("Unity: Building CoreCLR runtime");
        Console.WriteLine("******************************");

        var subsets = buildTargets.ToSubSetString();

        var args = new List<string>
        {
            $"-subset {subsets}",
            $"-a {gConfig.Architecture}",
            $"-c {gConfig.Configuration}",
            $"-v:{Utils.DotNetVerbosity(gConfig.VerbosityLevel)}"
        };

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string crossbuild = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && gConfig.Architecture.Equals("arm64") &&
                RuntimeInformation.OSArchitecture != Architecture.Arm64)
                crossbuild = " /p:CrossBuild=true";
            // Avoids a message
            // 'The -ninja option has no effect on Windows builds since the Ninja generator is the default generator.'
            args.Add($"-ninja{crossbuild}");
        }

        ProcessStartInfo sInfo = new()
        {
            FileName = BuildScript,
            Arguments = args.AggregateWithSpace(),
            WorkingDirectory = Paths.RepoRoot
        };

        Utils.RunProcess(sInfo, gConfig);
    }

    public static void TestUnityPal(GlobalConfig gConfig)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        Console.WriteLine("******************************");
        Console.WriteLine("Unity: Running PAL tests");
        Console.WriteLine("******************************");
        ProcessStartInfo psi = new();
        psi.FileName = BuildScript;
        psi.Arguments = "clr.paltests";
        Utils.RunProcess(psi, gConfig);

        string osString = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "OSX" : "linux";
        NPath paltests = Paths.Artifacts.Combine("bin", "coreclr", $"{osString}.{gConfig.Architecture}.Debug", "paltests");
        psi.FileName = paltests.Combine("runpaltests.sh");
        psi.Arguments = paltests;
        Utils.RunProcess(psi, gConfig);
    }

    public static void TestUnityRuntime(GlobalConfig gConfig)
    {
        Console.WriteLine("******************************");
        Console.WriteLine("Unity: Running runtime tests");
        Console.WriteLine("******************************");
        ProcessStartInfo psi = new();
        psi.FileName = Paths.RepoRoot.Combine("src", "tests", BuildScript.FileName);
        psi.Arguments = $"{gConfig.Architecture} {gConfig.Configuration} ci";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            psi.Arguments =
                $"{psi.Arguments} tree baseservices tree interop tree reflection -- /p:LibrariesConfiguration={gConfig.Configuration}";
        else
            psi.Arguments =
                $"{psi.Arguments} /p:LibrariesConfiguration={gConfig.Configuration} -tree:baseservices -tree:interop -tree:reflection";
        Utils.RunProcess(psi, gConfig);

        psi.FileName = Paths.RepoRoot.Combine("src", "tests", "run").ChangeExtension(BuildScript.ExtensionWithDot);
        psi.Arguments = $"{gConfig.Architecture} {gConfig.Configuration}";
        Utils.RunProcess(psi, gConfig);
    }

    public static void TestClassLibraries(GlobalConfig gConfig)
    {
        Console.WriteLine("******************************");
        Console.WriteLine("Unity: Running class library tests");
        Console.WriteLine("******************************");

        var args = new List<string>
        {
            "-subset libs.tests",
            "-test /p:RunSmokeTestsOnly=true",
            $"-a {gConfig.Architecture}",
            $"-c {gConfig.Configuration}",
            $"-v:{Utils.DotNetVerbosity(gConfig.VerbosityLevel)}"
        };

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Avoids a message
            // 'The -ninja option has no effect on Windows builds since the Ninja generator is the default generator.'
            args.Add($"-ninja");
        }

        ProcessStartInfo psi = new();
        psi.FileName = BuildScript;
        psi.Arguments = args.AggregateWithSpace();
        psi.WorkingDirectory = Paths.RepoRoot;
        Utils.RunProcess(psi, gConfig);
    }
}
