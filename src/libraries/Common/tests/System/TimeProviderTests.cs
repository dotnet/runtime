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
            DateTimeOffset providerDto = TimeProvider.System.GetUtcNow();
            DateTimeOffset dto2 = DateTimeOffset.UtcNow;

            Assert.InRange(providerDto.Ticks, dto1.Ticks, dto2.Ticks);
            Assert.Equal(TimeSpan.Zero, providerDto.Offset);
        }

        [Fact]
        public void TestLocalSystemTime()
        {
            DateTimeOffset dto1 = DateTimeOffset.Now;
            DateTimeOffset providerDto = TimeProvider.System.GetLocalNow();
            DateTimeOffset dto2 = DateTimeOffset.Now;

            // Ensure there was no daylight saving shift during the test execution.
            if (dto1.Offset == dto2.Offset)
            {
                Assert.InRange(providerDto.Ticks, dto1.Ticks, dto2.Ticks);
                Assert.Equal(dto1.Offset, providerDto.Offset);
            }
        }

        private class ZonedTimeProvider : TimeProvider
        {
            private TimeZoneInfo _zoneInfo;
            public ZonedTimeProvider(TimeZoneInfo zoneInfo) : base()
            {
                _zoneInfo = zoneInfo ?? TimeZoneInfo.Local;
            }
            public override TimeZoneInfo LocalTimeZone { get => _zoneInfo; }
            public static TimeProvider FromLocalTimeZone(TimeZoneInfo zoneInfo) => new ZonedTimeProvider(zoneInfo);
        }

        [Fact]
        public void TestSystemProviderWithTimeZone()
        {
            Assert.Equal(TimeZoneInfo.Local.Id, TimeProvider.System.LocalTimeZone.Id);

#if NETFRAMEWORK
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
#else
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Pacific Standard Time" : "America/Los_Angeles");
#endif // NETFRAMEWORK

            TimeProvider tp = ZonedTimeProvider.FromLocalTimeZone(tzi);
            Assert.Equal(tzi.Id, tp.LocalTimeZone.Id);

            DateTimeOffset utcDto1 = DateTimeOffset.UtcNow;
            DateTimeOffset localDto = tp.GetLocalNow();
            DateTimeOffset utcDto2 = DateTimeOffset.UtcNow;

            DateTimeOffset utcConvertedDto = TimeZoneInfo.ConvertTime(localDto, TimeZoneInfo.Utc);
            Assert.InRange(utcConvertedDto.Ticks, utcDto1.Ticks, utcDto2.Ticks);
        }

#if NETFRAMEWORK
        public static double s_tickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
        public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
            new TimeSpan((long)((endingTimestamp - startingTimestamp) * s_tickFrequency));
#else
        public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
                        Stopwatch.GetElapsedTime(startingTimestamp, endingTimestamp);
#endif // NETFRAMEWORK

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
            Assert.Equal(GetElapsedTime(providerTimestamp1, providerTimestamp2), TimeProvider.System.GetElapsedTime(providerTimestamp1, providerTimestamp2));

            long timestamp = TimeProvider.System.GetTimestamp();
            TimeSpan period1 = TimeProvider.System.GetElapsedTime(timestamp);
            TimeSpan period2 = TimeProvider.System.GetElapsedTime(timestamp);
            Assert.True(period1 <= period2);

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
                                    lock (s)
                                    {
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
                                    }
                                },
                            state,
                            TimeSpan.FromMilliseconds(state.Period), TimeSpan.FromMilliseconds(state.Period));

            state.TokenSource.Token.WaitHandle.WaitOne(Timeout.InfiniteTimeSpan);
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
                DateTimeOffset fastNow = fastClock.GetUtcNow();
                DateTimeOffset now = DateTimeOffset.UtcNow;

                Assert.True(fastNow > now, $"Expected {fastNow} > {now}");

                fastNow = fastClock.GetLocalNow();
                now = DateTimeOffset.Now;

                Assert.True(fastNow > now, $"Expected {fastNow} > {now}");
            }

            Assert.Equal(TimeSpan.TicksPerSecond, fastClock.TimestampFrequency);

            long stamp1 = fastClock.GetTimestamp();
            long stamp2 = fastClock.GetTimestamp();

            Assert.Equal(stamp2 - stamp1, fastClock.GetElapsedTime(stamp1, stamp2).Ticks);
        }

        public class DerivedTimeProvider : TimeProvider
        {
        }

        public static IEnumerable<object[]> TimersProvidersListData()
        {
            yield return new object[] { TimeProvider.System };
            yield return new object[] { new FastClock() };
            yield return new object[] { new DerivedTimeProvider() };
        }

        public static IEnumerable<object[]> TimersProvidersWithTaskFactorData()
        {
            yield return new object[] { TimeProvider.System, taskFactory};
            yield return new object[] { new FastClock(), taskFactory };

#if TESTEXTENSIONS
            yield return new object[] { TimeProvider.System, extensionsTaskFactory};
            yield return new object[] { new FastClock(), extensionsTaskFactory };
#endif // TESTEXTENSIONS
        }

