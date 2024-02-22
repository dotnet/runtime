// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Threading;
using System.Threading.Tasks;
using TestTimer = System.Timers.Timer;

namespace System.Timers.Tests
{
    public class TimerTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-0.5D)]
        [InlineData(-1)]
        [InlineData(double.MinValue)]
        [InlineData(double.MaxValue)]
        public void NegativeTests(double interval)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new TestTimer(interval));
        }

        [Fact]
        public void Ctor_PropertiesMatchExpectedDefaults()
        {
            using (var timer = new TestTimer(42))
            {
                Assert.Equal(42, timer.Interval);
                Assert.True(timer.AutoReset, "Expected AutoReset to be true");
                Assert.False(timer.Enabled, "Expected Enabled to be false");

                Assert.Null(timer.SynchronizingObject);
                Assert.Null(timer.Site);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public void TestTimerStartAutoReset()
        {
            using (var timer = new TestTimer(1))
            {
                var mres = new ManualResetEventSlim();
                int count = 0;
                int target = 1;

                timer.AutoReset = false;
                timer.Elapsed += (sender, e) =>
                {
                    if (Interlocked.Increment(ref count) == target)
                    {
                        mres.Set();
                    }
                };
                timer.Start();

                mres.Wait();
                Assert.False(timer.Enabled, "Auto-reset timer should not be enabled after elapsed");
                Assert.Equal(1, count);

                count = 0;
                target = 10;
                mres.Reset();
                timer.AutoReset = true;
                mres.Wait();

                timer.Stop();
                Assert.InRange(count, target, int.MaxValue);
            }
        }

        [Fact]
        public async Task ElapsedEventArgs_MatchesExpectedValues()
        {
            using (var timer = new TestTimer(1) { AutoReset = false })
            {
                DateTime start = DateTime.Now;
                var tcs = new TaskCompletionSource<ElapsedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
                timer.Elapsed += (sender, e) => tcs.SetResult(e);
                timer.Start();

                ElapsedEventArgs e = await tcs.Task;
                Assert.False(timer.Enabled);

                timer.Stop();
                DateTime end = DateTime.Now;

                const int WiggleRoomSeconds = 5;
                Assert.Equal(DateTimeKind.Local, e.SignalTime.Kind);
                Assert.InRange(
                    e.SignalTime.ToUniversalTime(),
                    start.ToUniversalTime() - TimeSpan.FromSeconds(WiggleRoomSeconds),
                    end.ToUniversalTime() + TimeSpan.FromSeconds(WiggleRoomSeconds));
            }
        }

        [Fact]
        public void ElapsedEventArgs_Ctor_SignalTime()
        {
            DateTime now = DateTime.Now;
            ElapsedEventArgs args = new ElapsedEventArgs(now);
            Assert.Equal(now, args.SignalTime);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(0.5D)]
        public void TestInterval(double interval)
        {
            using (var timer = new TestTimer(interval))
            {
                Assert.Equal(Math.Ceiling(interval), timer.Interval);
                timer.Interval = interval;
                Assert.Equal(interval, timer.Interval);
            }
        }
    }
}
