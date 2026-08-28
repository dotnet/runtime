// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

// Shapes where the JIT can prove that the "resumed" indicator is 1 at a
// suspension point: an await that always suspends dominates a later await, with
// no merge in between. The helpers emitted on the later await's suspension path
// (RestoreContextsOnSuspension, the inlined frame transition captures) no-op
// when the indicator is set, so they could be folded away entirely.
public class Async2ResumedKnownTrue
{
    [Fact]
    public static void TestTwoYields()
    {
        TwoYields().GetAwaiter().GetResult();
    }

    [Fact]
    public static void TestYieldThenAwaitTask()
    {
        YieldThenAwaitTask().GetAwaiter().GetResult();
    }

    [Fact]
    public static void TestYieldThenAwaitInlinedFrame()
    {
        YieldThenAwaitInlinedFrame().GetAwaiter().GetResult();
    }

    [Fact]
    public static void TestYieldThenAwaitResumedInlinedFrame()
    {
        YieldThenAwaitResumedInlinedFrame().GetAwaiter().GetResult();
    }

    // The second Yield's suspension path sees the indicator defined by the
    // first, which always suspends.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task TwoYields()
    {
        await Task.Yield();
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task YieldThenAwaitTask()
    {
        await Task.Yield();
        await Task.Delay(1);
    }

    // Same, but the later await sits in an inlined async frame, so its
    // suspension tail runs the enclosing frames' transition captures too.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task YieldThenAwaitInlinedFrame()
    {
        await Task.Yield();
        await Inner();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task Inner()
    {
        await Task.Delay(1);
    }

    // Here the inlined frame's own indicator is set as well by the time it suspends, so
    // none of the frame transition handling is needed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task YieldThenAwaitResumedInlinedFrame()
    {
        await Task.Yield();
        await InnerYieldThenAwait();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task InnerYieldThenAwait()
    {
        await Task.Yield();
        await Task.Delay(1);
    }
}
