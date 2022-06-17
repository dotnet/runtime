// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    public static class StopwatchTests
    {
        private static readonly ManualResetEvent s_sleepEvent = new ManualResetEvent(false);

        private static readonly int s_defaultSleepTimeMs = PlatformDetection.IsBrowser ? 5 : 1;

        [Fact]
        public static void GetTimestamp()
        {
            long ts1 = Stopwatch.GetTimestamp();
            Sleep(s_defaultSleepTimeMs);
            long ts2 = Stopwatch.GetTimestamp();
            Assert.NotEqual(ts1, ts2);
        }

        [Fact]
        public static void ConstructStartAndStop()
        {
            Stopwatch watch = new Stopwatch();
            Assert.False(watch.IsRunning);
            Assert.Equal(TimeSpan.Zero, watch.Elapsed);
            Assert.Equal(0, watch.ElapsedTicks);
            Assert.Equal(0, watch.ElapsedMilliseconds);
            watch.Start();
            Assert.True(watch.IsRunning);
            Sleep(s_defaultSleepTimeMs);
            Assert.True(watch.Elapsed > TimeSpan.Zero);

            watch.Stop();
            Assert.False(watch.IsRunning);

            var e1 = watch.Elapsed;
            Sleep(s_defaultSleepTimeMs);
            var e2 = watch.Elapsed;
            Assert.Equal(e1, e2);
            Assert.Equal((long)e1.TotalMilliseconds, watch.ElapsedMilliseconds);

            var t1 = watch.ElapsedTicks;
            Sleep(s_defaultSleepTimeMs);
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
            Sleep(s_defaultSleepTimeMs);
            Assert.True(watch.Elapsed > TimeSpan.Zero);

            watch.Reset();
            Assert.False(watch.IsRunning);
            Assert.Equal(TimeSpan.Zero, watch.Elapsed);
            Assert.Equal(0, watch.ElapsedTicks);
            Assert.Equal(0, watch.ElapsedMilliseconds);
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

        [Fact]
        public static void DebuggerAttributesValid()
        {
            Stopwatch watch = new Stopwatch();
            Assert.Equal("00:00:00 (IsRunning = False)", DebuggerAttributes.ValidateDebuggerDisplayReferences(watch));
            watch.Start();
            Thread.Sleep(10);
            Assert.Contains("(IsRunning = True)", DebuggerAttributes.ValidateDebuggerDisplayReferences(watch));
            Assert.DoesNotContain("00:00:00 ", DebuggerAttributes.ValidateDebuggerDisplayReferences(watch));
            watch.Stop();
            Assert.Contains("(IsRunning = False)", DebuggerAttributes.ValidateDebuggerDisplayReferences(watch));
            Assert.DoesNotContain("00:00:00 ", DebuggerAttributes.ValidateDebuggerDisplayReferences(watch));
        }

        [OuterLoop("Sleeps for relatively long periods of time")]
        [Fact]
        public static void ElapsedMilliseconds_WithinExpectedWindow()
        {
            const int AllowedTries = 30;
            const int SleepTime = 1000;
            const double WindowFactor = 2;

            var results = new List<long>();

            var sw = new Stopwatch();
            for (int trial = 0; trial < AllowedTries; trial++)
            {
                sw.Restart();
                Thread.Sleep(SleepTime);
                sw.Stop();

                if (sw.ElapsedMilliseconds >= (SleepTime / WindowFactor) &&
                    sw.ElapsedMilliseconds <= (SleepTime * WindowFactor))
                {
                    return;
                }

                results.Add(sw.ElapsedMilliseconds);
            }

            Assert.True(false, $"All {AllowedTries} fell outside of {WindowFactor} window of {SleepTime} sleep time: {string.Join(", ", results)}");
        }

        private static void Sleep(int milliseconds)
        {
            s_sleepEvent.WaitOne(milliseconds);
        }
    }
}
