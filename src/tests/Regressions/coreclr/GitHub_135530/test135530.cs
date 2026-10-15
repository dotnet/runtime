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

public class Runtime_135530
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Nop()
    {
    }

    // The abort is thrown again at the end of the inner catch block, inside the outer try block of the same method
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CatchAbortAgainInSameMethod()
    {
        try
        {
            Nop();

            try
            {
                s_readyForCancellation = true;
                RunInfiniteLoop();
            }
            catch (ThreadAbortException)
            {
                s_trace += "inner catch ";
            }

            Nop();
            s_trace += "unreachable ";
        }
        catch (ThreadAbortException)
        {
            s_trace += "outer catch ";
        }
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/135463", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsCoreClrInterpreter))]
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void TestEntryPoint()
    {
        var cts = new CancellationTokenSource();
        Task.Run(() =>
        {
            while (!s_readyForCancellation)
            {
                Thread.Sleep(10);
            }

            cts.Cancel();
        });

        s_trace = "";
        Exception exception = null;
        try
        {
            ControlledExecution.Run(CatchAbortAgainInSameMethod, cts.Token);
        }
        catch (Exception e)
        {
            exception = e;
        }

        Assert.IsType<OperationCanceledException>(exception);
        Assert.Equal("inner catch outer catch ", s_trace);
    }
}
