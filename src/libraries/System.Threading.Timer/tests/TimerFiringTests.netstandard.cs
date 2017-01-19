// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Xunit;

public partial class TimerFiringTests
{
    [Fact]
    public void Timer_Constructor_CallbackOnly_Change()
    {
        var e = new ManualResetEvent(false);
        using (var t = new Timer(s => e.Set()))
        {
            t.Change(0u, 0u);
            Assert.True(e.WaitOne(MaxPositiveTimeoutInMs));
        }
    }

    [Fact]
    public void Timer_Dispose_WaitHandle_Negative()
    {
        Assert.Throws<ArgumentNullException>(() => new Timer(s => { }).Dispose(null));
    }

    [Fact]
    public void Timer_Dispose_WaitHandle()
    {
        int tickCount = 0;
        var someTicksPending = new ManualResetEvent(false);
        var completeTicks = new ManualResetEvent(false);
        var allTicksCompleted = new ManualResetEvent(false);
        var t =
            new Timer(s =>
            {
                if (Interlocked.Increment(ref tickCount) == 2)
                    someTicksPending.Set();
                Assert.True(completeTicks.WaitOne(MaxPositiveTimeoutInMs));
                Interlocked.Decrement(ref tickCount);
            }, null, 0, 1);
        Assert.True(someTicksPending.WaitOne(MaxPositiveTimeoutInMs));
        completeTicks.Set();
        t.Dispose(allTicksCompleted);
        Assert.True(allTicksCompleted.WaitOne(MaxPositiveTimeoutInMs));
        Assert.Equal(0, tickCount);
        Assert.Throws<ObjectDisposedException>(() => t.Change(0, 0));
    }
}