#if NETFRAMEWORK
        private static void CancelAfter(TimeProvider provider, CancellationTokenSource cts, TimeSpan delay)
        {
            if (provider == TimeProvider.System)
            {
                cts.CancelAfter(delay);
            }
            else
            {
                ITimer timer = provider.CreateTimer(s => ((CancellationTokenSource)s).Cancel(), cts, delay, Timeout.InfiniteTimeSpan);
                cts.Token.Register(t => ((ITimer)t).Dispose(), timer);
            }
        }
#endif // NETFRAMEWORK

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [MemberData(nameof(TimersProvidersListData))]
        public static void CancellationTokenSourceWithTimer(TimeProvider provider)
        {
            //
            // Test out some int-based timeout logic
            //
#if TESTEXTENSIONS
            CancellationTokenSource cts = provider.CreateCancellationTokenSource(Timeout.InfiniteTimeSpan); // should be an infinite timeout
#else
            CancellationTokenSource cts = new CancellationTokenSource(Timeout.InfiniteTimeSpan, provider); // should be an infinite timeout
#endif // TESTEXTENSIONS
            ManualResetEventSlim mres = new ManualResetEventSlim(false);

            Assert.False(cts.Token.IsCancellationRequested,
               "CancellationTokenSourceWithTimer:  Cancellation signaled on infinite timeout (int)!");

#if TESTEXTENSIONS
            cts.Dispose();
            cts = provider.CreateCancellationTokenSource(TimeSpan.FromMilliseconds(1000000));
#else
            cts.CancelAfter(1000000);
#endif // TESTEXTENSIONS

            Assert.False(cts.Token.IsCancellationRequested,
               "CancellationTokenSourceWithTimer:  Cancellation signaled on super-long timeout (int) !");

#if TESTEXTENSIONS
            cts.Dispose();
            cts = provider.CreateCancellationTokenSource(TimeSpan.FromMilliseconds(1));
#else
            cts.CancelAfter(1);
#endif // TESTEXTENSIONS

            CancellationTokenRegistration ctr = cts.Token.Register(() => mres.Set());
            Debug.WriteLine("CancellationTokenSourceWithTimer: > About to wait on cancellation that should occur soon (int)... if we hang, something bad happened");

            mres.Wait();

            cts.Dispose();

            //
            // Test out some TimeSpan-based timeout logic
            //
            TimeSpan prettyLong = new TimeSpan(1, 0, 0);
#if TESTEXTENSIONS
            cts = provider.CreateCancellationTokenSource(prettyLong);
#else
            cts = new CancellationTokenSource(prettyLong, provider);
#endif // TESTEXTENSIONS

            mres = new ManualResetEventSlim(false);

            Assert.False(cts.Token.IsCancellationRequested,
               "CancellationTokenSourceWithTimer:  Cancellation signaled on super-long timeout (TimeSpan,1)!");

#if TESTEXTENSIONS
            cts.Dispose();
            cts = provider.CreateCancellationTokenSource(prettyLong);
#else
            cts.CancelAfter(prettyLong);
#endif // TESTEXTENSIONS

            Assert.False(cts.Token.IsCancellationRequested,
               "CancellationTokenSourceWithTimer:  Cancellation signaled on super-long timeout (TimeSpan,2) !");

#if TESTEXTENSIONS
            cts.Dispose();
            cts = provider.CreateCancellationTokenSource(TimeSpan.FromMilliseconds(1000));
#else
            cts.CancelAfter(new TimeSpan(1000));
#endif // TESTEXTENSIONS
            ctr = cts.Token.Register(() => mres.Set());

            Debug.WriteLine("CancellationTokenSourceWithTimer: > About to wait on cancellation that should occur soon (TimeSpan)... if we hang, something bad happened");

            mres.Wait();

            cts.Dispose();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(TimersProvidersWithTaskFactorData))]
        public static void RunDelayTests(TimeProvider provider, ITestTaskFactory taskFactory)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            // These should all complete quickly, with RAN_TO_COMPLETION status.
            Task task1 = taskFactory.Delay(provider, new TimeSpan(0));
            Task task2 = taskFactory.Delay(provider, new TimeSpan(0), token);

            Debug.WriteLine("RunDelayTests:    > Waiting for 0-delayed uncanceled tasks to complete.  If we hang, something went wrong.");
            try
            {
                Task.WaitAll(task1, task2);
            }
            catch (Exception e)
            {
                Assert.Fail(string.Format("RunDelayTests:    > FAILED.  Unexpected exception on WaitAll(simple tasks): {0}", e));
            }

            Assert.True(task1.Status == TaskStatus.RanToCompletion, "    > FAILED.  Expected Delay(TimeSpan(0), timeProvider) to run to completion");
            Assert.True(task2.Status == TaskStatus.RanToCompletion, "    > FAILED.  Expected Delay(TimeSpan(0), timeProvider, uncanceledToken) to run to completion");

            // This should take some time
            Task task3 = taskFactory.Delay(provider, TimeSpan.FromMilliseconds(20000));

            Assert.False(task3.IsCompleted, "RunDelayTests:    > FAILED.  Delay(20000) appears to have completed too soon(1).");
            Task t2 = Task.Delay(TimeSpan.FromMilliseconds(10));
            Assert.False(task3.IsCompleted, "RunDelayTests:    > FAILED.  Delay(10000) appears to have completed too soon(2).");
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(TimersProvidersWithTaskFactorData))]
        public static async Task RunWaitAsyncTests(TimeProvider provider, ITestTaskFactory taskFactory)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            var tcs1 = new TaskCompletionSource<bool>();
            Task task1 = taskFactory.WaitAsync(tcs1.Task, TimeSpan.FromDays(1), provider);
            Assert.False(task1.IsCompleted);
            tcs1.SetResult(true);
            await task1;

            var tcs2 = new TaskCompletionSource<bool>();
            Task task2 = taskFactory.WaitAsync(tcs2.Task, TimeSpan.FromDays(1), provider, cts.Token);
            Assert.False(task2.IsCompleted);
            tcs2.SetResult(true);
            await task2;

            var tcs3 = new TaskCompletionSource<int>();
            Task<int> task3 = taskFactory.WaitAsync<int>(tcs3.Task, TimeSpan.FromDays(1), provider);
            Assert.False(task3.IsCompleted);
            tcs3.SetResult(42);
            Assert.Equal(42, await task3);

            var tcs4 = new TaskCompletionSource<int>();
            Task<int> task4 = taskFactory.WaitAsync<int>(tcs4.Task, TimeSpan.FromDays(1), provider, cts.Token);
            Assert.False(task4.IsCompleted);
            tcs4.SetResult(42);
            Assert.Equal(42, await task4);

            var tcs5 = new TaskCompletionSource<bool>();
            await Assert.ThrowsAsync<TimeoutException>(() => taskFactory.WaitAsync(tcs5.Task, TimeSpan.Zero, provider, cts.Token));

            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => taskFactory.WaitAsync(tcs5.Task, TimeSpan.FromMilliseconds(10), provider, cts.Token));

            using CancellationTokenSource cts1 = new CancellationTokenSource();
            Task task5 = Task.Run(() => { while (!cts1.Token.IsCancellationRequested) { Thread.Sleep(10); } });
            await Assert.ThrowsAsync<TimeoutException>(() => taskFactory.WaitAsync(task5, TimeSpan.FromMilliseconds(10), provider));
            cts1.Cancel();
            await task5;

            using CancellationTokenSource cts2 = new CancellationTokenSource();
            Task task6 = Task.Run(() => { while (!cts2.Token.IsCancellationRequested) { Thread.Sleep(10); } });
            await Assert.ThrowsAsync<TimeoutException>(() => taskFactory.WaitAsync(task6, TimeSpan.FromMilliseconds(10), provider, cts2.Token));
            cts2.Cancel();
            await task6;

            using CancellationTokenSource cts3 = new CancellationTokenSource();
            Task<int> task7 = Task<int>.Run(() => { while (!cts3.Token.IsCancellationRequested) { Thread.Sleep(10); } return 100; });
            await Assert.ThrowsAsync<TimeoutException>(() => taskFactory.WaitAsync<int>(task7, TimeSpan.FromMilliseconds(10), provider));
            cts3.Cancel();
            Assert.Equal(100, await task7);

            using CancellationTokenSource cts4 = new CancellationTokenSource();
            Task<int> task8 = Task<int>.Run(() => { while (!cts4.Token.IsCancellationRequested) { Thread.Sleep(10); } return 200; });
            await Assert.ThrowsAsync<TimeoutException>(() => taskFactory.WaitAsync<int>(task8, TimeSpan.FromMilliseconds(10), provider, cts4.Token));
            cts4.Cancel();
            Assert.Equal(200, await task8);
        }

