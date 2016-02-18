// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;

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

    [Theory, MemberData(nameof(TimeSpanTestData))]
    public static void ParseExactTest(string inputTimeSpan, string format, TimeSpan expectedTimeSpan)
    {
        TimeSpan actualTimeSpan = TimeSpan.ParseExact(inputTimeSpan, format, new CultureInfo("en-US"));
        Assert.Equal(expectedTimeSpan, actualTimeSpan);

        bool parsed = TimeSpan.TryParseExact(inputTimeSpan, format, new CultureInfo("en-US"), out actualTimeSpan);
        Assert.True(parsed);
        Assert.Equal(expectedTimeSpan, actualTimeSpan);

        // TimeSpanStyles is interpreted only for custom formats
        if (format != "c" && format != "g" && format != "G")
        {
            actualTimeSpan = TimeSpan.ParseExact(inputTimeSpan, format, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative);
            Assert.Equal(expectedTimeSpan.Negate(), actualTimeSpan);

            parsed = TimeSpan.TryParseExact(inputTimeSpan, format, new CultureInfo("en-US"), TimeSpanStyles.AssumeNegative, out actualTimeSpan);
            Assert.True(parsed);
            Assert.Equal(expectedTimeSpan.Negate(), actualTimeSpan);
        }
    }

    [Theory, MemberData(nameof(TimeSpanTestData_IncorrectFormat))]
    public static void ParseExactTest_Exception(string inputTimeSpan, string format, Type expectedException)
    {
        Assert.Throws(expectedException, () => TimeSpan.ParseExact(inputTimeSpan, format, new CultureInfo("en-US")));

        TimeSpan actualTimeSpan;
        bool parsed = TimeSpan.TryParseExact(inputTimeSpan, format, new CultureInfo("en-US"), out actualTimeSpan);

        Assert.False(parsed);
        Assert.Equal(TimeSpan.Zero, actualTimeSpan);
    }

    public static IEnumerable<object[]> TimeSpanTestData
    {
        get
        {
            return new[]
            {
                //standard timespan formats 'c', 'g', 'G'
                new object[] { "12:24:02", "c", new TimeSpan(0, 12, 24, 2) },
                new object[] { "1.12:24:02", "c", new TimeSpan(1, 12, 24, 2) },
                new object[] { "-01.07:45:16.999", "c", new TimeSpan(1, 7, 45, 16, 999).Negate() },
                new object[] { "12:24:02", "g", new TimeSpan(0, 12, 24, 2) },
                new object[] { "1:12:24:02", "g", new TimeSpan(1, 12, 24, 2) },
                new object[] { "-01:07:45:16.999", "g", new TimeSpan(1, 7, 45, 16, 999).Negate() },
                new object[] { "1:12:24:02.243", "G", new TimeSpan(1, 12, 24, 2, 243) },
                new object[] { "-01:07:45:16.999", "G", new TimeSpan(1, 7, 45, 16, 999).Negate() },

                //custom timespan formats
                new object[] { "12.23:32:43", @"dd\.h\:m\:s", new TimeSpan(12, 23, 32, 43) },
                new object[] { "012.23:32:43.893", @"ddd\.h\:m\:s\.fff", new TimeSpan(12, 23, 32, 43, 893) },
                new object[] { "12.05:02:03", @"d\.hh\:mm\:ss", new TimeSpan(12, 5, 2, 3) },
                new object[] { "12:34 minutes", @"mm\:ss\ \m\i\n\u\t\e\s", new TimeSpan(0, 12, 34) }
            };
        }
    }

    public static IEnumerable<object[]> TimeSpanTestData_IncorrectFormat
    {
        get
        {
            return new[]
            {
                //standard timespan formats 'c', 'g', 'G'
                new object[] { "24:24:02", "c", typeof(OverflowException) },
                new object[] { "1:12:24:02", "c",  typeof(FormatException) },
                new object[] { "12:61:02", "g", typeof(OverflowException)  },
                new object[] { "1.12:24:02", "g", typeof(FormatException) },
                new object[] { "1:07:45:16.99999999", "G", typeof(OverflowException) },
                new object[] { "1:12:24:02", "G", typeof(FormatException) },

                //custom timespan formats
                new object[] { "12.35:32:43", @"dd\.h\:m\:s", typeof(OverflowException) },
                new object[] { "12.5:2:3", @"d\.hh\:mm\:ss", typeof(FormatException) },
                new object[] { "12.5:2", @"d\.hh\:mm\:ss", typeof(FormatException) }
            };
        }
    }
}
