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
    // Set by a call-free store right after a catch while an abort is pending. A call there (like the string
    // concatenation of s_trace) would carry its own safepoint, where the pending abort can be raised even if
    // nothing raised it again at the end of the catch.
    private static volatile bool s_reachedAfterCatch;

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

        s_reachedAfterCatch = true;
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

        s_reachedAfterCatch = true;
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

        s_reachedAfterCatch = true;
    }

    // The abort is requested while a catch runs inside of another catch handler. The inner catch returns into the
    // outer catch handler, where the abort stays pending, so it is thrown again at the end of the outer catch.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CatchInsideOfCatch()
    {
        try
        {
            throw new InvalidOperationException();
        }
        catch (InvalidOperationException)
        {
            try
            {
                throw new ArgumentException();
            }
            catch (ArgumentException)
            {
                s_readyForCancellation = true;
                WaitUntilAbortIsRequested();
                s_trace += "inner catch ";
            }

            s_trace += "end of outer catch ";
        }

        s_reachedAfterCatch = true;
    }

    // The leave of the inner catch exits the outer catch as well
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LeaveOutOfNestedCatch()
    {
        try
        {
            throw new InvalidOperationException();
        }
        catch (InvalidOperationException)
        {
            try
            {
                throw new ArgumentException();
            }
            catch (ArgumentException)
            {
                s_readyForCancellation = true;
                WaitUntilAbortIsRequested();
                s_trace += "inner catch ";
                goto Done;
            }
        }

    Done:
        s_reachedAfterCatch = true;
    }

    // The leave of the inner catch exits the enclosing try as well, so the abort thrown again at the end of the
    // inner catch is caught by the enclosing catch
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LeaveOutOfEnclosingTry()
    {
        try
        {
            try
            {
                s_readyForCancellation = true;
                RunInfiniteLoop();
            }
            catch (ThreadAbortException)
            {
                s_trace += "inner catch ";
                goto Done;
            }
        }
        catch (ThreadAbortException)
        {
            s_trace += "outer catch ";
        }

    Done:
        s_reachedAfterCatch = true;
    }

    // The inner catch returns to the head of the loop, which is the start of both the inner try and the enclosing
    // try. The abort thrown again at the end of the inner catch is caught by the enclosing catch, not by the inner
    // catch of the next iteration.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CatchInLoop()
    {
        int count = 0;
        try
        {
            while (true)
            {
                try
                {
                    s_readyForCancellation = true;
                    RunInfiniteLoop();
                }
                catch (ThreadAbortException)
                {
                    s_trace += "catch ";
                    if (++count == 3)
                    {
                        break;
                    }
                }
            }
        }
        catch (ThreadAbortException)
        {
            s_trace += "outer catch ";
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
    private static void RunAndAbort(string name, Action action, string expectedTrace)
    {
        Console.WriteLine(name);
        s_trace = "";
        s_readyForCancellation = false;
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
        Assert.False(s_reachedAfterCatch);
    }

    // ControlledExecution is supported on CoreCLR only
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void TestEntryPoint()
    {
        RunAndAbort(nameof(CatchAbort), CatchAbort, "catch ");
        RunAndAbort(nameof(CatchAbortInCaller), CatchAbortInCaller, "catch caller catch ");
        RunAndAbort(nameof(CatchInsideOfFinally), CatchInsideOfFinally, "catch in finally end of finally outer catch ");
        RunAndAbort(nameof(CatchInsideOfCatch), CatchInsideOfCatch, "inner catch end of outer catch ");
        RunAndAbort(nameof(LeaveOutOfNestedCatch), LeaveOutOfNestedCatch, "inner catch ");
    }

    // A positive InterpMode, or a runtime without a JIT, runs these methods in the interpreter.
    // DOTNET_Interpreter alone does not: RunInterpreter without InterpMode selects only the merged
    // runner's assembly, and this one stays jitted.
    public static bool IsJitted =>
        RuntimeFeature.IsDynamicCodeCompiled &&
        !(int.TryParse(Environment.GetEnvironmentVariable("DOTNET_InterpMode") ??
                           Environment.GetEnvironmentVariable("COMPlus_InterpMode"), out int mode) && mode > 0);

    // Jitted code runs these two only with the fix for https://github.com/dotnet/runtime/issues/135637
    [ActiveIssue("https://github.com/dotnet/runtime/issues/135637", typeof(Runtime_135463), nameof(IsJitted))]
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void TestEnclosingTry()
    {
        RunAndAbort(nameof(LeaveOutOfEnclosingTry), LeaveOutOfEnclosingTry, "inner catch outer catch ");
        RunAndAbort(nameof(CatchInLoop), CatchInLoop, "catch outer catch ");
    }
}
