// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BuildDriver;

public class BuildEmbeddingHost : BuildDriver
{
    public static void Run(GlobalConfig gConfig)
    {
        Console.WriteLine("******************************");
        Console.WriteLine("Unity: Building embedding host");
        Console.WriteLine("******************************");

        ProcessStartInfo psi = new();
        psi.FileName = "dotnet.cmd";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            psi.FileName = "dotnet.sh";
        psi.FileName = Paths.RepoRoot.Combine(psi.FileName);
        psi.Arguments = $"build unity/managed.sln -c {gConfig.Configuration}";
        psi.WorkingDirectory = Paths.RepoRoot;

        RunProcess(psi, gConfig);
    }
}
