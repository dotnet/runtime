// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Xunit;

public static class StopwatchTests
{
    [Fact]
    public static void TestGetTimestamp()
    {
        // Int64 Stopwatch.GetTimestamp()
        long ts1 = Stopwatch.GetTimestamp();
        Sleep();
        long ts2 = Stopwatch.GetTimestamp();
        Assert.NotEqual(ts1, ts2);
    }

    [Fact]
    public static void TestStartNew()
    {
        Stopwatch watch = new Stopwatch();

        Assert.False(watch.IsRunning);

        watch.Start();

        Sleep();
        Assert.True(watch.IsRunning);
        Assert.True(watch.Elapsed > TimeSpan.Zero);

        watch.Stop();
        var e1 = watch.Elapsed;
        var e2 = watch.Elapsed;
        Assert.Equal(e1, e2);

        Assert.False(watch.IsRunning);
    }

    [Fact]
    public static void Testctor()
    {
        Stopwatch watch = Stopwatch.StartNew();
        Sleep();
        Assert.True(watch.IsRunning);
        watch.Start(); // should be no-op
        Assert.True(watch.IsRunning);
        Assert.True(watch.Elapsed > TimeSpan.Zero);

        watch.Reset();
        Assert.False(watch.IsRunning);
        Assert.True(watch.Elapsed == TimeSpan.Zero);
    }

    private static void Sleep()
    {
        System.Threading.Tasks.Task.Delay(1).Wait();
    }
}
