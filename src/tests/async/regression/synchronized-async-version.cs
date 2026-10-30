// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2Synchronized
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public static async Task FaultedAwaitable(bool useValueTask, bool callerHoldsLock)
    {
        Async2Synchronized p = new();
        InvalidOperationException expected = new("boom");
        Task task = Task.FromException(expected);

        if (callerHoldsLock)
        {
            Monitor.Enter(p);
        }

        try
        {
            Task faulted = useValueTask ? p.FooValueTask(new ValueTask(task)).AsTask() : p.Foo(task);
            Assert.True(faulted.IsFaulted);
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => faulted);

            Assert.Same(expected, actual);
            Assert.Equal(callerHoldsLock, Monitor.IsEntered(p));
        }
        finally
        {
            if (Monitor.IsEntered(p))
            {
                Monitor.Exit(p);
            }
        }
    }

    [Fact]
    public static async Task TestEntryPointAsync()
    {
        Async2Synchronized p = new();
        TaskCompletionSource tcs = new();
        Task t = p.Foo(tcs.Task);
        // Returning the task from the [MethodImpl(MethodImplOptions.Synchronized)]
        // method Bar must release the lock it acquired, so it should not be held here.
        Assert.False(Monitor.IsEntered(p));
        tcs.SetResult();
        await t;
    }

    private async Task Foo(Task task)
    {
        await Bar(task);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private Task Bar(Task t)
    {
        return t;
    }

    [Fact]
    public static async ValueTask TestEntryPointValueTaskAsync()
    {
        Async2Synchronized p = new();
        TaskCompletionSource tcs = new();
        ValueTask t = p.FooValueTask(new ValueTask(tcs.Task));
        // Returning the ValueTask from the [MethodImpl(MethodImplOptions.Synchronized)]
        // method Bar must release the lock it acquired, so it should not be held here.
        Assert.False(Monitor.IsEntered(p));
        tcs.SetResult();
        await t;
    }

    private async ValueTask FooValueTask(ValueTask task)
    {
        await Bar(task);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private ValueTask Bar(ValueTask t)
    {
        return t;
    }
}
