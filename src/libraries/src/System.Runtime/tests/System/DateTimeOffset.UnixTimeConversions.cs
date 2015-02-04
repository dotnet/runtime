// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public static class DateTimeOffsetUnixTimeConversionTests
{
    private class TestTime
    {
        private TestTime(DateTimeOffset dateTimeOffset, long unixTimeMilliseconds, long unixTimeSeconds)
        {
            DateTimeOffset = dateTimeOffset;
            UnixTimeMilliseconds = unixTimeMilliseconds;
            UnixTimeSeconds = unixTimeSeconds;
        }

        public static TestTime FromMilliseconds(DateTimeOffset dateTimeOffset, long unixTimeMilliseconds)
        {
            long unixTimeSeconds = unixTimeMilliseconds / 1000;

            // Always round UnixTimeSeconds down toward 1/1/0001 00:00:00
            // (this happens automatically for unixTimeMilliseconds > 0)
            bool hasSubSecondPrecision = unixTimeMilliseconds % 1000 != 0;
            if (unixTimeMilliseconds < 0 && hasSubSecondPrecision)
            {
                --unixTimeSeconds;
            }

            return new TestTime(dateTimeOffset, unixTimeMilliseconds, unixTimeSeconds);
        }

        public static TestTime FromSeconds(DateTimeOffset dateTimeOffset, long unixTimeSeconds)
        {
            return new TestTime(dateTimeOffset, unixTimeSeconds * 1000, unixTimeSeconds);
        }

        public DateTimeOffset DateTimeOffset { get; private set; }
        public long UnixTimeMilliseconds { get; private set; }
        public long UnixTimeSeconds { get; private set; }
    }

    private static readonly TestTime[] s_testTimes = {
        TestTime.FromMilliseconds(DateTimeOffset.MinValue, -62135596800000),
        TestTime.FromMilliseconds(DateTimeOffset.MaxValue, 253402300799999),
        TestTime.FromMilliseconds(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero), 0),
        TestTime.FromMilliseconds(new DateTimeOffset(2014, 6, 13, 17, 21, 50, TimeSpan.Zero), 1402680110000),
        TestTime.FromMilliseconds(new DateTimeOffset(2830, 12, 15, 1, 23, 45, TimeSpan.Zero), 27169089825000),
        TestTime.FromMilliseconds(new DateTimeOffset(2830, 12, 15, 1, 23, 45, 399, TimeSpan.Zero), 27169089825399),
        TestTime.FromMilliseconds(new DateTimeOffset(9999, 12, 30, 23, 24, 25, TimeSpan.Zero), 253402212265000),
        TestTime.FromMilliseconds(new DateTimeOffset(1907, 7, 7, 7, 7, 7, TimeSpan.Zero), -1971967973000),
        TestTime.FromMilliseconds(new DateTimeOffset(1907, 7, 7, 7, 7, 7, 1, TimeSpan.Zero), -1971967972999),
        TestTime.FromMilliseconds(new DateTimeOffset(1907, 7, 7, 7, 7, 7, 777, TimeSpan.Zero), -1971967972223),
        TestTime.FromMilliseconds(new DateTimeOffset(601636288270011234, TimeSpan.Zero), -1971967972999)
    };

    [Fact]
    public static void TestToUnixTime()
    {
        foreach (var test in s_testTimes)
        {
            TestToUnixTimeMilliseconds(test);
            TestToUnixTimeSeconds(test);
        }
    }

    private static void TestToUnixTimeMilliseconds(TestTime test)
    {
        long expectedMilliseconds = test.UnixTimeMilliseconds;
        long actualMilliseconds = test.DateTimeOffset.ToUnixTimeMilliseconds();
        if (expectedMilliseconds != actualMilliseconds)
        {
            Assert.True(false,
                string.Format("Unix time converted from DateTimeOffset {0} ticks is inaccurate, expected: {1} ms, actual: {2} ms",
                    test.DateTimeOffset.UtcTicks, expectedMilliseconds, actualMilliseconds));
        }
    }

    private static void TestToUnixTimeSeconds(TestTime test)
    {
        long expectedSeconds = test.UnixTimeSeconds;
        long actualSeconds = test.DateTimeOffset.ToUnixTimeSeconds();
        if (expectedSeconds != actualSeconds)
        {
            Assert.True(false,
                string.Format("Unix time converted from DateTimeOffset {0} ticks is inaccurate, expected: {1} seconds, actual: {2} seconds",
                    test.DateTimeOffset.UtcTicks, expectedSeconds, actualSeconds));
        }
    }

    [Fact]
    public static void TestFromUnixTime()
    {
        foreach (var test in s_testTimes)
        {
            TestFromUnixTimeMilliseconds(test);
            TestFromUnixTimeSeconds(test);
        }
    }

    private static void TestFromUnixTimeMilliseconds(TestTime test)
    {
        // Only assert that expected == actual up to millisecond precision for conversion from milliseconds
        long expectedTicks = (test.DateTimeOffset.UtcTicks / TimeSpan.TicksPerMillisecond) * TimeSpan.TicksPerMillisecond;
        long actualTicks = DateTimeOffset.FromUnixTimeMilliseconds(test.UnixTimeMilliseconds).UtcTicks;

        if (expectedTicks != actualTicks)
        {
            Assert.True(false,
                string.Format("DateTimeOffset converted from Unix time {0} ms is inaccurate, expected: {1} ticks, actual: {2} ticks",
                    test.UnixTimeMilliseconds,
                    expectedTicks,
                    actualTicks));
        }
    }

    private static void TestFromUnixTimeSeconds(TestTime test)
    {
        // Only assert that expected == actual up to second precision for conversion from seconds
        long expectedTicks = (test.DateTimeOffset.UtcTicks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
        long actualTicks = DateTimeOffset.FromUnixTimeSeconds(test.UnixTimeSeconds).UtcTicks;

        if (expectedTicks != actualTicks)
        {
            Assert.True(false,
                string.Format("DateTimeOffset converted from Unix time {0} seconds ({1} ms) is inaccurate, expected: {2} ticks, actual: {3} ticks",
                    test.UnixTimeSeconds,
                    test.UnixTimeMilliseconds,
                    expectedTicks,
                    actualTicks));
        }
    }

    [Fact]
    public static void TestFromUnixTimeBounds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DateTimeOffset.FromUnixTimeMilliseconds(long.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => DateTimeOffset.FromUnixTimeSeconds(long.MinValue));

        Assert.Throws<ArgumentOutOfRangeException>(() => DateTimeOffset.FromUnixTimeMilliseconds(long.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => DateTimeOffset.FromUnixTimeSeconds(long.MaxValue));

        // DateTimeOffset.MinValue - 1 unit
        Assert.Throws<ArgumentOutOfRangeException>(() => DateTimeOffset.FromUnixTimeMilliseconds(-62135596800001));
        Assert.Throws<ArgumentOutOfRangeException>(() => DateTimeOffset.FromUnixTimeSeconds(-62135596801));

        // DateTimeOffset.MaxValue + 1 unit
        Assert.Throws<ArgumentOutOfRangeException>(() => DateTimeOffset.FromUnixTimeMilliseconds(253402300800000));
        Assert.Throws<ArgumentOutOfRangeException>(() => DateTimeOffset.FromUnixTimeSeconds(253402300800));
    }

    [Fact]
    public static void TestRoundtripDateTimes()
    {
        foreach (var test in s_testTimes)
        {
            // Roundtrip through Unix time in milliseconds
            long unixTimeMilliseconds = test.DateTimeOffset.ToUnixTimeMilliseconds();
            TestFromUnixTimeMilliseconds(TestTime.FromMilliseconds(test.DateTimeOffset, unixTimeMilliseconds));

            // Roundtrip through Unix time in seconds
            long unixTimeSeconds = test.DateTimeOffset.ToUnixTimeSeconds();
            TestFromUnixTimeSeconds(TestTime.FromSeconds(test.DateTimeOffset, unixTimeSeconds));
        }
    }

    [Fact]
    public static void TestRoundtripUnixTimes()
    {
        foreach (var test in s_testTimes)
        {
            // Roundtrip Unix time in milliseconds
            DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeMilliseconds(test.UnixTimeMilliseconds);
            TestToUnixTimeMilliseconds(TestTime.FromMilliseconds(dateTime, test.UnixTimeMilliseconds));

            // Roundtrip Unix time in seconds
            dateTime = DateTimeOffset.FromUnixTimeSeconds(test.UnixTimeSeconds);
            TestToUnixTimeSeconds(TestTime.FromSeconds(dateTime, test.UnixTimeSeconds));
        }
    }
}