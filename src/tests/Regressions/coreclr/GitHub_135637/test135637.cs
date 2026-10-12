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

// A thread abort that is raised again after a catch block has swallowed it is dispatched from the address the catch
// returns to. The block the catch returns to has to generate code of its own when it ends an enclosing EH region,
// otherwise that address belongs to the code that follows it and the abort reaches different handlers.
public class Runtime_135637
{
    private static volatile bool s_readyForCancellation;
    private static volatile int s_counter;
    private static string s_trace;
    // Set by call-free stores, so that what is observed does not depend on a call after a catch getting to run
    // while the abort is pending.
    private static volatile bool s_finallyRan;
    private static volatile bool s_newTryRan;
    private static volatile bool s_reachedAfterCatch;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunInfiniteLoop()
    {
        s_readyForCancellation = true;
        while (true)
        {
            if ((++s_counter & 0xfffff) == 0)
            {
                Thread.Sleep(0);
            }
        }
    }

    // The catch's leave passes through a finally before it reaches the code after the try/finally
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CatchThenFinally()
    {
        try
        {
            try
            {
                RunInfiniteLoop();
            }
            catch (ThreadAbortException)
            {
                s_trace += "catch ";
            }
            finally
            {
                s_finallyRan = true;
                s_trace += "finally ";
            }
        }
        catch (ThreadAbortException)
        {
            s_trace += "outer ";
        }

        s_reachedAfterCatch = true;
    }

    // The catch's leave target is outside the enclosing try
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LeaveOutOfOuterTry()
    {
        try
        {
            try
            {
                RunInfiniteLoop();
            }
            catch (ThreadAbortException)
            {
                s_trace += "inner ";
                goto Done;
            }
        }
        catch (ThreadAbortException)
        {
            s_trace += "outer ";
        }

    Done:
        s_reachedAfterCatch = true;
    }

    // The code after the catch is the start of another try
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConsecutiveTry()
    {
        try
        {
            RunInfiniteLoop();
        }
        catch (ThreadAbortException)
        {
            s_trace += "inner ";
        }

        try
        {
            s_newTryRan = true;
            s_trace += "new try ";
        }
        catch (ThreadAbortException)
        {
            s_trace += "new catch ";
        }

        s_reachedAfterCatch = true;
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
    private static void RunAndAbort(string name, Action action, string expectedTrace, bool expectedFinallyRan)
    {
        Console.WriteLine(name);
        s_trace = "";
        s_readyForCancellation = false;
        s_finallyRan = false;
        s_newTryRan = false;
        s_reachedAfterCatch = false;

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
        Assert.Equal(expectedFinallyRan, s_finallyRan);
        Assert.False(s_newTryRan);
        Assert.False(s_reachedAfterCatch);
    }

    // The handlers that see the abort raised again after a catch follow from the code the JIT lays out for the
    // catch's return. Under the interpreter the outer catch of LeaveOutOfOuterTry does not run.
    public static bool IsTestCodeCompiled => PlatformDetection.IsMultithreadingSupported && !Utilities.IsCoreClrInterpreter;

    [ConditionalFact(typeof(Runtime_135637), nameof(IsTestCodeCompiled))]
    public static void TestEntryPoint()
    {
        RunAndAbort(nameof(CatchThenFinally), CatchThenFinally, "catch finally outer ", expectedFinallyRan: true);
        RunAndAbort(nameof(LeaveOutOfOuterTry), LeaveOutOfOuterTry, "inner outer ", expectedFinallyRan: false);
        RunAndAbort(nameof(ConsecutiveTry), ConsecutiveTry, "inner ", expectedFinallyRan: false);
    }
}
