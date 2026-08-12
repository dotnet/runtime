// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// The async version of a non-async method does not save and restore contexts of its own:
// CORINFO_ASYNC_SAVE_CONTEXTS is not set for it, so it has no resumed indicator. Its
// awaits instead inherit the inlining call's indicator and contexts, which means a
// resumption inside one has to set the *caller's* indicator directly.
//
// These tests pin down that the caller still observes its own context after a suspension
// that happened inside such an inlined frame.
public class Async2AsyncVersionInline
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

    private static int s_suspensions;

    private static async Task Suspend()
    {
        await new AlwaysThreadPoolAwaitable();
        Interlocked.Increment(ref s_suspensions);
    }

    // Not marked async: the runtime compiles an async version of this whose IL is this
    // body, and that version tail awaits Suspend without any context handling of its own.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task TailAwaitingVersion() => Suspend();

    private static string s_contextAfterAwait = "";

    // The caller does have context handling. If the resumption inside the inlined async
    // version fails to mark this frame resumed, this frame's restore will run as though it
    // never suspended.
    private static async Task CallerAwaitsVersion()
    {
        SynchronizationContext.SetSynchronizationContext(s_syncContext1);
        await TailAwaitingVersion();
        s_contextAfterAwait = CurrentContextName;
    }

    [Fact]
    public static void ContextRestoredAfterInlinedAsyncVersionSuspends()
    {
        s_contextAfterAwait = "";
        s_suspensions = 0;
        RunOn(s_syncContext1, CallerAwaitsVersion);
        Assert.Equal(1, s_suspensions);
        Assert.Equal("ctx1", s_contextAfterAwait);
    }

    private static string s_outerContextAfterAwait = "";
    private static string s_innerContextAfterAwait = "";

    // Two levels: an async frame with its own contexts, awaiting an async version, which
    // tail awaits another frame that has its own contexts and suspends.
    private static async Task InnerWithContexts()
    {
        SynchronizationContext.SetSynchronizationContext(s_syncContext2);
        await Suspend();
        s_innerContextAfterAwait = CurrentContextName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task VersionOverInner() => InnerWithContexts();

    private static async Task OuterAwaitsVersion()
    {
        SynchronizationContext.SetSynchronizationContext(s_syncContext1);
        await VersionOverInner();
        s_outerContextAfterAwait = CurrentContextName;
    }

    [Fact]
    public static void ContextsRestoredThroughInlinedAsyncVersion()
    {
        s_outerContextAfterAwait = "";
        s_innerContextAfterAwait = "";
        s_suspensions = 0;
        RunOn(s_syncContext1, OuterAwaitsVersion);
        Assert.Equal(1, s_suspensions);
        Assert.Equal("ctx2", s_innerContextAfterAwait);
        Assert.Equal("ctx1", s_outerContextAfterAwait);
    }

    // The version is awaited in a non-tail position, so the caller has work after it.
    private static async Task<int> CallerWithWorkAfter()
    {
        SynchronizationContext.SetSynchronizationContext(s_syncContext1);
        await TailAwaitingVersion();
        s_contextAfterAwait = CurrentContextName;
        return 42;
    }

    [Fact]
    public static void ContextRestoredWhenVersionAwaitedInNonTailPosition()
    {
        s_contextAfterAwait = "";
        s_suspensions = 0;
        int result = 0;
        RunOn(s_syncContext1, async () => { result = await CallerWithWorkAfter(); });
        Assert.Equal(1, s_suspensions);
        Assert.Equal(42, result);
        Assert.Equal("ctx1", s_contextAfterAwait);
    }

    private static void RunOn(SynchronizationContext ctx, Func<Task> body)
    {
        SynchronizationContext original = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(ctx);
        try
        {
            body().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }
}
