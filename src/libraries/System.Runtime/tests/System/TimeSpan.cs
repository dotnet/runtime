// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public static unsafe class TimeSpanTests
{
    [Fact]
    public static void TestConstructors()
    {
        // TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds);
        TimeSpan ts = new TimeSpan(10, 9, 8, 7, 6);

        Assert.Equal(ts.Days, 10);
        Assert.Equal(ts.Hours, 9);
        Assert.Equal(ts.Minutes, 8);
        Assert.Equal(ts.Seconds, 7);
        Assert.Equal(ts.Milliseconds, 6);
    }

    [Fact]
    public static void TestLimits()
    {
        // Maximum TimeSpan       10675199.02:48:05.4775807
        // Minimum TimeSpan      -10675199.02:48:05.4775808

        Assert.Equal(TimeSpan.MaxValue.Days, 10675199);
        Assert.Equal(TimeSpan.MaxValue.Hours, 2);
        Assert.Equal(TimeSpan.MaxValue.Minutes, 48);
        Assert.Equal(TimeSpan.MaxValue.Seconds, 5);

        Assert.Equal(TimeSpan.MinValue.Days, -10675199);
        Assert.Equal(TimeSpan.MinValue.Hours, -2);
        Assert.Equal(TimeSpan.MinValue.Minutes, -48);
        Assert.Equal(TimeSpan.MinValue.Seconds, -5);

        // Zero TimeSpan                   00:00:00
        Assert.Equal(TimeSpan.Zero, new TimeSpan(0, 0, 0));
    }

    [Fact]
    public static void TestGetandSetParts()
    {
        // TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds);
        TimeSpan ts = new TimeSpan(10, 9, 8, 7, 6);

        TimeSpan spanRes = TimeSpan.FromDays(ts.Days);
        Assert.Equal(new TimeSpan(ts.Days, 0, 0, 0, 0), spanRes);

        spanRes = TimeSpan.FromHours(ts.Hours);
        Assert.Equal(new TimeSpan(0, ts.Hours, 0, 0, 0), spanRes);

        spanRes = TimeSpan.FromMinutes(ts.Minutes);
        Assert.Equal(new TimeSpan(0, 0, ts.Minutes, 0, 0), spanRes);

        spanRes = TimeSpan.FromSeconds(ts.Seconds);
        Assert.Equal(new TimeSpan(0, 0, 0, ts.Seconds, 0), spanRes);

        spanRes = TimeSpan.FromMilliseconds(ts.Milliseconds);
        Assert.Equal(new TimeSpan(0, 0, 0, 0, ts.Milliseconds), spanRes);
    }
}
