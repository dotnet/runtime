// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2Synchronized
{
    [Fact]
    public static void TestEntryPoint()
    {
        TestEntryPointAsync().GetAwaiter().GetResult();
    }

    private static async Task TestEntryPointAsync()
    {
        Async2Synchronized p = new();
        Task t = p.Foo();
        // Returning the task from the [MethodImpl(MethodImplOptions.Synchronized)]
        // method Bar must release the lock it acquired, so it should not be held here.
        Assert.False(Monitor.IsEntered(typeof(Async2Synchronized)));
        await t;
    }

    private async Task Foo()
    {
        await Bar(Task.Delay(500));
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private Task Bar(Task t)
    {
        return t;
    }

    [Fact]
    public static void TestEntryPointValueTask()
    {
        TestEntryPointValueTaskAsync().GetAwaiter().GetResult();
    }

    private static async ValueTask TestEntryPointValueTaskAsync()
    {
        Async2Synchronized p = new();
        ValueTask t = p.FooValueTask();
        // Returning the ValueTask from the [MethodImpl(MethodImplOptions.Synchronized)]
        // method Bar must release the lock it acquired, so it should not be held here.
        Assert.False(Monitor.IsEntered(typeof(Async2Synchronized)));
        await t;
    }

    private async ValueTask FooValueTask()
    {
        await Bar(new ValueTask(Task.Delay(500)));
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private ValueTask Bar(ValueTask t)
    {
        return t;
    }
}
