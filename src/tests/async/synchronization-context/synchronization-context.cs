// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2SynchronizationContext
{
    [Fact]
    public static void TestSyncContextContinue()
    {
        SynchronizationContext prevContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new MySyncContext());
            TestSyncContextContinueAsync().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevContext);
        }
    }

    private static async Task TestSyncContextContinueAsync()
    {
        MySyncContext context = (MySyncContext)SynchronizationContext.Current;
        await WrappedYieldToThreadPool(suspend: false);
        Assert.Same(context, SynchronizationContext.Current);

        await WrappedYieldToThreadPool(suspend: true);
        Assert.Same(context, SynchronizationContext.Current);

        await WrappedYieldToThreadPool(suspend: true).ConfigureAwait(true);
        Assert.Same(context, SynchronizationContext.Current);

        await WrappedYieldToThreadPool(suspend: false).ConfigureAwait(false);
        Assert.Same(context, SynchronizationContext.Current);

        // Currently disabled since ConfigureAwait does not become a runtime async call,
        // and this has a race condition until it does (where the callee finishes before
        // we check IsCompleted on the awaiter).
        //await WrappedYieldToThreadPool(suspend: true).ConfigureAwait(false);
        //Assert.Null(SynchronizationContext.Current);
        //
        //await WrappedYieldToThreadWithCustomSyncContext();
        //Assert.Null(SynchronizationContext.Current);
    }

    private static async Task WrappedYieldToThreadPool(bool suspend)
    {
        if (suspend)
        {
            await Task.Yield();
        }
    }

    private static async Task WrappedYieldToThreadWithCustomSyncContext()
    {
        Assert.Null(SynchronizationContext.Current);
        await new YieldToThreadWithCustomSyncContext();
        Assert.True(SynchronizationContext.Current is MySyncContext { });
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

    private struct YieldToThreadWithCustomSyncContext : ICriticalNotifyCompletion
    {
        public YieldToThreadWithCustomSyncContext GetAwaiter() => this;

        public void UnsafeOnCompleted(Action continuation)
        {
            new Thread(state =>
            {
                SynchronizationContext.SetSynchronizationContext(new MySyncContext());
                continuation();
            }).Start();
        }

        public void OnCompleted(Action continuation)
        {
            throw new NotImplementedException();
        }

        public bool IsCompleted => false;

        public void GetResult() { }
    }

    [Fact]
    public static void TestSyncContextSaveRestore()
    {
        SynchronizationContext prevContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new SyncContextWithoutRestore());
            TestSyncContextSaveRestoreAsync().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevContext);
        }
    }

    private static async Task TestSyncContextSaveRestoreAsync()
    {
        Assert.True(SynchronizationContext.Current is SyncContextWithoutRestore);
        await ClearSyncContext();
        Assert.True(SynchronizationContext.Current is SyncContextWithoutRestore);
    }

    private static async Task ClearSyncContext()
    {
        SynchronizationContext.SetSynchronizationContext(null);
    }

    [Fact]
    public static void TestSyncContextNotRestored()
    {
        SynchronizationContext prevContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new SyncContextWithoutRestore());
            TestSyncContextNotRestoredAsync().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevContext);
        }
    }

    private static async Task TestSyncContextNotRestoredAsync()
    {
        Assert.True(SynchronizationContext.Current is SyncContextWithoutRestore);
        await SuspendThenClearSyncContext();
        Assert.Null(SynchronizationContext.Current);
    }

    private static async Task SuspendThenClearSyncContext()
    {
        Assert.True(SynchronizationContext.Current is SyncContextWithoutRestore);
        SyncContextWithoutRestore syncCtx = (SyncContextWithoutRestore)SyncContextWithoutRestore.Current;
        Assert.Equal(0, syncCtx.NumPosts);

        await Task.Yield();
        Assert.Null(SynchronizationContext.Current);
        Assert.Equal(1, syncCtx.NumPosts);
    }

    private class SyncContextWithoutRestore : SynchronizationContext
    {
        public int NumPosts;

        public override void Post(SendOrPostCallback d, object state)
        {
            NumPosts++;
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                d(state);
            }, null);
        }
    }

    [Fact]
    public static void TestContinueOnCorrectSyncContext()
    {
        SynchronizationContext prevContext = SynchronizationContext.Current;
        try
        {
            TestContinueOnCorrectSyncContextAsync().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevContext);
        }
    }

    private static async Task TestContinueOnCorrectSyncContextAsync()
    {
        MySyncContext context1 = new MySyncContext();
        MySyncContext context2 = new MySyncContext();

        SynchronizationContext.SetSynchronizationContext(context1);
        await SetContext(context2, suspend: false);
        Assert.True(SynchronizationContext.Current == context1);

        await SetContext(context2, suspend: true);
        Assert.True(SynchronizationContext.Current == context1);
    }

    private static async Task SetContext(SynchronizationContext context, bool suspend)
    {
        SynchronizationContext.SetSynchronizationContext(context);

        if (suspend)
            await Task.Yield();
    }
}
