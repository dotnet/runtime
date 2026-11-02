// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using TestLibrary;

public class ParallelCrashTester
{
    [SkipOnCoreClr("Temporarily disabled due to https://github.com/dotnet/runtime/issues/80356.", RuntimeTestModes.AnyGCStress)]
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/80356", typeof(Utilities), nameof(Utilities.IsMacOSX), nameof(Utilities.IsX64))]
    public static void ParallelCrashMainThread()
    {
        RunParallelCrash(1);
    }

    [SkipOnCoreClr("Temporarily disabled due to https://github.com/dotnet/runtime/issues/80356.", RuntimeTestModes.AnyGCStress)]
    [Fact]
    public static void ParallelCrashWorkerThreads()
    {
        RunParallelCrash(2);
    }

    [SkipOnCoreClr("Temporarily disabled due to https://github.com/dotnet/runtime/issues/80356.", RuntimeTestModes.AnyGCStress)]
    [Fact]
    public static void ParallelCrashMainThreadAndWorkerThreads()
    {
        RunParallelCrash(0);
    }

    private static void RunParallelCrash(int arg)
    {
        Console.WriteLine($"Running ParallelCrash test({arg})");
        ProcessStartInfo startInfo = new ProcessStartInfo(
            Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun"),
            $"ParallelCrash.dll {arg}");
        startInfo.InheritedHandles = [];
        // Disable crash diagnostics since the target process is expected to crash
        startInfo.Environment.Remove("DOTNET_DbgEnableMiniDump");
        startInfo.Environment.Remove("DOTNET_EnableCrashReport");
        ProcessExitStatus status = Process.Run(startInfo);

        int expectedExitCode = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? -2146232797 : 128 + 6);
        if (status.ExitCode != expectedExitCode)
        {
            throw new Exception($"Exit code = {status.ExitCode}, expected {expectedExitCode}");
        }
    }
}
