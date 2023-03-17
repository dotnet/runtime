// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BuildDriver;

public class EmbeddingHost
{
    private static string DotNet =>
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? "dotnet.sh"
            : "dotnet.cmd";

    public static void Build(GlobalConfig gConfig, bool test = false)
    {
        string type = test ? "test" : "build";
        Console.WriteLine("******************************");
        Console.WriteLine($"Unity: {type} embedding host");
        Console.WriteLine("******************************");

        ProcessStartInfo psi = new();
        psi.FileName = Paths.RepoRoot.Combine(DotNet);
        psi.Arguments = $"{type} unity/managed.sln -c {gConfig.Configuration} -v:{gConfig.DotNetVerbosity}";
        psi.WorkingDirectory = Paths.RepoRoot;

        Utils.RunProcess(psi, gConfig);
    }

    public static void Test(GlobalConfig gConfig)
    {
        // Run managed first
        Build(gConfig, true);

        // Tests fail on x86 currently, most likely due to not using the real GC yet.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            gConfig.Architecture.Equals("x86"))
        {
            Console.WriteLine("******************************");
            Console.WriteLine($"Skipping native embedding API tests on x86");
            Console.WriteLine("******************************");
            return;
        }

        // now native
        string args = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $". -A {Utils.WinArchitecture(gConfig.Architecture)}"
            : $"-DCMAKE_BUILD_TYPE={gConfig.Configuration}";
        ProcessStartInfo psi = new();
        psi.FileName = "cmake";
        psi.Arguments = args;
        psi.WorkingDirectory = Paths.UnityEmbedApiTests;

        Utils.RunProcess(psi, gConfig);

        psi.Arguments = "--build .";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            psi.Arguments = $"--build . --config {gConfig.Configuration}";
        Utils.RunProcess(psi, gConfig);


        psi.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Paths.UnityEmbedApiTests.Combine(gConfig.Configuration, "mono_test_app.exe")
            : Paths.UnityEmbedApiTests.Combine("mono_test_app");
        psi.Arguments = string.Empty;
        Utils.RunProcess(psi, gConfig);
    }
}
