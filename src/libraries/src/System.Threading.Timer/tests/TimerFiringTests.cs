// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using Xunit;

public class TimerFiringTests
{
    [Fact]
    public void Timer_Fires_After_DueTime_Ellapses()
    {
        AutoResetEvent are = new AutoResetEvent(false);

        using (var t = new Timer(new TimerCallback((object s) =>
        {
            are.Set();
        }), null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(Timeout.Infinite) /* not relevant */))
        {
            Assert.True(are.WaitOne(TimeSpan.FromMilliseconds(750)));
        }
    }

    [Fact]
    public void Timer_Fires_AndPassesStateThroughCallback()
    {
        object state = new object();
        using (var t = new Timer(new TimerCallback((object s) =>
        {
            Assert.Same(s, state);
        }), state, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(Timeout.Infinite) /* not relevant */)) { }
    }

    [Fact]
    public void Timer_Fires_AndPassesNullStateThroughCallback()
    {
        using (var t = new Timer(new TimerCallback((object s) =>
        {
            Assert.Null(s);
        }), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(Timeout.Infinite) /* not relevant */)) { }
    }

    [Fact]
    public void Timer_Fires_After_DueTime_AndOn_Period()
    {
        int count = 0;
        AutoResetEvent are = new AutoResetEvent(false);

        using (var t = new Timer(new TimerCallback((object s) =>
        {
            count++;
            if (count >= 2)
            {
                are.Set();
            }
        }), null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(50)))
        {
            Assert.True(are.WaitOne(TimeSpan.FromMilliseconds(2000)));
        }
    }

    [Fact]
    public void Timer_FiresOnlyOnce_OnDueTime_With_InfinitePeriod()
    {
        int count = 0;
        AutoResetEvent are = new AutoResetEvent(false);

        using (var t = new Timer(new TimerCallback((object s) =>
        {
            count++;
            if (count >= 2)
            {
                are.Set();
            }
        }), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(Timeout.Infinite) /* not relevant */))
        {
            Assert.False(are.WaitOne(TimeSpan.FromSeconds(1)));
        }
    }

    [Fact]
    public void MultipleTimers_ShouldFire_InCorrectOrder()
    {
        object t1State = new object();
        object t2State = new object();
        Timer t1 = null;
        Timer t2 = null;
        int count = 0;
        AutoResetEvent are = new AutoResetEvent(false);
        TimerCallback tc = new TimerCallback((object o) =>
        {
            if (count == 0)
            {
                Assert.Same(t2State, o);
                count++;
            }
            else
            {
                Assert.Same(t1State, o);
                are.Set();
            }
        });

        using (t1 = new Timer(tc, t1State, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1)))
        using (t2 = new Timer(tc, t2State, TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(-1)))
        {
            // Wait for both events to fire
            Assert.True(are.WaitOne(TimeSpan.FromMilliseconds(1500 /*1.5 seconds*/)));
        }
    }

    [Fact]
    public void Timer_CanDisposeSelfInCallback()
    {
        // There's nothing to validate besides that we don't throw an exception, so rely on xunit 
        // to catch any exception that would be thrown and signal a test failure
        Timer t = null;
        AutoResetEvent are = new AutoResetEvent(false);
        TimerCallback tc = new TimerCallback((object o) =>
        {
            t.Dispose();
            are.Set();
        });
        t = new Timer(tc, null, 1, -1);
        Assert.True(are.WaitOne(50));
    }

    [Fact]
    public void Timer_CanBeDisposedMultipleTimes()
    {
        // There's nothing to validate besides that we don't throw an exception, so rely on xunit 
        // to catch any exception that would be thrown and signal a test failure
        TimerCallback tc = new TimerCallback((object o) => { });
        var t = new Timer(tc, null, 100, -1);
        for (int i = 0; i < 10; i++)
            t.Dispose();
    }

    [Fact]
    public void NonRepeatingTimer_ThatHasAlreadyFired_CanChangeAndFireAgain()
    {
        AutoResetEvent are = new AutoResetEvent(false);
        TimerCallback tc = new TimerCallback((object o) => are.Set());
        using (var t = new Timer(tc, null, 1, Timeout.Infinite))
        {
            Assert.True(are.WaitOne(50), "Should have received first timer event");
            Assert.False(are.WaitOne(20), "Should not have received a second timer event");
            t.Change(10, Timeout.Infinite);
            Assert.True(are.WaitOne(50), "Should have received a second timer event after changing it");
        }
    }

    [Fact]
    public void Running_Timer_CanBeFinalizedAndStopsFiring()
    {
        AutoResetEvent are = new AutoResetEvent(false);
        TimerCallback tc = new TimerCallback((object o) => are.Set());
        var t = new Timer(tc, null, 100, 500);
        Assert.True(are.WaitOne(250), "Failed to get first timer fire");
        t = null; // Remove our refence so the timer can be GC'd 
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(are.WaitOne(750), "Should not have received a timer fire after it was collected");
    }

    [Fact]
    public void MultpleTimers_PeriodicTimerIsntBlockedByABlockedTimer()
    {
        AutoResetEvent are1 = new AutoResetEvent(false);
        AutoResetEvent are2 = new AutoResetEvent(false);
        TimerCallback tc = new TimerCallback((object o) =>
        {
            using (var t1 = new Timer((object obj) => are1.Set(), null, 1, -1))
            {
                Assert.True(are1.WaitOne(250), "Should have received a second callback while blocked");
                are2.Set();
            }
        });

        using (var t1 = new Timer(tc, null, 1, -1))
        {
            Assert.True(are2.WaitOne(500), "Blocking callback prevented a second timer from firing");
        }
    }

    [Fact]
    public void ManyTimers_EachTimerDoesFire()
    {
        int maxTimers = 10000;
        CountdownEvent ce = new CountdownEvent(maxTimers);
        Timer[] timers = System.Linq.Enumerable.Range(0, maxTimers).Select(_ => new Timer(s => ce.Signal(), null, 100 /* enough time to wait on the are */, -1)).ToArray();
        try
        {
            Assert.True(ce.Wait(500), String.Format("Not all timers fired, {0} left of {1}", ce.CurrentCount, maxTimers));
        }
        finally
        {
            foreach (Timer t in timers)
                t.Dispose();
        }
    }
}
