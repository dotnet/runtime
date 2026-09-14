// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Xunit;

public class Async2TaskAdapters
{
    // Wrapping a runtime-async Task in a ValueTask (ValueTask(Task) constructor) and
    // awaiting the resulting ValueTask should suspend and resume correctly.
    [Fact]
    public static void TestTaskToValueTask()
    {
        SynchronizationContext prevContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new MySyncContext());
            WrapTaskInValueTaskCaller().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevContext);
        }
    }

    private static async Task WrapTaskInValueTaskCaller()
    {
        await WrapTaskInValueTask();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValueTask WrapTaskInValueTask()
    {
        return new ValueTask(YieldingTask());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task YieldingTask()
    {
        await Task.Yield();
    }

    // Converting a ValueTask backed by an IValueTaskSource into a Task via AsTask()
    // and awaiting it should complete correctly.
    [Fact]
    public static void TestValueTaskSourceToTask()
    {
        SynchronizationContext prevContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new MySyncContext());
            ConvertSourceToTaskCaller().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevContext);
        }
    }

    private static async Task ConvertSourceToTaskCaller()
    {
        await ConvertSourceToTask();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task ConvertSourceToTask()
    {
        return SourceValueTask().AsTask();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValueTask SourceValueTask()
    {
        return new ValueTask(new Source(), 1);
    }

    // Verifies that the two ValueTask<Task<int>> constructor overloads are handled correctly:
    //   - return new ValueTask<Task<int>>(TaskOfIntReturningFunction()) passes a Task<int>, which binds
    //     to the ValueTask<TResult>(TResult result) constructor, wrapping the Task<int> as a value.
    //   - return new ValueTask<Task<int>>(TaskOfTaskOfIntReturningFunction()) passes a Task<Task<int>>,
    //     which binds to the ValueTask<TResult>(Task<TResult> task) constructor, an actual async call.
    [Fact]
    public static void TestValueTaskOfTaskValueVersusAsync()
    {
        WrapValueVersusAsync().GetAwaiter().GetResult();
    }

    private static async Task WrapValueVersusAsync()
    {
        // Value case: a suspended Task<int> is passed to the ValueTask<Task<int>>(TResult result)
        // constructor, so the ValueTask holds it as an already-available value. The ValueTask is
        // completed even while the inner Task<int> is still suspended.
        TaskCompletionSource valueGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ValueTask<Task<int>> valueCase = WrapTaskValueCaller(valueGate);
        Assert.True(valueCase.IsCompletedSuccessfully);
        Task<int> innerFromValue = await valueCase;
        Assert.False(innerFromValue.IsCompleted);
        valueGate.SetResult();
        Assert.Equal(42, await innerFromValue);

        // Async case: a suspended Task<Task<int>> is passed to the ValueTask<Task<int>>(Task<TResult> task)
        // constructor, so the ValueTask is backed by that task and is not completed until the outer
        // task completes.
        TaskCompletionSource asyncGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ValueTask<Task<int>> asyncCase = WrapTaskTaskCaller(asyncGate);
        Assert.False(asyncCase.IsCompleted);
        asyncGate.SetResult();
        Task<int> innerFromAsync = await asyncCase;
        Assert.Equal(123, await innerFromAsync);
    }

    private static async ValueTask<Task<int>> WrapTaskValueCaller(TaskCompletionSource gate)
    {
        return await WrapTaskValue(gate);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValueTask<Task<int>> WrapTaskValue(TaskCompletionSource gate)
    {
        return new ValueTask<Task<int>>(TaskOfIntReturningFunction(gate));
    }

    private static async ValueTask<Task<int>> WrapTaskTaskCaller(TaskCompletionSource gate)
    {
        return await WrapTaskTask(gate);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValueTask<Task<int>> WrapTaskTask(TaskCompletionSource gate)
    {
        return new ValueTask<Task<int>>(TaskOfTaskOfIntReturningFunction(gate));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> TaskOfIntReturningFunction(TaskCompletionSource gate)
    {
        await gate.Task;
        return 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<Task<int>> TaskOfTaskOfIntReturningFunction(TaskCompletionSource gate)
    {
        await gate.Task;
        return Task.FromResult(123);
    }

    private class Source : IValueTaskSource
    {
        public void GetResult(short token)
        {
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return ValueTaskSourceStatus.Pending;
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            Assert.Equal(ValueTaskSourceOnCompletedFlags.None, flags);
            continuation(state);
        }
    }

    private class MySyncContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                SynchronizationContext prevContext = Current;
                try
                {
                    SetSynchronizationContext(this);
                    d(state);
                }
                finally
                {
                    SetSynchronizationContext(prevContext);
                }
            }, null);
        }
    }
}
