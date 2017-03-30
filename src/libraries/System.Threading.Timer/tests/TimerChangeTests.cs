// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Xunit;

public partial class TimerChangeTests
{
    private void EmptyTimerTarget(object o) { }

    [Fact]
    public void Timer_Change_NegativeTimeSpan_DueTime_Throws()
    {
        using (var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>t.Change(TimeSpan.FromMilliseconds(-2), new TimeSpan(1) /* not relevant */));
        }
    }

    [Fact]
    public void Timer_Change_NegativeTimeSpan_Period_Throws()
    {
        using (var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => t.Change(new TimeSpan(1) /* not relevant */, TimeSpan.FromMilliseconds(-2)));
        }
    }

    [Fact]
    public void Timer_Change_NegativeInt_DueTime_Throws()
    {
        using (var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => t.Change(-2, 1 /* not relevant */));
        }
    }

    [Fact]
    public void Timer_Change_NegativeInt_Period_Throws()
    {
        using (var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => t.Change(1 /* not relevant */, -2));
        }
    }

    [Fact]
    public void Timer_Change_TooLongTimeSpan_DueTime_Throws()
    {
        using (var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => t.Change(TimeSpan.FromMilliseconds((long)0xFFFFFFFF), new TimeSpan(1) /* not relevant */));
        }
    }

    [Fact]
    public void Timer_Change_TooLongTimeSpan_Period_Throws()
    {
        using (var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => t.Change(new TimeSpan(1) /* not relevant */, TimeSpan.FromMilliseconds((long)0xFFFFFFFF)));
        }
    }

    [Fact]
    public void Timer_Change_TimeSpan_AfterDispose_Throws()
    {
        var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1);
        t.Dispose();
        Assert.Throws<ObjectDisposedException>(() => t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
    }

    [Fact]
    public void Timer_Change_Int_AfterDispose_Throws()
    {
        var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1);
        t.Dispose();
        Assert.Throws<ObjectDisposedException>(() => t.Change(1, 1));
    }

    [Fact]
    public void Timer_Change_BeforeDueTime_ChangesWhenTimerWillFire()
    {
        AutoResetEvent are = new AutoResetEvent(false);

        using (var t = new Timer(new TimerCallback((object s) =>
        {
            are.Set();
        }), null, TimeSpan.FromSeconds(500), TimeSpan.FromMilliseconds(50)))
        {
            Assert.False(are.WaitOne(TimeSpan.FromMilliseconds(100)), "The reset event should not have been set yet");
            t.Change(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(-1));
            Assert.True(are.WaitOne(TimeSpan.FromMilliseconds(TimerFiringTests.MaxPositiveTimeoutInMs)), "Should have received a timer event after this new duration");
        }
    }

    [Fact]
    public void Timer_Change_Int64_Negative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget).Change((long)-2, (long)-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget).Change((long)-1, (long)-2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget).Change((long)0xffffffff, (long)-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget).Change((long)-1, (long)0xffffffff));
    }

    [Fact]
    public void Timer_Change_UInt32_Int64_AfterDispose_Throws()
    {
        var t = new Timer(EmptyTimerTarget);
        t.Dispose();
        Assert.Throws<ObjectDisposedException>(() => t.Change(0u, 0u));
        Assert.Throws<ObjectDisposedException>(() => t.Change(0L, 0L));
    }
}
