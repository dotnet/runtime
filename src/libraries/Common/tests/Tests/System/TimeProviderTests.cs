// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Tests.System
{
    public class TimeProviderTests
    {
        [Fact]
        public void TestUtcSystemTime()
        {
            DateTimeOffset dto1 = DateTimeOffset.UtcNow;
            DateTimeOffset providerDto = TimeProvider.System.UtcNow;
            DateTimeOffset dto2 = DateTimeOffset.UtcNow;

            Assert.InRange(providerDto.Ticks, dto1.Ticks, dto2.Ticks);
            Assert.Equal(TimeSpan.Zero, providerDto.Offset);
        }

        [Fact]
        public void TestLocalSystemTime()
        {
            DateTimeOffset dto1 = DateTimeOffset.Now;
            DateTimeOffset providerDto = TimeProvider.System.LocalNow;
            DateTimeOffset dto2 = DateTimeOffset.Now;

            // Ensure there was no daylight saving shift during the test execution.
            if (dto1.Offset == dto2.Offset)
            {
                Assert.InRange(providerDto.Ticks, dto1.Ticks, dto2.Ticks);
                Assert.Equal(dto1.Offset, providerDto.Offset);
            }
        }

        [Fact]
        public void TestSystemProviderWithTimeZone()
        {
            Assert.Equal(TimeZoneInfo.Local.Id, TimeProvider.System.LocalTimeZone.Id);

            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Pacific Standard Time" : "America/Los_Angeles");

            TimeProvider tp = TimeProvider.FromLocalTimeZone(tzi);
            Assert.Equal(tzi.Id, tp.LocalTimeZone.Id);

            DateTimeOffset utcDto1 = DateTimeOffset.UtcNow;
            DateTimeOffset localDto = tp.LocalNow;
            DateTimeOffset utcDto2 = DateTimeOffset.UtcNow;

            DateTimeOffset utcConvertedDto = TimeZoneInfo.ConvertTime(localDto, TimeZoneInfo.Utc);
            Assert.InRange(utcConvertedDto.Ticks, utcDto1.Ticks, utcDto2.Ticks);
        }

        [Fact]
        public void TestSystemTimestamp()
        {
            long timestamp1 = Stopwatch.GetTimestamp();
            long providerTimestamp1 = TimeProvider.System.GetTimestamp();
            long timestamp2 = Stopwatch.GetTimestamp();
            Thread.Sleep(100);
            long providerTimestamp2 = TimeProvider.System.GetTimestamp();

            Assert.InRange(providerTimestamp1, timestamp1, timestamp2);
            Assert.True(providerTimestamp2 > timestamp2);
            Assert.Equal(Stopwatch.GetElapsedTime(providerTimestamp1, providerTimestamp2), TimeProvider.System.GetElapsedTime(providerTimestamp1, providerTimestamp2));

            Assert.Equal(Stopwatch.Frequency, TimeProvider.System.TimestampFrequency);
        }

        public static IEnumerable<object[]> TimersProvidersData()
        {
            yield return new object[] { TimeProvider.System, 6000 };
            yield return new object[] { new FastClock(),     3000 };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(TimersProvidersData))]
        public void TestProviderTimer(TimeProvider provider, int MaxMilliseconds)
        {
            TimerState state = new TimerState();

            state.Timer = provider.CreateTimer(
                            stat =>
                                {
                                    TimerState s = (TimerState)stat;
                                    s.Counter++;

                                    s.TotalTicks += DateTimeOffset.UtcNow.Ticks - s.UtcNow.Ticks;

                                    switch (s.Counter)
                                    {
                                        case 2:
                                            s.Period = 400;
                                            s.Timer.Change(TimeSpan.FromMilliseconds(s.Period), TimeSpan.FromMilliseconds(s.Period));
                                            break;

                                        case 4:
                                            s.TokenSource.Cancel();
                                            s.Timer.Dispose();
                                            break;
                                    }

                                    s.UtcNow = DateTimeOffset.UtcNow;
                                },
                            state,
                            TimeSpan.FromMilliseconds(state.Period), TimeSpan.FromMilliseconds(state.Period));

            state.TokenSource.Token.WaitHandle.WaitOne(30000);
            state.TokenSource.Dispose();

            Assert.Equal(4, state.Counter);
            Assert.Equal(400, state.Period);
            Assert.True(MaxMilliseconds >= state.TotalTicks / TimeSpan.TicksPerMillisecond, $"The total fired periods {state.TotalTicks / TimeSpan.TicksPerMillisecond}ms expected not exceeding the expected max {MaxMilliseconds}");
        }

        [Fact]
        public void FastClockTest()
        {
            FastClock fastClock = new FastClock();

            for (int i = 0; i < 20; i++)
            {
                DateTimeOffset fastNow = fastClock.UtcNow;
                DateTimeOffset now = DateTimeOffset.UtcNow;

                Assert.True(fastNow > now, $"Expected {fastNow} > {now}");

                fastNow = fastClock.LocalNow;
                now = DateTimeOffset.Now;

                Assert.True(fastNow > now, $"Expected {fastNow} > {now}");
            }

            Assert.Equal(TimeSpan.TicksPerSecond, fastClock.TimestampFrequency);

            long stamp1 = fastClock.GetTimestamp();
            long stamp2 = fastClock.GetTimestamp();

            Assert.Equal(stamp2 - stamp1, fastClock.GetElapsedTime(stamp1, stamp2).Ticks);
        }

        public static IEnumerable<object[]> TimersProvidersListData()
        {
            yield return new object[] { TimeProvider.System };
            yield return new object[] { new FastClock() };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(TimersProvidersListData))]
        public static void CancellationTokenSourceWithTimer(TimeProvider provider)
        {
            //
            // Test out some int-based timeout logic
            //
            CancellationTokenSource cts = new CancellationTokenSource(Timeout.InfiniteTimeSpan, provider); // should be an infinite timeout
            CancellationToken token = cts.Token;
            ManualResetEventSlim mres = new ManualResetEventSlim(false);
            CancellationTokenRegistration ctr = token.Register(() => mres.Set());

            Assert.False(token.IsCancellationRequested,
               "CancellationTokenSourceWithTimer:  Cancellation signaled on infinite timeout (int)!");

            cts.CancelAfter(1000000);

            Assert.False(token.IsCancellationRequested,
               "CancellationTokenSourceWithTimer:  Cancellation signaled on super-long timeout (int) !");

            cts.CancelAfter(1);

            Debug.WriteLine("CancellationTokenSourceWithTimer: > About to wait on cancellation that should occur soon (int)... if we hang, something bad happened");

            mres.Wait();

            cts.Dispose();

            //
            // Test out some TimeSpan-based timeout logic
            //
            TimeSpan prettyLong = new TimeSpan(1, 0, 0);
            cts = new CancellationTokenSource(prettyLong, provider);
            token = cts.Token;
            mres = new ManualResetEventSlim(false);
            ctr = token.Register(() => mres.Set());

            Assert.False(token.IsCancellationRequested,
               "CancellationTokenSourceWithTimer:  Cancellation signaled on super-long timeout (TimeSpan,1)!");

            cts.CancelAfter(prettyLong);

            Assert.False(token.IsCancellationRequested,
               "CancellationTokenSourceWithTimer:  Cancellation signaled on super-long timeout (TimeSpan,2) !");

            cts.CancelAfter(new TimeSpan(1000));

            Debug.WriteLine("CancellationTokenSourceWithTimer: > About to wait on cancellation that should occur soon (TimeSpan)... if we hang, something bad happened");

            mres.Wait();

            cts.Dispose();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(TimersProvidersListData))]
        public static void RunDelayTests(TimeProvider provider)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            // These should all complete quickly, with RAN_TO_COMPLETION status.
            Task task1 = Task.Delay(new TimeSpan(0), provider);
            Task task2 = Task.Delay(new TimeSpan(0), provider, token);

            Debug.WriteLine("RunDelayTests:    > Waiting for 0-delayed uncanceled tasks to complete.  If we hang, something went wrong.");
            try
            {
                Task.WaitAll(task1, task2);
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("RunDelayTests:    > FAILED.  Unexpected exception on WaitAll(simple tasks): {0}", e));
            }

            Assert.True(task1.Status == TaskStatus.RanToCompletion, "    > FAILED.  Expected Delay(TimeSpan(0), timeProvider) to run to completion");
            Assert.True(task2.Status == TaskStatus.RanToCompletion, "    > FAILED.  Expected Delay(TimeSpan(0), timeProvider, uncanceledToken) to run to completion");

            // This should take some time
            Task task3 = Task.Delay(TimeSpan.FromMilliseconds(20000), provider);
            Assert.False(task3.IsCompleted, "RunDelayTests:    > FAILED.  Delay(20000) appears to have completed too soon(1).");
            Task t2 = Task.Delay(TimeSpan.FromMilliseconds(10));
            Assert.False(task3.IsCompleted, "RunDelayTests:    > FAILED.  Delay(10000) appears to have completed too soon(2).");
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(TimersProvidersListData))]
        public static async void RunWaitAsyncTests(TimeProvider provider)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            var tcs1 = new TaskCompletionSource();
            Task task1 = tcs1.Task.WaitAsync(TimeSpan.FromDays(1), provider);
            Assert.False(task1.IsCompleted);
            tcs1.SetResult();
            await task1;

            var tcs2 = new TaskCompletionSource();
            Task task2 = tcs2.Task.WaitAsync(TimeSpan.FromDays(1), provider, cts.Token);
            Assert.False(task2.IsCompleted);
            tcs2.SetResult();
            await task2;

            var tcs3 = new TaskCompletionSource<int>();
            Task<int> task3 = tcs3.Task.WaitAsync(TimeSpan.FromDays(1), provider);
            Assert.False(task3.IsCompleted);
            tcs3.SetResult(42);
            Assert.Equal(42, await task3);

            var tcs4 = new TaskCompletionSource<int>();
            Task<int> task4 = tcs4.Task.WaitAsync(TimeSpan.FromDays(1), provider, cts.Token);
            Assert.False(task4.IsCompleted);
            tcs4.SetResult(42);
            Assert.Equal(42, await task4);

            using CancellationTokenSource cts1 = new CancellationTokenSource();
            Task task5 = Task.Run(() => { while (!cts1.Token.IsCancellationRequested) { Thread.Sleep(10); } });
            await Assert.ThrowsAsync<TimeoutException>(() => task5.WaitAsync(TimeSpan.FromMilliseconds(10), provider));
            cts1.Cancel();
            await task5;

            using CancellationTokenSource cts2 = new CancellationTokenSource();
            Task task6 = Task.Run(() => { while (!cts2.Token.IsCancellationRequested) { Thread.Sleep(10); } });
            await Assert.ThrowsAsync<TimeoutException>(() => task6.WaitAsync(TimeSpan.FromMilliseconds(10), provider, cts2.Token));
            cts1.Cancel();
            await task5;

            using CancellationTokenSource cts3 = new CancellationTokenSource();
            Task<int> task7 = Task<int>.Run(() => { while (!cts3.Token.IsCancellationRequested) { Thread.Sleep(10); } return 100; });
            await Assert.ThrowsAsync<TimeoutException>(() => task7.WaitAsync(TimeSpan.FromMilliseconds(10), provider));
            cts3.Cancel();
            Assert.Equal(100, await task7);

            using CancellationTokenSource cts4 = new CancellationTokenSource();
            Task<int> task8 = Task<int>.Run(() => { while (!cts4.Token.IsCancellationRequested) { Thread.Sleep(10); } return 200; });
            await Assert.ThrowsAsync<TimeoutException>(() => task8.WaitAsync(TimeSpan.FromMilliseconds(10), provider, cts4.Token));
            cts4.Cancel();
            Assert.Equal(200, await task8);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(TimersProvidersListData))]
        public static async void PeriodicTimerTests(TimeProvider provider)
        {
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1), provider);
            Assert.True(await timer.WaitForNextTickAsync());

            timer.Dispose();
            Assert.False(timer.WaitForNextTickAsync().Result);

            timer.Dispose();
            Assert.False(timer.WaitForNextTickAsync().Result);
        }

        [Fact]
        public static void NegativeTests()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FastClock(-1)); // negative frequency
            Assert.Throws<ArgumentOutOfRangeException>(() => new FastClock(0)); // zero frequency
            Assert.Throws<ArgumentNullException>(() => TimeProvider.FromLocalTimeZone(null));

            Assert.Throws<ArgumentNullException>(() => TimeProvider.System.CreateTimer(null, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan));
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeProvider.System.CreateTimer(obj => { }, null, TimeSpan.FromMilliseconds(-2), Timeout.InfiniteTimeSpan));
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeProvider.System.CreateTimer(obj => { }, null, Timeout.InfiniteTimeSpan, TimeSpan.FromMilliseconds(-2)));

            Assert.Throws<ArgumentNullException>(() => new CancellationTokenSource(Timeout.InfiniteTimeSpan, null));

            Assert.Throws<ArgumentNullException>(() => new PeriodicTimer(TimeSpan.FromMilliseconds(1), null));
        }

        class TimerState
        {
            public TimerState()
            {
                Counter = 0;
                Period = 300;
                TotalTicks = 0;
                UtcNow = DateTimeOffset.UtcNow;
                TokenSource = new CancellationTokenSource();
            }

            public CancellationTokenSource TokenSource { get; set; }
            public int Counter { get; set; }
            public int Period { get; set; }
            public DateTimeOffset UtcNow { get; set; }
            public ITimer Timer { get; set; }
            public long TotalTicks { get; set; }
        };

        // Clock that speeds up the reported time
        class FastClock : TimeProvider
        {
            private long _minutesToAdd;
            private TimeZoneInfo _zone;

            public FastClock(long timestampFrequency = TimeSpan.TicksPerSecond, TimeZoneInfo? zone = null) : base(timestampFrequency)
            {
                _zone = zone ?? TimeZoneInfo.Local;
            }

            public override DateTimeOffset UtcNow
            {
                get
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;

                    _minutesToAdd++;
                    long remainingTicks = (DateTimeOffset.MaxValue.Ticks - now.Ticks);

                    if (_minutesToAdd * TimeSpan.TicksPerMinute > remainingTicks)
                    {
                        _minutesToAdd = 0;
                        return now;
                    }

                    return now.AddMinutes(_minutesToAdd);
                }
            }

            public override TimeZoneInfo LocalTimeZone => _zone;

            public override long GetTimestamp() => UtcNow.Ticks;

            public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
                new FastTimer(callback, state, dueTime, period);
        }

        // Timer that fire faster
        class FastTimer : ITimer
        {
            private Timer _timer;

            public FastTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            {
                if (dueTime != Timeout.InfiniteTimeSpan)
                {
                    dueTime = new TimeSpan(dueTime.Ticks / 2);
                }

                if (period != Timeout.InfiniteTimeSpan)
                {
                    period = new TimeSpan(period.Ticks / 2);
                }

                _timer = new Timer(callback, state, dueTime, period);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (dueTime != Timeout.InfiniteTimeSpan)
                {
                    dueTime = new TimeSpan(dueTime.Ticks / 2);
                }

                if (period != Timeout.InfiniteTimeSpan)
                {
                    period = new TimeSpan(period.Ticks / 2);
                }

                return _timer.Change(dueTime, period);
            }

            public void Dispose() => _timer.Dispose();
            public ValueTask DisposeAsync() => _timer.DisposeAsync();
        }
    }
}
