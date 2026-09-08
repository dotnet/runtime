// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Xunit;

// The async version of a non-async task returning method forwards through a transparent
// await, and that await runs in whatever frame it ends up in rather than in one of its
// own. Once such a version is inlined into an async frame that was itself inlined, the
// await has to take the enclosing frame's contexts, so that a suspension records that the
// frame resumed. Getting that wrong leaves the enclosing frame believing it never resumed
// and running its context handling again on stale state.
public class Async2InlinedAsyncVersionFrames
{
    // Completes synchronously for even values and asynchronously for odd ones, so a loop
    // mixes iterations that suspend with iterations that do not.
    private sealed class Source : IValueTaskSource<int>
    {
        private ManualResetValueTaskSourceCore<int> _core;

        public Source() => _core.RunContinuationsAsynchronously = true;

        public ValueTask<int> Start(int value)
        {
            _core.Reset();
            if ((value & 1) == 0)
            {
                _core.SetResult(value);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(_ => _core.SetResult(value));
            }

            return new ValueTask<int>(this, _core.Version);
        }

        public int GetResult(short token) => _core.GetResult(token);
        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object?> c, object? s, short t, ValueTaskSourceOnCompletedFlags f)
            => _core.OnCompleted(c, s, t, f);
    }

    private static readonly Source s_source = new Source();

    // Non-async, so the suspension ends up inside its async version, a frame with no
    // contexts of its own.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTask<int> Leaf(int value) => s_source.Start(value);

    // Async, so this frame does own contexts. Its awaits must stay out of a try region or
    // it cannot be inlined at all.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async ValueTask<long> Middle(int[] values)
    {
        long total = 0;
        for (int i = 0; i < values.Length; i++)
        {
            total += await Leaf(values[i]).ConfigureAwait(false);
            total += await Leaf(values[i] + 1).ConfigureAwait(false);
        }

        return total + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async ValueTask<long> Other(int[] values)
    {
        return await Leaf(values.Length).ConfigureAwait(false);
    }

    // Non-async and not inlinable, so its async version is compiled as a root that has no
    // contexts, with the context owning frames above spliced into it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValueTask<long> Dispatch(int[] values, bool other)
    {
        if (other)
        {
            return Other(values);
        }

        return Middle(values);
    }

    private static async Task<long> Call(int[] values, bool other)
    {
        return await Dispatch(values, other).ConfigureAwait(false);
    }

    [Theory]
    [InlineData(false, 16L)]
    [InlineData(true, 3L)]
    public static void SuspendingInsideInlinedAsyncVersion(bool other, long expected)
    {
        int[] values = new int[] { 1, 2, 3 };
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(expected, Call(values, other).GetAwaiter().GetResult());
        }
    }

    // Both frames in turn, so the method has suspension points from more than one of them.
    private static async Task<long> CallBoth(int[] values)
    {
        long a = await Dispatch(values, true).ConfigureAwait(false);
        long b = await Dispatch(values, false).ConfigureAwait(false);
        return a + b;
    }

    [Fact]
    public static void SuspendingInsideSeveralInlinedFrames()
    {
        int[] values = new int[] { 1, 2, 3 };
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(19L, CallBoth(values).GetAwaiter().GetResult());
        }
    }
}
