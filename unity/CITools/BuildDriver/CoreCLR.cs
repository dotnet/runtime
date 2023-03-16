// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using NiceIO;

namespace BuildDriver;
public class CoreCLR
{
    private static NPath BuildScript => Paths.RepoRoot.Combine(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "build.cmd" : "build.sh");

    public static void Build(GlobalConfig gConfig)
    {
        Console.WriteLine("******************************");
        Console.WriteLine("Unity: Building CoreCLR runtime");
        Console.WriteLine("******************************");

        string crossbuild = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && gConfig.Architecture.Equals("arm64") &&
            RuntimeInformation.OSArchitecture != Architecture.Arm64)
            crossbuild = " /p:CrossBuild=true";

        ProcessStartInfo sInfo = new()
        {
            FileName = BuildScript,
            Arguments = $"-subset clr+libs -a {gConfig.Architecture} -c {gConfig.Configuration} -ci -ninja{crossbuild}",
            WorkingDirectory = Paths.RepoRoot
        };

        Utils.RunProcess(sInfo, gConfig);
    }

    public static void Test(GlobalConfig gConfig)
    {
        Console.WriteLine("******************************");
        Console.WriteLine("Unity: Running class library tests");
        Console.WriteLine("******************************");

        ProcessStartInfo psi = new();
        psi.FileName = BuildScript;
        psi.Arguments = $"-subset libs.tests -test /p:RunSmokeTestsOnly=true -a {gConfig.Architecture} -c {gConfig.Configuration} -ci -ninja";
        psi.WorkingDirectory = Paths.RepoRoot;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            psi.Environment.Add("LD_LIBRARY_PATH", "/usr/local/opt/openssl/lib");
        Utils.RunProcess(psi, gConfig);

        Console.WriteLine("******************************");
        Console.WriteLine("Unity: Running runtime tests");
        Console.WriteLine("******************************");
        psi.FileName = Paths.RepoRoot.Combine("src", "tests", BuildScript.FileName);
        psi.Arguments = $"{gConfig.Architecture} {gConfig.Configuration} ci";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            psi.Arguments =
                $"{psi.Arguments} tree baseservices tree interop tree reflection -- /p:LibrariesConfiguration={gConfig.Configuration}";
        else
            psi.Arguments =
                $"{psi.Arguments} /p:LibrariesConfiguration={gConfig.Configuration} -tree:baseservices -tree:interop -tree:reflection";
        psi.Environment.Remove("LD_LIBRARY_PATH"); // just in case
        Utils.RunProcess(psi, gConfig);

        psi.FileName = Paths.RepoRoot.Combine("src", "tests", "run").ChangeExtension(BuildScript.ExtensionWithDot);
        psi.Arguments = $"{gConfig.Architecture} {gConfig.Configuration}";
        Utils.RunProcess(psi, gConfig);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        Console.WriteLine("******************************");
        Console.WriteLine("Unity: Running PAL tests");
        Console.WriteLine("******************************");
        psi.FileName = BuildScript;
        psi.Arguments = "clr.paltests";
        Utils.RunProcess(psi, gConfig);

        string osString = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "OSX" : "Linux";
        NPath paltests = Paths.Artifacts.Combine("bin", "coreclr", $"{osString}.{gConfig.Architecture}.Debug", "paltests");
        psi.FileName = paltests.Combine("runpaltests.sh");
        psi.Arguments = paltests;
        Utils.RunProcess(psi, gConfig);
    }
}
