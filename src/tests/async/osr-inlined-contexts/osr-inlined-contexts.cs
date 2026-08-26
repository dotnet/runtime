// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// Coverage for runtime async inlining in methods that are rejitted via OSR.
//
// An OSR method is entered with the tier 0 method's continuation, whose layout differs
// from the one the OSR method computes. Reading an inlined frame's state off it would be
// wrong, so this pins down that inlining async callees into an OSR method still behaves:
// an inlined callee's contexts are its own, and resuming inside one keeps every frame's
// contexts intact.
public class Async2OsrInlinedContexts
{
    private sealed class MarkerContext : SynchronizationContext
    {
        // Queue the callback and re-establish this context around it, as a real UI-style
        // context would. Running it inline instead would make an awaiting loop recurse
        // one frame deeper per iteration, since each suspension posts from inside the
        // previous callback.
        public override void Post(SendOrPostCallback d, object? state)
        {
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

    // Enough iterations to get the enclosing method rejitted via OSR.
    private const int Iterations = 100_000;

    private static int s_sideEffect;

    // Async, and so has its own context save and restore, but never suspends, so it is
    // inlinable into the loop below.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task BumpAsync(int value)
    {
        s_sideEffect += value;
    }

    private static async Task<int> LoopWithInlinedCallee()
    {
        int total = 0;
        for (int i = 0; i < Iterations; i++)
        {
            await BumpAsync(1);
            total += i;
        }

        return total;
    }

    [Fact]
    public static void InlinedCalleeDoesNotClobberContextsInOsrMethod()
    {
        SynchronizationContext original = SynchronizationContext.Current;
        MarkerContext marker = new MarkerContext();
        try
        {
            SynchronizationContext.SetSynchronizationContext(marker);

            LoopWithInlinedCallee().GetAwaiter().GetResult();

            // The inlined callee has its own context save and restore, which must not
            // disturb the caller's.
            Assert.Same(marker, SynchronizationContext.Current);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    private static readonly AsyncLocal<int> s_local = new AsyncLocal<int>();

    // Same, but the callee suspends, so the loop also exercises resuming inside an inlined
    // frame of an OSR method.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task YieldAsync()
    {
        await Task.Yield();
    }

    private static async Task<int> LoopWithSuspendingInlinedCallee()
    {
        SynchronizationContext.SetSynchronizationContext(new MarkerContext());
        s_local.Value = 7;

        int total = 0;
        for (int i = 0; i < 2_000; i++)
        {
            await YieldAsync();
            Assert.IsType<MarkerContext>(SynchronizationContext.Current);
            Assert.Equal(7, s_local.Value);
            total += i;
        }

        return total;
    }

    [Fact]
    public static void ResumingInsideInlinedFrameOfOsrMethodKeepsContexts()
    {
        SynchronizationContext original = SynchronizationContext.Current;
        try
        {
            LoopWithSuspendingInlinedCallee().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }
}
