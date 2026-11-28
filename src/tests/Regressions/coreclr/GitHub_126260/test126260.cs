// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime;
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
    /// The child process preloads a native library that registers a handler before the runtime starts, then
    /// interrupts a spinning thread both for GC suspension and for an abort.
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
            // The loader ignores DYLD_INSERT_LIBRARIES for binaries with the hardened runtime enabled, so the
            // preload is not guaranteed on macOS. LD_PRELOAD is always honored on Linux.
            Assert.True(OperatingSystem.IsMacOS(), "The native library was not preloaded before the runtime started.");
            Console.WriteLine("The native library was not preloaded before the runtime started; skipping.");
            return;
        }

        if (TestLibrary.Utilities.IsCoreClrInterpreter)
        {
            // Interpreted loops have GC safe points, so suspending or aborting a spinning thread doesn't need an activation.
            Console.WriteLine("Running under the interpreter; skipping.");
            return;
        }

        SuspendSpinningThreadForGC();
        Assert.True(GetHandlerCallCount() > 0, "The previously registered activation signal handler was never invoked.");

        // An activation that aborts the thread doesn't return from the activation handler, so it must still be forwarded.
        int callsBeforeAbort = GetHandlerCallCount();
        AbortSpinningThread();
        Assert.True(GetHandlerCallCount() > callsBeforeAbort, "The activation that aborted the thread was not forwarded to the previously registered handler.");
    }

    private static void SuspendSpinningThreadForGC()
    {
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
    }

    private static void AbortSpinningThread()
    {
        using var cts = new CancellationTokenSource();
#pragma warning disable SYSLIB0046 // ControlledExecution.Run is obsolete
        Assert.Throws<OperationCanceledException>(() => ControlledExecution.Run(() =>
        {
            cts.CancelAfter(100);
            SpinForever();
        }, cts.Token));
#pragma warning restore SYSLIB0046
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

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static void SpinForever()
    {
        while (true)
        {
        }
    }
}
