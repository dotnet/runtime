// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BuildDriver;
public class BuildCoreCLR : BuildDriver
{
    public static void Run(GlobalConfig gConfig)
    {
        Console.WriteLine("******************************");
        Console.WriteLine("Unity: Building CoreCLR runtime");
        Console.WriteLine("******************************");

        string build = "build.cmd";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            build = "build.sh";

        string crossbuild = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && gConfig.Architecture.Equals("arm64") &&
            RuntimeInformation.OSArchitecture != Architecture.Arm64)
            crossbuild = " /p:CrossBuild=true";

        ProcessStartInfo sInfo = new ProcessStartInfo()
        {
            FileName = Paths.RepoRoot.Combine(build),
            Arguments = $"-subset clr+libs -a {gConfig.Architecture} -c {gConfig.Configuration} -ci -ninja{crossbuild}",
            WorkingDirectory = Paths.RepoRoot
        };

        RunProcess(sInfo, gConfig);
    }
}