#if !NETFRAMEWORK
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(TimersProvidersListData))]
        public static async Task PeriodicTimerTests(TimeProvider provider)
        {
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1), provider);
            Assert.True(await timer.WaitForNextTickAsync());

            timer.Dispose();
            Assert.False(timer.WaitForNextTickAsync().Result);

            timer.Dispose();
            Assert.False(timer.WaitForNextTickAsync().Result);
        }
#endif // !NETFRAMEWORK

        [Fact]
        public static void NegativeTests()
        {
            FastClock clock = new FastClock(-1);  // negative frequency
            Assert.Throws<InvalidOperationException>(() => clock.GetElapsedTime(1, 2));
            clock = new FastClock(0); // zero frequency
            Assert.Throws<InvalidOperationException>(() => clock.GetElapsedTime(1, 2));

            Assert.Throws<ArgumentNullException>(() => TimeProvider.System.CreateTimer(null, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan));
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeProvider.System.CreateTimer(obj => { }, null, TimeSpan.FromMilliseconds(-2), Timeout.InfiniteTimeSpan));
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeProvider.System.CreateTimer(obj => { }, null, Timeout.InfiniteTimeSpan, TimeSpan.FromMilliseconds(-2)));

#if !NETFRAMEWORK
            Assert.Throws<ArgumentNullException>(() => new CancellationTokenSource(Timeout.InfiniteTimeSpan, null));

            Assert.Throws<ArgumentNullException>(() => new PeriodicTimer(TimeSpan.FromMilliseconds(1), null));
