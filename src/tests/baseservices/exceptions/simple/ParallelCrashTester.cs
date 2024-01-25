// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

public class ParallelCrashTester
{
    [Fact]
    public static void ParallelCrashMainThread()
    {
        RunParallelCrash(1);
    }

    [Fact]
    public static void ParallelCrashWorkerThreads()
    {
        RunParallelCrash(2);
    }

    [Fact]
    public static void ParallelCrashMainThreadAndWorkerThreads()
    {
        RunParallelCrash(0);
    }

    private static void RunParallelCrash(int arg)
    {
        Console.WriteLine($"Running ParallelCrash test({arg})");
        Process testProcess = new Process();

        testProcess.StartInfo.FileName = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun");
        testProcess.StartInfo.Arguments = $"ParallelCrash.dll {arg}";
        testProcess.StartInfo.UseShellExecute = false;
        // Disable creating dump since the target process is expected to crash
        testProcess.StartInfo.Environment.Remove("DOTNET_DbgEnableMiniDump");
        testProcess.Start();
        testProcess.WaitForExit();

        int expectedExitCode = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? -2146232797 : 128 + 6);
        if (testProcess.ExitCode != expectedExitCode)
        {
            throw new Exception($"Exit code = {testProcess.ExitCode}, expected {expectedExitCode}");
        }
    }
}
