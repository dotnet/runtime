// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// When an inlined async frame logically returns to its caller after a resumption, it has
// to get back onto the caller's continuation context. If that means switching contexts the
// transition itself suspends, so everything the caller still needs afterwards has to
// survive that suspension -- including the record of which frames have resumed.
public class Async2InlinedFrameTransitionSuspends
{
    // Never runs the callback inline: every Post goes to a dedicated thread, so getting
    // back onto this context always forces a suspension.
    private sealed class QueueingContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback, object?)> _queue = new();

        public readonly string Name;

        public QueueingContext(string name)
        {
            Name = name;
            var thread = new Thread(Loop) { IsBackground = true, Name = name };
            thread.Start();
        }

        private void Loop()
        {
            foreach ((SendOrPostCallback d, object? state) in _queue.GetConsumingEnumerable())
            {
                // The user code is free to install its own context on this thread, so put
                // ours back before every callback rather than once at thread start.
                SynchronizationContext.SetSynchronizationContext(this);
                d(state);
            }
        }

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));
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

    private static readonly QueueingContext s_ctx1 = new QueueingContext("ctx1");
    private static readonly QueueingContext s_ctx2 = new QueueingContext("ctx2");
    
    private static string CurrentContextName => (SynchronizationContext.Current as QueueingContext)?.Name ?? "none";

    private static readonly AsyncLocal<int> s_local = new AsyncLocal<int>();

    private static int s_innerLocalAfterAwait;
    private static string s_middleAfterAwait = "";
    private static string s_outerAfterAwait = "";
    private static int s_suspensions;

    // Suspends on the thread pool. A custom awaiter does not capture a continuation
    // context, so this frame resumes off all three contexts; only its ExecutionContext is
    // restored. Getting back out of it to Middle is what has to switch contexts.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task Inner()
    {
        s_local.Value = 3;
        await new AlwaysThreadPoolAwaitable();
        Interlocked.Increment(ref s_suspensions);
        s_innerLocalAfterAwait = s_local.Value;
    }

    // The transition out of Inner has to post back to ctx2 and so suspends. Middle must
    // still be recorded as resumed afterwards, otherwise its own transition below is
    // skipped and Outer never gets back onto ctx1.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task Middle()
    {
        SynchronizationContext.SetSynchronizationContext(s_ctx2);
        await Inner();
        s_middleAfterAwait = CurrentContextName;
    }

    private static async Task Outer()
    {
        SynchronizationContext.SetSynchronizationContext(s_ctx1);
        await Middle();
        s_outerAfterAwait = CurrentContextName;
    }

    [Fact]
    public static void ContextsRestoredWhenFrameTransitionSuspends()
    {
        s_innerLocalAfterAwait = 0;
        s_middleAfterAwait = "";
        s_outerAfterAwait = "";
        s_suspensions = 0;

        RunOn(s_ctx1, Outer);

        Assert.Equal(1, s_suspensions);
        Assert.Equal(3, s_innerLocalAfterAwait);
        Assert.Equal("ctx2", s_middleAfterAwait);
        Assert.Equal("ctx1", s_outerAfterAwait);
    }

    // The same, but the frames keep awaiting after a transition has suspended, so the
    // resumed state has to stay correct across several rounds.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task LoopInner(int iterations)
    {
        s_local.Value = 3;
        for (int i = 0; i < iterations; i++)
        {
            await new AlwaysThreadPoolAwaitable();
            Interlocked.Increment(ref s_suspensions);
            Assert.Equal(3, s_local.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task LoopMiddle(int iterations)
    {
        SynchronizationContext.SetSynchronizationContext(s_ctx2);
        for (int i = 0; i < iterations; i++)
        {
            await LoopInner(iterations);
            Assert.Equal("ctx2", CurrentContextName);
        }
    }

    private static async Task LoopOuter(int iterations)
    {
        SynchronizationContext.SetSynchronizationContext(s_ctx1);
        for (int i = 0; i < iterations; i++)
        {
            await LoopMiddle(iterations);
            Assert.Equal("ctx1", CurrentContextName);
        }

        s_outerAfterAwait = CurrentContextName;
    }

    [Fact]
    public static void ContextsSurviveRepeatedSuspendingTransitions()
    {
        s_outerAfterAwait = "";
        s_suspensions = 0;

        RunOn(s_ctx1, () => LoopOuter(3));

        Assert.Equal(27, s_suspensions);
        Assert.Equal("ctx1", s_outerAfterAwait);
    }

    private static void RunOn(SynchronizationContext ctx, Func<Task> body)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ctx.Post(async _ =>
        {
            try
            {
                await body();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, null);

        tcs.Task.GetAwaiter().GetResult();
    }
}