#endif // !NETFRAMEWORK
        }

#if TESTEXTENSIONS
        [Fact]
        public static void InvokeCallbackFromCreateTimer()
        {
            TimeProvider p = new InvokeCallbackCreateTimerProvider();

            CancellationTokenSource cts = p.CreateCancellationTokenSource(TimeSpan.FromSeconds(0));
            Assert.True(cts.IsCancellationRequested);

            Task t = p.Delay(TimeSpan.FromSeconds(0));
            Assert.True(t.IsCompleted);

            t = new TaskCompletionSource<bool>().Task.WaitAsync(TimeSpan.FromSeconds(0), p);
            Assert.True(t.IsFaulted);
        }

        class InvokeCallbackCreateTimerProvider : TimeProvider
        {
            public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            {
                ITimer t = base.CreateTimer(callback, state, dueTime, period);
                if (dueTime != Timeout.InfiniteTimeSpan)
                {
                    callback(state);
                }
                return t;
            }
        }
#endif

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
            private long _timestampFrequency;

            public FastClock(long timestampFrequency = TimeSpan.TicksPerSecond, TimeZoneInfo? zone = null) : base()
            {
                _timestampFrequency = timestampFrequency;
                _zone = zone ?? TimeZoneInfo.Local;
            }

            public override DateTimeOffset GetUtcNow()
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

            public override long TimestampFrequency { get => _timestampFrequency; }

            public override TimeZoneInfo LocalTimeZone => _zone;

            public override long GetTimestamp() => GetUtcNow().Ticks;

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

                try
                {
                    return _timer.Change(dueTime, period);
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }

            public void Dispose() => _timer.Dispose();

#if NETFRAMEWORK
            public ValueTask DisposeAsync()
            {
                _timer.Dispose();
                return default;
            }
#else
            public ValueTask DisposeAsync() => _timer.DisposeAsync();
#endif // NETFRAMEWORK
        }

        public interface ITestTaskFactory
        {
            Task Delay(TimeProvider provider, TimeSpan delay, CancellationToken cancellationToken = default);
            Task WaitAsync(Task task, TimeSpan timeout, TimeProvider provider, CancellationToken cancellationToken = default);
            Task<TResult> WaitAsync<TResult>(Task<TResult> task, TimeSpan timeout, TimeProvider provider, CancellationToken cancellationToken = default);
        }

        private class TestTaskFactory : ITestTaskFactory
        {
            public Task Delay(TimeProvider provider, TimeSpan delay, CancellationToken cancellationToken = default)
            {
#if NETFRAMEWORK
                return provider.Delay(delay, cancellationToken);
#else
                return Task.Delay(delay, provider, cancellationToken);
#endif // NETFRAMEWORK
            }

            public Task WaitAsync(Task task, TimeSpan timeout, TimeProvider provider, CancellationToken cancellationToken = default)
                => task.WaitAsync(timeout, provider, cancellationToken);

            public Task<TResult> WaitAsync<TResult>(Task<TResult> task, TimeSpan timeout, TimeProvider provider, CancellationToken cancellationToken = default)
                => task.WaitAsync(timeout, provider, cancellationToken);
        }

        private static TestTaskFactory taskFactory = new();

