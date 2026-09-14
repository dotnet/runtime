// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2ExecutionContext
{
    [Fact]
    public static void TestDefaultFlow()
    {
        Test().GetAwaiter().GetResult();
    }

    [Fact]
    public static void TestSuppressedFlow()
    {
        TestNoFlowOuter().GetAwaiter().GetResult();
    }

    public static AsyncLocal<long?> s_local = new AsyncLocal<long?>();
    private static async Task Test()
    {
        s_local.Value = 42;
        await ChangeThenReturn();
        Assert.Equal(42, s_local.Value);

        try
        {
            s_local.Value = 43;
            await ChangeThenThrow();
        }
        catch (Exception)
        {
            Assert.Equal(43, s_local.Value);
        }

        s_local.Value = 44;
        await ChangeThenReturnInlined();
        Assert.Equal(44, s_local.Value);

        try
        {
            s_local.Value = 45;
            await ChangeThenThrowInlined();
        }
        catch (Exception)
        {
            Assert.Equal(45, s_local.Value);
        }

        s_local.Value = 46;
        await Task.Yield();
        Assert.Equal(46, s_local.Value);
    }

    private static async Task TestNoFlowOuter()
    {
        s_local.Value = 7;
        await TestNoFlowInner();
        // by default exec context should flow, even if inner frames suppress the flow
        Assert.Equal(7, s_local.Value);
    }

    private static async Task TestNoFlowInner()
    {
        ExecutionContext.SuppressFlow();

        s_local.Value = 42;
        // returns synchronously, context stays the same.
        await ChangeThenReturn();
        Assert.Equal(42, s_local.Value);

        // returns asynchronously, context should not flow.
        // the value is technically nondeterministic,
        // but in our current implementation it will be 12345
        await ChangeYieldThenReturn();
        Assert.Equal(12345, s_local.Value);

        // NB: no need to restore flow here as we will
        //     be popping to the parent context anyways.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ChangeThenThrow()
    {
        s_local.Value = 123;
        throw new Exception();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ChangeThenReturn()
    {
        s_local.Value = 123;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ChangeYieldThenReturn()
    {
        s_local.Value = 12345;
        // restore flow so that state is not cleared by Yield
        ExecutionContext.RestoreFlow();
        await Task.Yield();
        Assert.Equal(12345, s_local.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task ChangeThenThrowInlined()
    {
        s_local.Value = 123;
        throw new Exception();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task ChangeThenReturnInlined()
    {
        s_local.Value = 123;
    }

    [Fact]
    public static int TestRestoreTier0ContextInOsr()
    {
        return TestRestoreTier0ContextInOsrAsync().GetAwaiter().GetResult();
    }

    private static AsyncLocal<int> s_osrLocal = new AsyncLocal<int>();
    private static async Task<int> TestRestoreTier0ContextInOsrAsync()
    {
        s_osrLocal.Value = 100;

        await LoopWithOsrTransition();

        return s_osrLocal.Value;
    }

    private static async Task LoopWithOsrTransition()
    {
        s_osrLocal.Value = 101;

        int val = 0;
        for (int i = 0; i < 10005; i++)
        {
            val += i;
            if (i > 10000)
            {
                await Task.Delay(50);
            }
        }

        s_osrLocal.Value = val;
    }

    [Fact]
    public static void TestValueTask()
    {
        TestValueTaskAsync().GetAwaiter().GetResult();
    }

    private static async ValueTask TestValueTaskAsync()
    {
        s_local.Value = 42;
        await ChangeThenReturnValueTask();
        Assert.Equal(42, s_local.Value);
    }

    private static async ValueTask ChangeThenReturnValueTask()
    {
        s_local.Value = 123;
    }

    private static AsyncLocal<int> s_leakLocal = new AsyncLocal<int>();

    // Regression test: a callee that changes the ExecutionContext and then
    // suspends must not leak the change to its synchronous caller through the
    // task-returning thunk. The check must happen in the caller's synchronous
    // region (before the caller itself suspends); a suspension in the caller
    // would otherwise mask the leak via the caller's own context restore.
    [Fact]
    public static void TestNoExecutionContextLeakOnSuspension()
    {
        TestNoExecutionContextLeakOnSuspensionAsync().GetAwaiter().GetResult();
    }

    private static async Task TestNoExecutionContextLeakOnSuspensionAsync()
    {
        s_leakLocal.Value = 100;

        var tcs = new TaskCompletionSource();
        Task callee = ChangeExecutionContextThenSuspend(tcs.Task);

        // The callee changed the AsyncLocal and then suspended. That change must
        // not have leaked out to us through the task-returning thunk.
        Assert.Equal(100, s_leakLocal.Value);

        tcs.SetResult();
        await callee;

        Assert.Equal(100, s_leakLocal.Value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ChangeExecutionContextThenSuspend(Task toAwait)
    {
        s_leakLocal.Value = 999;
        await toAwait;
    }
}
