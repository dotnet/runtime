// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    public static class StopwatchTests
    {
        private static readonly ManualResetEvent s_sleepEvent = new ManualResetEvent(false);

        [Fact]
        public static void GetTimestamp()
        {
            long ts1 = Stopwatch.GetTimestamp();
            Sleep();
            long ts2 = Stopwatch.GetTimestamp();
            Assert.NotEqual(ts1, ts2);
        }

        [Fact]
        public static void ConstructStartAndStop()
        {
            Stopwatch watch = new Stopwatch();
            Assert.False(watch.IsRunning);
            watch.Start();
            Assert.True(watch.IsRunning);
            Sleep();
            Assert.True(watch.Elapsed > TimeSpan.Zero);

            watch.Stop();
            Assert.False(watch.IsRunning);

            var e1 = watch.Elapsed;
            Sleep();
            var e2 = watch.Elapsed;
            Assert.Equal(e1, e2);
            Assert.Equal((long)e1.TotalMilliseconds, watch.ElapsedMilliseconds);

            var t1 = watch.ElapsedTicks;
            Sleep();
            var t2 = watch.ElapsedTicks;
            Assert.Equal(t1, t2);
        }

        [Fact]
        public static void StartNewAndReset()
        {
            Stopwatch watch = Stopwatch.StartNew();
            Assert.True(watch.IsRunning);
            watch.Start(); // should be no-op
            Assert.True(watch.IsRunning);
            Sleep();
            Assert.True(watch.Elapsed > TimeSpan.Zero);

            watch.Reset();
            Assert.False(watch.IsRunning);
            Assert.Equal(TimeSpan.Zero, watch.Elapsed);
        }

        [Fact]
        public static void StartNewAndRestart()
        {
            Stopwatch watch = Stopwatch.StartNew();
            Assert.True(watch.IsRunning);
            Sleep(10);
            TimeSpan elapsedSinceStart = watch.Elapsed;
            Assert.True(elapsedSinceStart > TimeSpan.Zero);

            const int MaxAttempts = 5; // The comparison below could fail if we get very unlucky with when the thread gets preempted
            int attempt = 0;
            while (true)
            {
                watch.Restart();
                Assert.True(watch.IsRunning);
                try
                {
                    Assert.True(watch.Elapsed < elapsedSinceStart);
                }
                catch
                {
                    if (++attempt < MaxAttempts) continue;
                    throw;
                }
                break;
            }
        }

        private static void Sleep(int milliseconds = 1)
        {
            s_sleepEvent.WaitOne(milliseconds);
        }
    }
}