#if TESTEXTENSIONS
        private class TestExtensionsTaskFactory : ITestTaskFactory
        {
            public Task Delay(TimeProvider provider, TimeSpan delay, CancellationToken cancellationToken = default)
                => TimeProviderTaskExtensions.Delay(provider, delay, cancellationToken);

            public Task WaitAsync(Task task, TimeSpan timeout, TimeProvider provider, CancellationToken cancellationToken = default)
                => TimeProviderTaskExtensions.WaitAsync(task, timeout, provider, cancellationToken);

            public Task<TResult> WaitAsync<TResult>(Task<TResult> task, TimeSpan timeout, TimeProvider provider, CancellationToken cancellationToken = default)
                => TimeProviderTaskExtensions.WaitAsync<TResult>(task, timeout, provider, cancellationToken);
        }

        private static TestExtensionsTaskFactory extensionsTaskFactory = new();
#endif // TESTEXTENSIONS

        // A timer that get fired on demand
        private class ManualTimer : ITimer
        {
            TimerCallback _callback;
            object? _state;

            public ManualTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            {
                _callback = callback;
                _state = state;
            }

            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Fire()
            {
                _callback?.Invoke(_state);
                IsFired = true;
            }

            public bool IsFired { get; set; }

            public void Dispose() { }
            public ValueTask DisposeAsync () { return default; }
        }

        private class ManualTimeProvider : TimeProvider
        {
            public ManualTimer Timer { get; set; }

            public ManualTimeProvider() { }
            public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            {
                Timer = new ManualTimer(callback, state, dueTime, period);
                return Timer;
            }
        }

        public static IEnumerable<object[]> TaskFactoryData()
        {
            yield return new object[] { taskFactory };

#if TESTEXTENSIONS
            yield return new object[] { extensionsTaskFactory };
#endif // TESTEXTENSIONS
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(TaskFactoryData))]
        public async Task TestDelayTaskContinuation(ITestTaskFactory taskFactory)
        {
            //
            // Test time expiration and validate continuation is called synchronously.
            //

            ManualTimeProvider manualTimeProvider = new ManualTimeProvider();
            int callbackCount = 0;

            _ = Continuation(manualTimeProvider, default, () => callbackCount++);

             Assert.NotNull(manualTimeProvider.Timer);
             manualTimeProvider.Timer.Fire();

            // Delay should be completed and the continuation should be called synchronously.
            Assert.Equal(1, callbackCount);

            //
            // Test cancellation and validate continuation is called asynchronously.
            //

            manualTimeProvider = new ManualTimeProvider();
            CancellationTokenSource cts = new CancellationTokenSource();

            var tl = new ThreadLocal<int>();
            tl.Value = 10;
            int t1Value = 0;

            Task task = Continuation(manualTimeProvider, cts.Token, () =>  t1Value = tl.Value);
            cts.Cancel();

            // reset the thread local value as the continuation callback could end up running on this thread pool thread.
            tl.Value = 0;
            await task;

            Assert.NotEqual(10, t1Value);

            async Task Continuation(TimeProvider timeProvider, CancellationToken token, Action callback)
            {
                try
                {
                    await taskFactory.Delay(timeProvider, TimeSpan.FromSeconds(10), token);
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                }

                callback();
            }
        }

        [Fact]
        // 1- Creates the CTS with a delay that we control via the time provider.
        // 2- Disposes the CTS.
        // 3- Then fires the timer. We want to validate the process doesn't crash.
        public static void TestCTSWithDelayFiringAfterCancellation()
        {
            ManualTimeProvider manualTimer = new ManualTimeProvider();
#if TESTEXTENSIONS
            CancellationTokenSource cts = manualTimer.CreateCancellationTokenSource(TimeSpan.FromSeconds(60));
#else
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(60), manualTimer);
#endif // TESTEXTENSIONS

            Assert.NotNull(manualTimer.Timer);
            Assert.False(manualTimer.Timer.IsFired);

            cts.Dispose();

            manualTimer.Timer.Fire();
            Assert.True(manualTimer.Timer.IsFired);
        }
    }
}
