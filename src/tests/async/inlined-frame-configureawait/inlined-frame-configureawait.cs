// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// A frame that awaited with ConfigureAwait(false) does not want to be brought back onto
// the SynchronizationContext that was current when it suspended. That has to hold for the
// logical return of an inlined async frame too.
public class Async2InlinedFrameConfigureAwait
{
    private sealed class TrackingContext : SynchronizationContext
    {
        public int Posts;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref Posts);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                SynchronizationContext currentCtx = Current;
                SetSynchronizationContext(this);
                try
                {
                    d(state);
                }
                finally
                {
                    SetSynchronizationContext(currentCtx);
                }
            });
        }
    }

    private struct AlwaysThreadPoolAwaitable : INotifyCompletion
    {
        public bool IsCompleted => false;

        public void OnCompleted(Action continuation) => ThreadPool.QueueUserWorkItem(_ => continuation());

        public void GetResult()
        {
        }

        public AlwaysThreadPoolAwaitable GetAwaiter() => this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task Suspends()
    {
        await new AlwaysThreadPoolAwaitable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task MiddleConfigured()
    {
        await Suspends().ConfigureAwait(false);
    }

    private static async Task OuterConfigured()
    {
        await MiddleConfigured().ConfigureAwait(false);
    }

    [Fact]
    public static async Task ConfiguredAwaitDoesNotReturnToContext()
    {
        TrackingContext tracking = new TrackingContext();
        SynchronizationContext.SetSynchronizationContext(tracking);

#pragma warning disable xUnit1030 // Capturing the test context would add a post and invalidate the assertion.
        await OuterConfigured().ConfigureAwait(false);
#pragma warning restore xUnit1030
        Assert.Equal(0, tracking.Posts);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task LoopMiddleConfigured(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            await Suspends().ConfigureAwait(false);
        }
    }

    private static async Task LoopOuterConfigured(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            await LoopMiddleConfigured(iterations).ConfigureAwait(false);
        }
    }

    [Fact]
    public static async Task ConfiguredAwaitInLoopDoesNotReturnToContext()
    {
        TrackingContext tracking = new TrackingContext();
        SynchronizationContext.SetSynchronizationContext(tracking);

#pragma warning disable xUnit1030 // Capturing the test context would add a post and invalidate the assertion.
        await LoopOuterConfigured(5).ConfigureAwait(false);
#pragma warning restore xUnit1030
        Assert.Equal(0, tracking.Posts);
    }
}
