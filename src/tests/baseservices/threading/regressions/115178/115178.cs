// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using Xunit;

/*
 * Issue description:
 User APCs can be queued to a thread that is waiting on a wait handle.
 This should not cancel the wait if the APC has not been queued by runtime
 as a result of internal interrupt handling.
*/

public class Test_wait_interrupted_user_apc
{
    public static bool Run115178Test => TestLibrary.Utilities.IsWindows;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentThread();

    [DllImport("kernel32.dll")]
    private static extern uint QueueUserAPC(
        IntPtr pfnAPC,
        IntPtr hThread,
        UIntPtr dwData);

    [DllImport("kernel32.dll")]
    private static extern bool DuplicateHandle(
        IntPtr hSourceProcessHandle,
        IntPtr hSourceHandle,
        IntPtr hTargetProcessHandle,
        out IntPtr lpTargetHandle,
        uint dwDesiredAccess,
        bool bInheritHandle,
        uint dwOptions);

    private delegate void ApcCallback(UIntPtr param);

    private static readonly ManualResetEventSlim waitEvent = new ManualResetEventSlim(false);

    private static readonly ManualResetEventSlim apcExecuted = new ManualResetEventSlim(false);

    private static IntPtr threadHandle;

    private static int result = 100;

    private static void OnApcCallback(UIntPtr param)
    {
        apcExecuted.Set();
    }

    private static void RunTestUsingInfiniteWait()
    {
        Console.WriteLine($"Running RunTestUsingInfiniteWait test.");

        var enterWait = new ManualResetEventSlim(false);
        var leaveWait = new ManualResetEventSlim(false);

        apcExecuted.Reset();
        waitEvent.Reset();

        new Thread(() =>
        {
            Console.WriteLine($"Starting thread waiting on event.");

            IntPtr pseudoHandle = GetCurrentThread();
            if (!DuplicateHandle(
                    GetCurrentProcess(),
                    GetCurrentThread(),
                    GetCurrentProcess(),
                    out threadHandle,
                    0,
                    false,
                    2))
            {
                Console.WriteLine($"Error duplicating handle, error code: {Marshal.GetLastWin32Error()}");
                result = 1;
            }

            enterWait.Set();

            bool signaled = waitEvent.Wait(Timeout.Infinite);
            if (!signaled)
            {
                Console.WriteLine($"Error waiting on event, unknown user APC canceled wait.");
                result = 2;
            }

            signaled = waitEvent.Wait(0);
            if (!signaled)
            {
                Console.WriteLine($"Error waiting on event, event should be signaled.");
                result = 3;
            }

            Console.WriteLine($"Stopping thread waiting on event.");
            leaveWait.Set();
        }).Start();

        ApcCallback callback = OnApcCallback;
        IntPtr pfnAPC = Marshal.GetFunctionPointerForDelegate(callback);

        Console.WriteLine($"Waiting for thread to enter wait...");
        enterWait.Wait(Timeout.Infinite);

        Console.WriteLine($"Queue user APC.");
        QueueUserAPC(pfnAPC, threadHandle, UIntPtr.Zero);

        Console.WriteLine($"Waiting for APC to execute...");
        apcExecuted.Wait(Timeout.Infinite);

        Console.WriteLine($"Signaling wait event.");
        waitEvent.Set();

        Console.WriteLine($"Waiting for thread to leave wait...");
        leaveWait.Wait(Timeout.Infinite);

        Console.WriteLine($"RunTestUsingInfiniteWait test executed.");

        GC.KeepAlive(callback);
    }

