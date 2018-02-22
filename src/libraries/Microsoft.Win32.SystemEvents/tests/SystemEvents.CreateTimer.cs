// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace Microsoft.Win32.SystemEventsTests
{
    public class CreateTimerTests
    {
        /// <summary>
        /// Minimum permitted interval
        /// </summary>
        public const int TimerInterval = 10;

        [Fact]
        public void CreateTimerInvalidInterval()
        {
            Assert.Throws<ArgumentException>(() => SystemEvents.CreateTimer(0));
            Assert.Throws<ArgumentException>(() => SystemEvents.CreateTimer(-1));
            Assert.Throws<ArgumentException>(() => SystemEvents.CreateTimer(int.MinValue));
        }
        
        [Fact]
        public void TimerElapsedSignaled()
        {
            var elapsed = new AutoResetEvent(false);

            object elapsedSender = null;
            IntPtr timer = IntPtr.Zero;

            TimerElapsedEventHandler handler = (sender, args) =>
            {
                if (args?.TimerId == timer)
                {
                    elapsedSender = sender;
                    elapsed.Set();
                }
            };

            SystemEvents.TimerElapsed += handler;
            try
            {
                timer = SystemEvents.CreateTimer(TimerInterval);

                Assert.True(elapsed.WaitOne(TimerInterval * SystemEventsTest.ExpectedEventMultiplier));
                Assert.IsType<SystemEvents>(elapsedSender);

                // Timer should fire more than once
                Assert.True(elapsed.WaitOne(TimerInterval * SystemEventsTest.ExpectedEventMultiplier));

                SystemEvents.KillTimer(timer);
                elapsed.Reset();

                // Timer should not fire once killed
                Assert.False(elapsed.WaitOne(TimerInterval * SystemEventsTest.UnexpectedEventMultiplier));
            }
            finally
            {
                SystemEvents.TimerElapsed -= handler;
            }
        }

        [Fact]
        public void ConcurrentTimers()
        {
            const int numConcurrentTimers = 10;
            var timersSignalled = new Dictionary<IntPtr, bool>();

            TimerElapsedEventHandler handler = (sender, args) =>
            {
                if (timersSignalled.ContainsKey(args.TimerId))
                {
                    timersSignalled[args.TimerId] = true;
                }
            };

            SystemEvents.TimerElapsed += handler;
            try
            {
                for (int i = 0; i < numConcurrentTimers; i++)
                {
                    timersSignalled[SystemEvents.CreateTimer(TimerInterval)] = false;
                }

                Thread.Sleep(TimerInterval * SystemEventsTest.ExpectedEventMultiplier);

                foreach(var timer in timersSignalled.Keys.ToArray())
                {
                    Assert.True(timersSignalled[timer]);
                    SystemEvents.KillTimer(timer);
                }
            }
            finally
            {
                SystemEvents.TimerElapsed -= handler;
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(500)]  // .5s
        [InlineData(1000)]  // 1s
        [InlineData(2000)]  // 2s
        [InlineData(10000)] // 10s
        [InlineData(30000)] // 30s
        public void TimerElapsedIsRoughlyEquivalentToInterval(int interval)
        {
            const double permittedPercentDifference = 0.1;
            var elapsed = new AutoResetEvent(false);
            IntPtr timer = IntPtr.Zero;
            var stopwatch = new Stopwatch();

            TimerElapsedEventHandler handler = (sender, args) =>
            {
                if (args?.TimerId == timer)
                {
                    stopwatch.Stop();
                    elapsed.Set();
                }
            };

            SystemEvents.TimerElapsed += handler;
            try
            {
                timer = SystemEvents.CreateTimer(interval);
                stopwatch.Start();
                Assert.True(elapsed.WaitOne(interval * SystemEventsTest.ExpectedEventMultiplier));

                var percentDifference = (double)Math.Abs(stopwatch.ElapsedMilliseconds - interval) / interval;
                Assert.True(percentDifference < permittedPercentDifference, $"Timer should fire within {permittedPercentDifference * 100.0}% of expected interval {interval}, actual: {stopwatch.ElapsedMilliseconds}, difference: {percentDifference * 100.0}%");
            }
            finally
            {
                SystemEvents.TimerElapsed -= handler;
            }

        }

        [Fact]
        public void KillTimerInvalidTimer()
        {
            Assert.Throws<ExternalException>(() => SystemEvents.KillTimer(IntPtr.Zero));
            Assert.Throws<ExternalException>(() => SystemEvents.KillTimer((IntPtr)(-1)));
        }
    }
}
