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
    public static void TestEntryPoint()
    {
        SynchronizationContext prevContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new MySyncContext(42));
            Test().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevContext);
        }
    }

    private static async Task Test()
    {
        MySyncContext context = (MySyncContext)SynchronizationContext.Current;
        Assert.Equal(42, context.Posts);

        await WrappedYieldToThreadPool(suspend: false);
        Assert.Equal(42, context.Posts);
        Assert.Same(context, SynchronizationContext.Current);

        await WrappedYieldToThreadPool(suspend: true);
        Assert.Equal(43, context.Posts);
        Assert.Same(context, SynchronizationContext.Current);

        await WrappedYieldToThreadPool(suspend: true).ConfigureAwait(true);
        Assert.Equal(44, context.Posts);
        Assert.Same(context, SynchronizationContext.Current);

        await WrappedYieldToThreadPool(suspend: false).ConfigureAwait(false);
        Assert.Equal(44, context.Posts);
        Assert.Same(context, SynchronizationContext.Current);

        await WrappedYieldToThreadPool(suspend: true).ConfigureAwait(false);
        Assert.Equal(44, context.Posts);
        Assert.Null(SynchronizationContext.Current);
    }

    private static async Task WrappedYieldToThreadPool(bool suspend)
    {
        if (suspend)
        {
            await new YieldToThreadPool();
        }
    }

    private struct YieldToThreadPool : ICriticalNotifyCompletion
    {
        public YieldToThreadPool GetAwaiter() => this;

        public bool IsCompleted => false;

        public void UnsafeOnCompleted(Action action)
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ => action(), null);
        }

        public void OnCompleted(Action continuation)
        {
            throw new NotImplementedException();
        }

        public void GetResult()
        {
        }
    }

    private class MySyncContext(int initialPosts) : SynchronizationContext
    {
        public int Posts = initialPosts;

        public override void Post(SendOrPostCallback d, object state)
        {
            Posts++;
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                SetSynchronizationContext(this);
                d(state);
            }, null);
        }
    }
}