    private static void RunTestUsingTimedWait()
    {
        Console.WriteLine($"Running RunTestUsingTimedWait test.");

        var enterWait = new ManualResetEventSlim(false);
        var leaveWait = new ManualResetEventSlim(false);

        apcExecuted.Reset();
        waitEvent.Reset();

        new Thread(() =>
        {
            Console.WriteLine($"Starting thread waiting on event.");

            IntPtr pseudoHandle = GetCurrentThread();
            if (!DuplicateHandle(
                    GetCurrentProcess(),
                    GetCurrentThread(),
                    GetCurrentProcess(),
                    out threadHandle,
                    0,
                    false,
                    2))
            {
                Console.WriteLine($"Error duplicating handle, error code: {Marshal.GetLastWin32Error()}");
                result = 4;
            }

            enterWait.Set();

            apcExecuted.Wait(Timeout.Infinite);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            waitEvent.Wait(2000);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds < 2000)
            {
                Console.WriteLine($"Error waiting on event, wait returned too early.");
                result = 5;
            }

            Console.WriteLine($"Stopping thread waiting on event.");
            leaveWait.Set();
        }).Start();

        ApcCallback callback = OnApcCallback;
        IntPtr pfnAPC = Marshal.GetFunctionPointerForDelegate(callback);

        Console.WriteLine($"Waiting for thread to enter wait...");
        enterWait.Wait(Timeout.Infinite);

        Console.WriteLine($"Queue user APC's.");

        do
        {
            QueueUserAPC(pfnAPC, threadHandle, UIntPtr.Zero);
        }
        while (!leaveWait.Wait(100));

        Console.WriteLine($"Waiting for thread to leave wait...");
        leaveWait.Wait(Timeout.Infinite);

        Console.WriteLine($"RunTestUsingTimedWait test executed.");

        GC.KeepAlive(callback);
    }

    private static void RunTestInterruptInfiniteWait()
    {
        Console.WriteLine($"Running RunTestInterruptInfiniteWait test.");

        var enterWait = new ManualResetEventSlim(false);
        var leaveWait = new ManualResetEventSlim(false);

        apcExecuted.Reset();
        waitEvent.Reset();

        var waitThread = new Thread(() =>
        {
            Console.WriteLine($"Starting thread waiting on event.");

            IntPtr pseudoHandle = GetCurrentThread();
            if (!DuplicateHandle(
                    GetCurrentProcess(),
                    GetCurrentThread(),
                    GetCurrentProcess(),
                    out threadHandle,
                    0,
                    false,
                    2))
            {
                Console.WriteLine($"Error duplicating handle, error code: {Marshal.GetLastWin32Error()}");
                result = 6;
            }

            enterWait.Set();

            try
            {
                var signaled = waitEvent.Wait(Timeout.Infinite);
                if (!signaled)
                {
                    Console.WriteLine($"Error waiting on event, unknown user APC canceled wait.");
                    result = 7;
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadInterruptedException)
                {
                    Console.WriteLine($"Thread was interrupted as expected.");
                }
                else
                {
                    Console.WriteLine($"Unexpected exception: {ex.Message}");
                    result = 8;
                }
            }

            Console.WriteLine($"Stopping thread waiting on event.");
            leaveWait.Set();
        });

        waitThread.Start();

        ApcCallback callback = OnApcCallback;
        IntPtr pfnAPC = Marshal.GetFunctionPointerForDelegate(callback);

        Console.WriteLine($"Waiting for thread to enter wait...");
        enterWait.Wait(Timeout.Infinite);

        Console.WriteLine($"Queue user APC.");
        QueueUserAPC(pfnAPC, threadHandle, UIntPtr.Zero);

        Console.WriteLine($"Waiting for APC to execute...");
        apcExecuted.Wait(Timeout.Infinite);

        Console.WriteLine($"Interrupting thread wait...");
        waitThread.Interrupt();

        Thread.Sleep(200);

        Console.WriteLine($"Signaling wait event.");
        waitEvent.Set();

        Console.WriteLine($"Waiting for thread to leave wait...");
        leaveWait.Wait(Timeout.Infinite);

        Console.WriteLine($"RunTestInterruptInfiniteWait test executed.");

        GC.KeepAlive(callback);
    }

    [ConditionalFact(nameof(Run115178Test))]
    public static int TestEntryPoint()
    {
        RunTestUsingInfiniteWait();
        RunTestUsingTimedWait();
        RunTestInterruptInfiniteWait();
        return result;
    }
}
