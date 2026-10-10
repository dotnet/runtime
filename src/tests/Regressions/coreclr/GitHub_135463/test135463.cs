// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TestLibrary;
using Xunit;

// ControlledExecution.Run is the API that aborts a thread
#pragma warning disable SYSLIB0046

public class Runtime_135463
{
    private static volatile bool s_readyForCancellation;
    private static volatile int s_counter;
    private static string s_trace;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunInfiniteLoop()
    {
        while (true)
        {
            if ((++s_counter & 0xfffff) == 0)
            {
                Thread.Sleep(0);
            }
        }
    }

    private static void WaitUntilAbortIsRequested()
    {
        while ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) == 0)
        {
            Thread.Sleep(10);
        }
    }

    // The abort is thrown again at the end of the catch block that swallowed it
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CatchAbort()
    {
        try
        {
            s_readyForCancellation = true;
            RunInfiniteLoop();
        }
        catch (ThreadAbortException)
        {
            s_trace += "catch ";
        }

        s_trace += "unreachable ";
    }

    // The caller catches the abort that was thrown again at the end of the catch block in its callee
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CatchAbortInCaller()
    {
        try
        {
            CatchAbort();
        }
        catch (ThreadAbortException)
        {
            s_trace += "caller catch ";
        }

        s_trace += "unreachable ";
    }

    // A thread is not aborted while it runs a finally block, so the abort stays pending at the end of a catch block
    // that is inside of one
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CatchInsideOfFinally()
    {
        try
        {
            try
            {
                throw new InvalidOperationException();
            }
            finally
            {
                s_readyForCancellation = true;
                WaitUntilAbortIsRequested();

                try
                {
                    throw new ArgumentException();
                }
                catch (ArgumentException)
                {
                    s_trace += "catch in finally ";
                }

                s_trace += "end of finally ";
            }
        }
        catch (InvalidOperationException)
        {
            s_trace += "outer catch ";
        }

        s_trace += "unreachable ";
    }

    private static void CancelWhenReady(CancellationTokenSource cts)
    {
        while (!s_readyForCancellation)
        {
            Thread.Sleep(10);
        }

        cts.Cancel();
    }

    // Runs the action, aborts it once it is ready and checks which of its blocks ran
    private static void RunAndAbort(string name, Action action, string expectedTrace)
    {
        Console.WriteLine(name);
        s_trace = "";
        s_readyForCancellation = false;

        var cts = new CancellationTokenSource();
        Task.Run(() => CancelWhenReady(cts));

        Exception exception = null;
        try
        {
            ControlledExecution.Run(action, cts.Token);
        }
        catch (Exception e)
        {
            exception = e;
        }

        Assert.IsType<OperationCanceledException>(exception);
        Assert.Equal(expectedTrace, s_trace);
    }

    // The action aborts itself. CancellationTokenSource.Cancel catches the abort and it is thrown again at the end of
    // that catch block, in compiled code when only user code is interpreted.
    private static void CancelItself()
    {
        Console.WriteLine(nameof(CancelItself));
        s_trace = "";

        var cts = new CancellationTokenSource();
        Exception exception = null;
        try
        {
            ControlledExecution.Run(() =>
            {
                cts.Cancel();
                s_trace += "unreachable ";
            }, cts.Token);
        }
        catch (Exception e)
        {
            exception = e;
        }

        Assert.IsType<OperationCanceledException>(exception);
        Assert.Equal("", s_trace);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void TestEntryPoint()
    {
        RunAndAbort(nameof(CatchAbort), CatchAbort, "catch ");
        RunAndAbort(nameof(CatchAbortInCaller), CatchAbortInCaller, "catch caller catch ");
        RunAndAbort(nameof(CatchInsideOfFinally), CatchInsideOfFinally, "catch in finally end of finally outer catch ");
        CancelItself();
    }
}
