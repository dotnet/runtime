// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// Behaviors that must be preserved when runtime async calls are inlined. These are the
// worked examples from docs/design/coreclr/jit/runtime-async-inlining.md.
//
// Inlining removes both a call site and a callee, and with them the context handling the
// async infrastructure would otherwise have performed at each frame boundary. These tests
// pin down the observable results of that handling, so they must produce the same answers
// whether or not the calls are inlined.
//
// The inner frames are marked AggressiveInlining so that the inlining actually happens
// rather than being left to the profitability heuristic, which rejects these callees.
public class Async2InlinedFrameContexts
{
    private sealed class NamedContext : SynchronizationContext
    {
        public readonly string Name;

        public NamedContext(string name) => Name = name;

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

    // Always suspends and resumes on a thread pool thread, so resumption never happens on
    // whatever context the awaiting frames were running in.
    private struct AlwaysThreadPoolAwaitable : INotifyCompletion
    {
        public bool IsCompleted => false;

        public void OnCompleted(Action continuation)
        {
            ThreadPool.QueueUserWorkItem(_ => continuation());
        }

        public void GetResult()
        {
        }

        public AlwaysThreadPoolAwaitable GetAwaiter() => this;
    }

    private static readonly NamedContext s_syncContext1 = new NamedContext("ctx1");
    private static readonly NamedContext s_syncContext2 = new NamedContext("ctx2");

    private static string CurrentContextName => (SynchronizationContext.Current as NamedContext)?.Name ?? "none";

    // Each frame sets its own synchronization context before awaiting, so each frame's
    // continuation captures a different one. After the innermost await resumes on the
    // thread pool, every frame must be back on the context it captured by the time it
    // observes it again.
    private static string s_fooContextAfterAwait = "";
    private static string s_barContextAfterAwait = "";

    private static async Task ContextFoo()
    {
        SynchronizationContext.SetSynchronizationContext(s_syncContext1);
        await ContextBar();
        s_fooContextAfterAwait = CurrentContextName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task ContextBar()
    {
        SynchronizationContext.SetSynchronizationContext(s_syncContext2);
        await ContextBaz();
        s_barContextAfterAwait = CurrentContextName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task ContextBaz()
    {
        await new AlwaysThreadPoolAwaitable();
    }

    [Fact]
    public static void SynchronizationContextIsRestoredPerFrame()
    {
        SynchronizationContext original = SynchronizationContext.Current;
        try
        {
            ContextFoo().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        // Bar awaited Baz having set ctx2, so Bar resumes on ctx2. Foo awaited Bar having
        // set ctx1, so Foo resumes on ctx1.
        Assert.Equal("ctx2", s_barContextAfterAwait);
        Assert.Equal("ctx1", s_fooContextAfterAwait);
    }

    private static readonly AsyncLocal<int> s_local = new AsyncLocal<int>();

    private static int s_bazLocalAfterAwait;
    private static int s_barLocalAfterAwait;
    private static int s_fooLocalAfterAwait;

    private static async Task LocalFoo()
    {
        s_local.Value = 1;
        await LocalBar();
        s_fooLocalAfterAwait = s_local.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task LocalBar()
    {
        s_local.Value = 2;
        await LocalBaz();
        s_barLocalAfterAwait = s_local.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task LocalBaz()
    {
        s_local.Value = 3;
        await new AlwaysThreadPoolAwaitable();
        s_bazLocalAfterAwait = s_local.Value;
    }

    [Fact]
    public static void ExecutionContextIsRestoredPerFrame()
    {
        LocalFoo().GetAwaiter().GetResult();

        // Each frame sees the AsyncLocal value it set, because every frame boundary
        // restores the ExecutionContext that frame captured.
        Assert.Equal(3, s_bazLocalAfterAwait);
        Assert.Equal(2, s_barLocalAfterAwait);
        Assert.Equal(1, s_fooLocalAfterAwait);
    }

    // The same, but with the awaits inside loops so the frames suspend and resume
    // repeatedly. A frame's captured contexts must survive later suspensions of the
    // frames nested inside it.
    private static async Task LoopFoo(int iterations)
    {
        SynchronizationContext.SetSynchronizationContext(s_syncContext1);
        s_local.Value = 1;

        for (int i = 0; i < iterations; i++)
        {
            await LoopBar(iterations);
            Assert.Equal("ctx1", CurrentContextName);
            Assert.Equal(1, s_local.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task LoopBar(int iterations)
    {
        SynchronizationContext.SetSynchronizationContext(s_syncContext2);
        s_local.Value = 2;

        for (int i = 0; i < iterations; i++)
        {
            await new AlwaysThreadPoolAwaitable();
            Assert.Equal(2, s_local.Value);
        }
    }

    [Fact]
    public static void ContextsSurviveRepeatedSuspensions()
    {
        SynchronizationContext original = SynchronizationContext.Current;
        try
        {
            LoopFoo(4).GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }
}
