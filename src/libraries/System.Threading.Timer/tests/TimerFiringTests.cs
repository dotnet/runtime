// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace System.Threading.Tests
{
    public class TimerFiringTests
    {
        internal const int MaxPositiveTimeoutInMs = 30000;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Timer_Fires_After_DueTime_Ellapses()
        {
            AutoResetEvent are = new AutoResetEvent(false);

            using (var t = new Timer(new TimerCallback((object s) =>
            {
                are.Set();
            }), null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(Timeout.Infinite) /* not relevant */))
            {
                Assert.True(are.WaitOne(TimeSpan.FromMilliseconds(MaxPositiveTimeoutInMs)));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Timer_Fires_AndPassesStateThroughCallback()
        {
            AutoResetEvent are = new AutoResetEvent(false);

            object state = new object();
            using (var t = new Timer(new TimerCallback((object s) =>
            {
                Assert.Same(s, state);
                are.Set();
            }), state, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(Timeout.Infinite) /* not relevant */))
            {
                Assert.True(are.WaitOne(TimeSpan.FromMilliseconds(MaxPositiveTimeoutInMs)));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Timer_Fires_AndPassesNullStateThroughCallback()
        {
            AutoResetEvent are = new AutoResetEvent(false);

            using (var t = new Timer(new TimerCallback((object s) =>
            {
                Assert.Null(s);
                are.Set();
            }), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(Timeout.Infinite) /* not relevant */))
            {
                Assert.True(are.WaitOne(TimeSpan.FromMilliseconds(MaxPositiveTimeoutInMs)));
            }
        }

        [OuterLoop("Several second delays")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // values chosen based on knowing the 333 pivot used in implementation
        [InlineData(1, 1)]
        [InlineData(50, 50)]
        [InlineData(250, 50)]
        [InlineData(50, 250)]
        [InlineData(50, 400)]
        [InlineData(400, 50)]
        [InlineData(400, 400)]
        public void Timer_Fires_After_DueTime_AndOn_Period(int dueTime, int period)
        {
            int count = 0;
            AutoResetEvent are = new AutoResetEvent(false);

            using (var t = new Timer(new TimerCallback((object s) =>
            {
                if (Interlocked.Increment(ref count) >= 2)
                {
                    are.Set();
                }
            }), null, TimeSpan.FromMilliseconds(dueTime), TimeSpan.FromMilliseconds(period)))
            {
                Assert.True(are.WaitOne(TimeSpan.FromMilliseconds(MaxPositiveTimeoutInMs)));
            }
        }

        [Fact]
        public void Timer_FiresOnlyOnce_OnDueTime_With_InfinitePeriod()
        {
            int count = 0;
            AutoResetEvent are = new AutoResetEvent(false);

            using (var t = new Timer(new TimerCallback((object s) =>
            {
                if (Interlocked.Increment(ref count) >= 2)
                {
                    are.Set();
                }
            }), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(Timeout.Infinite) /* not relevant */))
            {
                Assert.False(are.WaitOne(TimeSpan.FromMilliseconds(250 /*enough for 2 fires + buffer*/)));
            }
        }

        [OuterLoop("Waits seconds")]
        [Fact]
        public void Timer_ChangeToDelete_DoesntFire()
        {
            RetryHelper.Execute(() =>
            {
                const int DueTime = 1000;
                var mres = new ManualResetEventSlim();
                using (var t = new Timer(_ => mres.Set(), null, DueTime, -1))
                {
                    t.Change(Timeout.Infinite, Timeout.Infinite);
                    Assert.False(mres.Wait(DueTime * 2));
                }
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Timer_CanDisposeSelfInCallback()
        {
            Timer t = null;
            AutoResetEvent are = new AutoResetEvent(false);
            TimerCallback tc = new TimerCallback((object o) =>
            {
                t.Dispose();
                are.Set();
            });
            t = new Timer(tc, null, -1, -1);
            t.Change(1, -1);
            Assert.True(are.WaitOne(MaxPositiveTimeoutInMs));
            GC.KeepAlive(t);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void NonRepeatingTimer_ThatHasAlreadyFired_CanChangeAndFireAgain()
        {
            AutoResetEvent are = new AutoResetEvent(false);
            TimerCallback tc = new TimerCallback((object o) => are.Set());
            using (var t = new Timer(tc, null, 1, Timeout.Infinite))
            {
                Assert.True(are.WaitOne(MaxPositiveTimeoutInMs), "Should have received first timer event");
                Assert.False(are.WaitOne(500), "Should not have received a second timer event");
                t.Change(10, Timeout.Infinite);
                Assert.True(are.WaitOne(MaxPositiveTimeoutInMs), "Should have received a second timer event after changing it");
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void MultpleTimers_PeriodicTimerIsntBlockedByBlockedCallback()
        {
            int callbacks = 2;
            Barrier b = new Barrier(callbacks + 1);
            Timer t = null;
            t = new Timer(_ =>
            {
                if (Interlocked.Decrement(ref callbacks) >= 0)
                {
                    Assert.True(b.SignalAndWait(MaxPositiveTimeoutInMs));
                }
                t.Dispose();
            }, null, -1, -1);
            t.Change(1, 50);

            Assert.True(b.SignalAndWait(MaxPositiveTimeoutInMs));
            GC.KeepAlive(t);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void ManyTimers_EachTimerDoesFire()
        {
            int maxTimers = 10000;
            CountdownEvent ce = new CountdownEvent(maxTimers);
            Timer[] timers = System.Linq.Enumerable.Range(0, maxTimers).Select(_ => new Timer(s => ce.Signal(), null, 100 /* enough time to wait on the are */, -1)).ToArray();
            try
            {
                Assert.True(ce.Wait(MaxPositiveTimeoutInMs), string.Format("Not all timers fired, {0} left of {1}", ce.CurrentCount, maxTimers));
            }
            finally
            {
                foreach (Timer t in timers)
                    t.Dispose();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [OuterLoop("Incurs seconds delay to wait for events that should never happen")]
        [Fact]
        public async Task Timer_LongTimersDontFirePrematurely_ShortTimersFireSuccessfully()
        {
            var tcsShort1 = new TaskCompletionSource();
            var tcsShort2 = new TaskCompletionSource();
            var tcsLong1 = new TaskCompletionSource();
            var tcsLong2 = new TaskCompletionSource();

            using (var timerLong1 = new Timer(_ => tcsLong1.SetResult(), null, TimeSpan.FromDays(30), Timeout.InfiniteTimeSpan))
            using (var timerLong2 = new Timer(_ => tcsLong2.SetResult(), null, TimeSpan.FromDays(40), Timeout.InfiniteTimeSpan))
            using (var timerShort1 = new Timer(_ => tcsShort1.SetResult(), null, 100, -1))
            using (var timerShort2 = new Timer(_ => tcsShort2.SetResult(), null, 200, -1))
            {
                await Task.WhenAll(tcsShort1.Task, tcsShort2.Task);
                await Task.Delay(2_000); // wait a few seconds to see if long timers complete when they shouldn't
                Assert.Equal(TaskStatus.WaitingForActivation, tcsLong1.Task.Status);
                Assert.Equal(TaskStatus.WaitingForActivation, tcsLong2.Task.Status);
            }
        }

        [OuterLoop("Takes several seconds")]
        [Fact]
        public async Task Timer_ManyDifferentSingleDueTimes_AllFireSuccessfully()
        {
            await Task.WhenAll(from p in Enumerable.Range(0, Environment.ProcessorCount)
                               select Task.Run(async () =>
                               {
                                   await Task.WhenAll(from i in Enumerable.Range(1, 1_000) select DueTimeAsync(i));
                                   await Task.WhenAll(from i in Enumerable.Range(1, 1_000) select DueTimeAsync(1_001 - i));
                               }));
        }

        [OuterLoop("Takes several seconds")]
        [Fact]
        public async Task Timer_ManyDifferentPeriodicTimes_AllFireSuccessfully()
        {
            await Task.WhenAll(from p in Enumerable.Range(0, Environment.ProcessorCount)
                               select Task.Run(async () =>
                               {
                                   await Task.WhenAll(from i in Enumerable.Range(1, 400) select PeriodAsync(period: i, iterations: 3));
                                   await Task.WhenAll(from i in Enumerable.Range(1, 400) select PeriodAsync(period: 401 - i, iterations: 3));
                               }));
        }

        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.Browser, "macOS and Browser in CI appears to have a lot more variation")]
        [OuterLoop("Takes several seconds")]
        [Theory] // selection based on 333ms threshold used by implementation
        [InlineData(new int[] { 15 })]
        [InlineData(new int[] { 333 })]
        [InlineData(new int[] { 332, 333, 334 })]
        [InlineData(new int[] { 200, 300, 400 })]
        [InlineData(new int[] { 200, 250, 300 })]
        [InlineData(new int[] { 400, 450, 500 })]
        [InlineData(new int[] { 1000 })]
        public async Task Timer_ManyDifferentSerialSingleDueTimes_AllFireWithinAllowedRange(int[] dueTimes)
        {
            const int MillisecondsPadding = 100; // for each timer, out of range == Math.Abs(actualTime - dueTime) > MillisecondsPadding
            const int MaxAllowedOutOfRangePercentage = 20; // max % allowed out of range to pass test

            for (int tries = 0; ; tries++)
            {
                try
                {
                    var outOfRange = new ConcurrentQueue<KeyValuePair<int, long>>();

                    long totalTimers = 0;
                    await Task.WhenAll(from p in Enumerable.Range(0, Environment.ProcessorCount)
                                       select Task.Run(async () =>
                                       {
                                           await Task.WhenAll(from dueTimeTemplate in dueTimes
                                                              from dueTime in Enumerable.Repeat(dueTimeTemplate, 10)
                                                              select Task.Run(async () =>
                                                              {
                                                                  var sw = new Stopwatch();
                                                                  for (int i = 1; i <= 1_000 / dueTime; i++)
                                                                  {
                                                                      sw.Restart();
                                                                      await DueTimeAsync(dueTime);
                                                                      sw.Stop();

                                                                      Interlocked.Increment(ref totalTimers);
                                                                      if (Math.Abs(sw.ElapsedMilliseconds - dueTime) > MillisecondsPadding)
                                                                      {
                                                                          outOfRange.Enqueue(new KeyValuePair<int, long>(dueTime, sw.ElapsedMilliseconds));
                                                                      }
                                                                  }
                                                              }));
                                       }));

                    double percOutOfRange = (double)outOfRange.Count / totalTimers * 100;
                    if (percOutOfRange > MaxAllowedOutOfRangePercentage)
                    {
                        IOrderedEnumerable<IGrouping<int, KeyValuePair<int, long>>> results =
                            from sample in outOfRange
                            group sample by sample.Key into groupedByDueTime
                            orderby groupedByDueTime.Key
                            select groupedByDueTime;

                        var sb = new StringBuilder();
                        sb.Append($"{percOutOfRange}% out of {totalTimers} timer firings were off by more than {MillisecondsPadding}ms");
                        foreach (IGrouping<int, KeyValuePair<int, long>> result in results)
                        {
                            sb.AppendLine();
                            sb.Append($"Expected: {result.Key}, Actuals: {string.Join(", ", result.Select(k => k.Value))}");
                        }

                        Assert.True(false, sb.ToString());
                    }
                }
                catch (XunitException) when (tries < 3)
                {
                    // This test will occasionally fail apparently because it was switched out
                    // for a short period. Eat and go around again
                    await Task.Delay(TimeSpan.FromSeconds(10)); // Should be very rare: wait for machine to settle
                    continue;
                }

                return;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TimersCreatedConcurrentlyOnDifferentThreadsAllFire()
        {
            int processorCount = Environment.ProcessorCount;

            int timerTickCount = 0;
            TimerCallback timerCallback = data => Interlocked.Increment(ref timerTickCount);

            var threadStarted = new AutoResetEvent(false);
            var createTimers = new ManualResetEvent(false);
            var timers = new Timer[processorCount];
            Action<object> createTimerThreadStart = data =>
            {
                int i = (int)data;
                var sw = new Stopwatch();
                threadStarted.Set();
                createTimers.WaitOne();

                // Use the CPU a bit around creating the timer to try to have some of these threads run concurrently
                sw.Restart();
                do
                {
                    Thread.SpinWait(1000);
                } while (sw.ElapsedMilliseconds < 10);

                timers[i] = new Timer(timerCallback, null, 1, Timeout.Infinite);

                // Use the CPU a bit around creating the timer to try to have some of these threads run concurrently
                sw.Restart();
                do
                {
                    Thread.SpinWait(1000);
                } while (sw.ElapsedMilliseconds < 10);
            };

            var waitsForThread = new Action[timers.Length];
            for (int i = 0; i < timers.Length; ++i)
            {
                var t = ThreadTestHelpers.CreateGuardedThread(out waitsForThread[i], createTimerThreadStart);
                t.IsBackground = true;
                t.Start(i);
                threadStarted.CheckedWait();
            }

            createTimers.Set();
            ThreadTestHelpers.WaitForCondition(() => timerTickCount == timers.Length);

            foreach (var waitForThread in waitsForThread)
            {
                waitForThread();
            }
        }

        private static Task DueTimeAsync(int dueTime)
        {
            // We could just use Task.Delay, but it only uses Timer as an implementation detail.
            // Since these are Timer tests, we use an implementation that explicitly uses Timer.
            var tcs = new TaskCompletionSource();
            var t = new Timer(_ => tcs.SetResult()); // rely on Timer(TimerCallback) rooting itself
            t.Change(dueTime, -1);
            return tcs.Task;
        }

        private static async Task PeriodAsync(int period, int iterations)
        {
            var tcs = new TaskCompletionSource();
            using (var t = new Timer(_ => { if (Interlocked.Decrement(ref iterations) == 0) tcs.SetResult(); })) // rely on Timer(TimerCallback) rooting itself
            {
                t.Change(period, period);
                await tcs.Task.ConfigureAwait(false);
            }
        }
    }
}
