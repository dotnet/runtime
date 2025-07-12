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
    public static void TestSyncContexts()
    {
        SynchronizationContext prevContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new MySyncContext());
            TestSyncContext().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevContext);
        }
    }

    private static async Task TestSyncContext()
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

        await WrappedYieldToThreadPool(suspend: true).ConfigureAwait(false);
        Assert.Null(SynchronizationContext.Current);

        await WrappedYieldToThreadWithCustomSyncContext();
        Assert.Null(SynchronizationContext.Current);
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
}
