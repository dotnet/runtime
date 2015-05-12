// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

public class ProgressTests
{
    [Fact]
    public void Ctor()
    {
        new Progress<int>();
        new Progress<int>(i => { });
        Assert.Throws<ArgumentNullException>(() => new Progress<int>(null));
    }

    [Fact]
    public void NoWorkQueuedIfNoHandlers()
    {
        RunWithoutSyncCtx(() =>
        {
            var tsc = new TrackingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(tsc);
            Progress<int> p = new Progress<int>();
            for (int i = 0; i < 3; i++)
                ((IProgress<int>)p).Report(i);
            Assert.Equal(0, tsc.Posts);
            SynchronizationContext.SetSynchronizationContext(null);
        });
    }

    [Fact]
    public void TargetsCurrentSynchronizationContext()
    {
        RunWithoutSyncCtx(() =>
        {
            var tsc = new TrackingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(tsc);
            Progress<int> p = new Progress<int>(i => { });
            for (int i = 0; i < 3; i++)
                ((IProgress<int>)p).Report(i);
            Assert.Equal(3, tsc.Posts);
            SynchronizationContext.SetSynchronizationContext(null);
        });
    }

    [Fact]
    public void EventRaisedWithActionHandler()
    {
        RunWithoutSyncCtx(() =>
        {
            Barrier b = new Barrier(2);
            Progress<int> p = new Progress<int>(i =>
            {
                Assert.Equal(b.CurrentPhaseNumber, i);
                b.SignalAndWait();
            });
            for (int i = 0; i < 3; i++)
            {
                ((IProgress<int>)p).Report(i);
                b.SignalAndWait();
            }
        });
    }

    [Fact]
    public void EventRaisedWithEventHandler()
    {
        RunWithoutSyncCtx(() =>
        {
            Barrier b = new Barrier(2);
            Progress<int> p = new Progress<int>();
            p.ProgressChanged += (s, i) =>
            {
                Assert.Same(s, p);
                Assert.Equal(b.CurrentPhaseNumber, i);
                b.SignalAndWait();
            };
            for (int i = 0; i < 3; i++)
            {
                ((IProgress<int>)p).Report(i);
                b.SignalAndWait();
            }
        });
    }

    private static void RunWithoutSyncCtx(Action action)
    {
        Task.Run(action).GetAwaiter().GetResult();
    }

    private sealed class TrackingSynchronizationContext : SynchronizationContext
    {
        internal int Posts = 0;

        public override void Post(SendOrPostCallback d, object state)
        {
            Posts++;
            base.Post(d, state);
        }
    }

}
