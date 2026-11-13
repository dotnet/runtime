// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Runtime_135215
{
    private static int s_completed;

    [Fact]
    public static async Task AwaitUpcastCompletedTask()
    {
        await (Task)Task.FromResult(1);
    }

    [Fact]
    public static async Task AwaitUpcastSuspendingTask()
    {
        int before = s_completed;
        await (Task)YieldingTask(2);
        await (Task)RuntimeAsyncYieldingTask(3);
        await (Task)RuntimeAsyncCompletedTask(4);
        Assert.Equal(before + 3, s_completed);
    }

    [Fact]
    public static async Task AwaitUpcastTaskWithConfigureAwait()
    {
        await AwaitUpcastTaskConfigured();
    }

    [Fact]
    public static async Task AwaitUpcastTaskWithVariousResultTypes()
    {
        int before = s_completed;
        await (Task)RuntimeAsyncYieldingTask("abc");
        await (Task)RuntimeAsyncYieldingTask((1L, 2L, 3L));
        await (Task)RuntimeAsyncYieldingTask(Guid.NewGuid());
        await (Task)RuntimeAsyncYieldingTask(1.5);
        await AwaitUpcastGeneric("abc");
        await AwaitUpcastGeneric(new object());
        await AwaitUpcastGeneric(42);
        Assert.Equal(before + 7, s_completed);
    }

    [Fact]
    public static async Task AwaitTaskReturningUpcast()
    {
        int before = s_completed;
        await TaskReturningUpcast();
        await TaskReturningUpcastFromValueTask();
        await ValueTaskReturningUpcast();
        Assert.Equal(before + 3, s_completed);
    }

    private static async Task AwaitUpcastTaskConfigured()
    {
        int before = s_completed;
        await ((Task)Task.FromResult(1)).ConfigureAwait(false);
        await ((Task)YieldingTask(2)).ConfigureAwait(false);
        await ((Task)RuntimeAsyncYieldingTask(3)).ConfigureAwait(true);
        Assert.Equal(before + 2, s_completed);
    }

    private static async Task AwaitUpcastGeneric<T>(T value)
    {
        await (Task)RuntimeAsyncYieldingTask(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task TaskReturningUpcast() => RuntimeAsyncYieldingTask(5);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task TaskReturningUpcastFromValueTask() => ValueTaskYieldingTask(6).AsTask();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValueTask ValueTaskReturningUpcast() => new ValueTask(RuntimeAsyncYieldingTask(7));

    [RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> YieldingTask(int value)
    {
        await Task.Yield();
        Interlocked.Increment(ref s_completed);
        return value;
    }

    [RuntimeAsyncMethodGeneration(false)]
    private static async ValueTask<int> ValueTaskYieldingTask(int value)
    {
        await Task.Yield();
        Interlocked.Increment(ref s_completed);
        return value;
    }

    private static async Task<T> RuntimeAsyncYieldingTask<T>(T value)
    {
        await Task.Yield();
        Interlocked.Increment(ref s_completed);
        return value;
    }

    private static async Task<int> RuntimeAsyncCompletedTask(int value)
    {
        Interlocked.Increment(ref s_completed);
        return value;
    }
}
