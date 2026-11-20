// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public class ForwardActivationSignalToPreviousHandler
{
    private const string ChildEnvironmentVariable = "DOTNET_TEST_126260_CHILD";
    private const string NativeLibraryName = "nativetest126260";

    [DllImport(NativeLibraryName)]
    private static extern int IsHandlerInstalledBeforeRuntime();

    [DllImport(NativeLibraryName)]
    private static extern int GetHandlerCallCount();

    private static volatile bool s_spinning;
    private static volatile bool s_stopSpinning;

    /// <summary>
    /// Multiple runtimes in one process (for example CoreCLR and NativeAOT libraries) share the signal used to
    /// interrupt threads for GC suspension. Each runtime must forward every activation to the handler registered
    /// before it; otherwise a runtime that registered earlier never sees its own activations and its GC hangs.
    /// The child process preloads a native library that registers a handler before the runtime starts.
    /// </summary>
    [Fact]
    [SkipOnMono("Mono does not use the activation signal for thread suspension.")]
    public static void PreviousActivationHandlerIsInvoked()
    {
        if (Environment.GetEnvironmentVariable(ChildEnvironmentVariable) is null)
        {
            RunChildProcess();
            return;
        }

        if (IsHandlerInstalledBeforeRuntime() == 0)
        {
            Console.WriteLine("The native library was not preloaded before the runtime started; skipping.");
            return;
        }

        var spinner = new Thread(Spin);
        spinner.Start();
        while (!s_spinning)
        {
            Thread.Sleep(1);
        }

        for (int i = 0; i < 100; i++)
        {
            GC.Collect();
            // Let the spinner resume its loop so that the next GC has to interrupt it again.
            Thread.Sleep(1);
        }

        s_stopSpinning = true;
        spinner.Join();

        Assert.True(GetHandlerCallCount() > 0, "The previously registered activation signal handler was never invoked.");
    }

    private static void RunChildProcess()
    {
        string[] arguments = TestLibrary.Utilities.IsNativeAot
            ? []
            : [typeof(ForwardActivationSignalToPreviousHandler).Assembly.Location];

        var startInfo = new ProcessStartInfo(Environment.ProcessPath!, arguments);
        startInfo.Environment[ChildEnvironmentVariable] = "1";
        if (OperatingSystem.IsMacOS())
        {
            startInfo.Environment["DYLD_INSERT_LIBRARIES"] = Path.Combine(AppContext.BaseDirectory, $"lib{NativeLibraryName}.dylib");
        }
        else
        {
            startInfo.Environment["LD_PRELOAD"] = Path.Combine(AppContext.BaseDirectory, $"lib{NativeLibraryName}.so");
        }

        using Process child = Process.Start(startInfo)!;
        if (!child.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            child.Kill();
            Assert.Fail("The child process timed out.");
        }

        Assert.Equal(100, child.ExitCode);
    }

    // Compiled with full optimizations up front so the loop has no tier-0 patchpoints or other safe points:
    // a thread spinning here can only be suspended for GC by injecting an activation signal.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void Spin()
    {
        s_spinning = true;
        while (!s_stopSpinning)
        {
        }
    }
}
