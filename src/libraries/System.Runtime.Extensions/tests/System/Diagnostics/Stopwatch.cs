// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Diagnostics;
using Xunit;

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
        Sleep();
        Assert.True(watch.IsRunning);
        Assert.True(watch.Elapsed > TimeSpan.Zero);

        watch.Stop();
        Assert.False(watch.IsRunning);

        var e1 = watch.Elapsed;
        Sleep();
        var e2 = watch.Elapsed;
        Assert.Equal(e1, e2);
        Assert.Equal((long)e1.TotalMilliseconds, watch.ElapsedMilliseconds);
    }

    [Fact]
    public static void StartNewAndReset()
    {
        Stopwatch watch = Stopwatch.StartNew();
        Sleep();
        Assert.True(watch.IsRunning);
        watch.Start(); // should be no-op
        Assert.True(watch.IsRunning);
        Assert.True(watch.Elapsed > TimeSpan.Zero);

        watch.Reset();
        Assert.False(watch.IsRunning);
        Assert.Equal(TimeSpan.Zero, watch.Elapsed);
    }

    private static void Sleep()
    {
        s_sleepEvent.WaitOne(1);
    }
}
