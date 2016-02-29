// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

public static class DateTimeOffsetTests
{
    [Fact]
    public static void TestMaxValue()
    {
        VerifyDateTimeOffset(DateTimeOffset.MaxValue, 9999, 12, 31, 23, 59, 59, 999, TimeSpan.Zero);
    }

    [Fact]
    public static void TestMinValue()
    {
        VerifyDateTimeOffset(DateTimeOffset.MinValue, 1, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public static void TestCtor_Empty()
    {
        VerifyDateTimeOffset(new DateTimeOffset(), 1, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        VerifyDateTimeOffset(default(DateTimeOffset), 1, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public static void TestCtor_DateTime()
    {
        var dateTimeOffset = new DateTimeOffset(new DateTime(2012, 6, 11, 0, 0, 0, 0, DateTimeKind.Utc));
        VerifyDateTimeOffset(dateTimeOffset, 2012, 6, 11, 0, 0, 0, 0, TimeSpan.Zero);

        dateTimeOffset = new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 4, DateTimeKind.Local));
        VerifyDateTimeOffset(dateTimeOffset, 1986, 8, 15, 10, 20, 5, 4, null);

        DateTimeOffset today = new DateTimeOffset(DateTime.Today);
        DateTimeOffset now = DateTimeOffset.Now;
        VerifyDateTimeOffset(today, now.Year, now.Month, now.Day, 0, 0, 0, 0, now.Offset);

        today = new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc));
        Assert.Equal(TimeSpan.Zero, today.Offset);
        Assert.False(today.UtcDateTime.IsDaylightSavingTime());
    }

    [Fact]
    public static void TestCtor_DateTime_Invalid()
    {
        // DateTime < DateTimeOffset.MinValue
        DateTimeOffset min = DateTimeOffset.MinValue;
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day - 1, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour - 1, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute - 1, min.Second, min.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second - 1, min.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond - 1, DateTimeKind.Utc)));

        // DateTime > DateTimeOffset.MaxValue
        DateTimeOffset max = DateTimeOffset.MaxValue;
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month + 1, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, max.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, max.Millisecond, DateTimeKind.Utc)));
        Assert.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond + 1, DateTimeKind.Utc)));
    }

    [Fact]
    public static void TestCtor_DateTime_TimeSpan()
    {
        var dateTimeOffset = new DateTimeOffset(DateTime.MinValue, TimeSpan.FromHours(-14));
        VerifyDateTimeOffset(dateTimeOffset, 1, 1, 1, 0, 0, 0, 0, TimeSpan.FromHours(-14));

        dateTimeOffset = new DateTimeOffset(DateTime.MaxValue, TimeSpan.FromHours(14));
        VerifyDateTimeOffset(dateTimeOffset, 9999, 12, 31, 23, 59, 59, 999, TimeSpan.FromHours(14));

        dateTimeOffset = new DateTimeOffset(new DateTime(2012, 12, 31, 13, 50, 10), TimeSpan.Zero);
        VerifyDateTimeOffset(dateTimeOffset, 2012, 12, 31, 13, 50, 10, 0, TimeSpan.Zero);
    }

    [Fact]
    public static void TestCtor_DateTime_TimeSpan_Invalid()
    {
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.Now, TimeSpan.FromHours(15))); // Local time and non timezone timespan
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.Now, TimeSpan.FromHours(-15))); // Local time and non timezone timespan

        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, TimeSpan.FromHours(1))); // Local time and non zero timespan

        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, 3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, -3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, 0, 0, -3))); // TimeSpan is not whole minutes

        // DateTime < DateTimeOffset.MinValue
        DateTimeOffset min = DateTimeOffset.MinValue;
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day - 1, min.Hour, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour - 1, min.Minute, min.Second, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute - 1, min.Second, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second - 1, min.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond - 1, DateTimeKind.Utc), TimeSpan.Zero));

        // DateTime > DateTimeOffset.MaxValue
        DateTimeOffset max = DateTimeOffset.MaxValue;
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month + 1, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, max.Millisecond, DateTimeKind.Utc), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond + 1, DateTimeKind.Utc), TimeSpan.Zero));
    }

    [Fact]
    public static void TestCtor_Long_TimeSpan()
    {
        var expected = new DateTime(1, 2, 3, 4, 5, 6, 7);
        var dateTimeOffset = new DateTimeOffset(expected.Ticks, TimeSpan.Zero);
        VerifyDateTimeOffset(dateTimeOffset, dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, dateTimeOffset.Hour, dateTimeOffset.Minute, dateTimeOffset.Second, dateTimeOffset.Millisecond, TimeSpan.Zero);
    }

    [Fact]
    public static void TestCtor_Long_TimeSpan_Invalid()
    {
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, -3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(0, new TimeSpan(0, 0, 0, 0, -3))); // TimeSpan is not whole minutes

        Assert.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(0, TimeSpan.FromHours(-15))); // TimeZone.Offset > 14
        Assert.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(0, TimeSpan.FromHours(15))); // TimeZone.Offset < -14

        Assert.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTimeOffset(DateTimeOffset.MinValue.Ticks - 1, TimeSpan.Zero)); // Ticks < DateTimeOffset.MinValue.Ticks
        Assert.Throws<ArgumentOutOfRangeException>("ticks", () => new DateTimeOffset(DateTimeOffset.MaxValue.Ticks + 1, TimeSpan.Zero)); // Ticks > DateTimeOffset.MaxValue.Ticks
    }

    [Fact]
    public static void TestCtor_Int_Int_Int_Int_Int_Int_Int_TimeSpan()
    {
        var dateTimeOffset = new DateTimeOffset(1973, 10, 6, 14, 30, 0, 500, TimeSpan.Zero);
        VerifyDateTimeOffset(dateTimeOffset, 1973, 10, 6, 14, 30, 0, 500, TimeSpan.Zero);
    }

    [Fact]
    public static void TestCtor_Int_Int_Int_Int_Int_Int_Int_TimeSpan_Invalid()
    {
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, -3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, -3))); // TimeSpan is not whole minutes

        Assert.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, TimeSpan.FromHours(-15))); // TimeZone.Offset > 14
        Assert.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, TimeSpan.FromHours(15))); // TimeZone.Offset < -14

        // Invalid DateTime
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(0, 1, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Year < 1
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(10000, 1, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Year > 9999

        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 0, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Month < 1
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 13, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Motnh > 23

        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 0, 1, 1, 1, 1, TimeSpan.Zero)); // Day < 1
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 32, 1, 1, 1, 1, TimeSpan.Zero)); // Day > days in month

        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, -1, 1, 1, 1, TimeSpan.Zero)); // Hour < 0
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 24, 1, 1, 1, TimeSpan.Zero)); // Hour > 23

        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, -1, 1, 1, TimeSpan.Zero)); // Minute < 0
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 60, 1, 1, TimeSpan.Zero)); // Minute > 59

        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 1, -1, 1, TimeSpan.Zero)); // Second < 0
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 1, 60, 1, TimeSpan.Zero)); // Second > 59

        Assert.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, -1, TimeSpan.Zero)); // Millisecond < 0
        Assert.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, 1000, TimeSpan.Zero)); // Millisecond > 999

        // DateTime < DateTimeOffset.MinValue
        DateTimeOffset min = DateTimeOffset.MinValue;
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year - 1, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month - 1, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day - 1, min.Hour, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour - 1, min.Minute, min.Second, min.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute - 1, min.Second, min.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second - 1, min.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second, min.Millisecond - 1, TimeSpan.Zero));

        // DateTime > DateTimeOffset.MaxValue
        DateTimeOffset max = DateTimeOffset.MaxValue;
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month + 1, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, max.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, max.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, max.Millisecond, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>("millisecond", () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second, max.Millisecond + 1, TimeSpan.Zero));
    }

    [Fact]
    public static void TestCtor_Int_Int_Int_Int_Int_Int_TimeSpan()
    {
        var dateTimeOffset = new DateTimeOffset(1973, 10, 6, 14, 30, 0, TimeSpan.Zero);
        VerifyDateTimeOffset(dateTimeOffset, 1973, 10, 6, 14, 30, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public static void TestCtor_Int_Int_Int_Int_Int_Int_TimeSpan_Invalid()
    {
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, -3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, 3))); // TimeSpan is not whole minutes
        Assert.Throws<ArgumentException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0, 0, -3))); // TimeSpan is not whole minutes

        Assert.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, TimeSpan.FromHours(-15))); // TimeZone.Offset > 14
        Assert.Throws<ArgumentOutOfRangeException>("offset", () => new DateTimeOffset(1, 1, 1, 1, 1, 1, TimeSpan.FromHours(15))); // TimeZone.Offset < -14

        // Invalid DateTime
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(0, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Year < 1
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(10000, 1, 1, 1, 1, 1, TimeSpan.Zero)); // Year > 9999

        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 0, 1, 1, 1, 1, TimeSpan.Zero)); // Month < 1
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 13, 1, 1, 1, 1, TimeSpan.Zero)); // Month > 23

        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 0, 1, 1, 1, TimeSpan.Zero)); // Day < 1
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 32, 1, 1, 1, TimeSpan.Zero)); // Day > days in month

        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, -1, 1, 1, TimeSpan.Zero)); // Hour < 0
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 24, 1, 1, TimeSpan.Zero)); // Hour > 23

        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, -1, 1, TimeSpan.Zero)); // Minute < 0
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 60, 1, TimeSpan.Zero)); // Minute > 59

        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 1, -1, TimeSpan.Zero)); // Second < 0
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(1, 1, 1, 1, 1, 60, TimeSpan.Zero)); // Second > 59

        // DateTime < DateTimeOffset.MinValue
        DateTimeOffset min = DateTimeOffset.MinValue;
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year - 1, min.Month, min.Day, min.Hour, min.Minute, min.Second, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month - 1, min.Day, min.Hour, min.Minute, min.Second, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day - 1, min.Hour, min.Minute, min.Second, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour - 1, min.Minute, min.Second, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute - 1, min.Second, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(min.Year, min.Month, min.Day, min.Hour, min.Minute, min.Second - 1, TimeSpan.Zero));

        // DateTime > DateTimeOffset.MaxValue
        DateTimeOffset max = DateTimeOffset.MaxValue;
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year + 1, max.Month, max.Day, max.Hour, max.Minute, max.Second, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month + 1, max.Day + 1, max.Hour, max.Minute, max.Second, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day + 1, max.Hour, max.Minute, max.Second, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour + 1, max.Minute, max.Second, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute + 1, max.Second, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(null, () => new DateTimeOffset(max.Year, max.Month, max.Day, max.Hour, max.Minute, max.Second + 1, TimeSpan.Zero));
    }

    [Fact]
    public static void TestAddSubtract_TimeSpan()
    {
        var dateTimeOffset = new DateTimeOffset(new DateTime(2012, 6, 18, 10, 5, 1, 0));
        TimeSpan timeSpan = dateTimeOffset.TimeOfDay;

        DateTimeOffset newDate = dateTimeOffset.Subtract(timeSpan);
        Assert.Equal(new DateTimeOffset(new DateTime(2012, 6, 18, 0, 0, 0, 0)).Ticks, newDate.Ticks);
        Assert.Equal(dateTimeOffset.Ticks, newDate.Add(timeSpan).Ticks);
    }

    public static IEnumerable<object[]> Subtract_TimeSpan_TestData()
    {
        var dateTimeOffset = new DateTimeOffset(new DateTime(2012, 6, 18, 10, 5, 1, 0, DateTimeKind.Utc));

        yield return new object[] { dateTimeOffset, new TimeSpan(10, 5, 1), new DateTimeOffset(new DateTime(2012, 6, 18, 0, 0, 0, 0, DateTimeKind.Utc)) };
        yield return new object[] { dateTimeOffset, new TimeSpan(-10, -5, -1), new DateTimeOffset(new DateTime(2012, 6, 18, 20, 10, 2, 0, DateTimeKind.Utc)) };
    }

    [Theory]
    [MemberData(nameof(Subtract_TimeSpan_TestData))]
    public static void TestSubtract_TimeSpan(DateTimeOffset dt, TimeSpan ts, DateTimeOffset expected)
    {
        Assert.Equal(expected, dt - ts);
        Assert.Equal(expected, dt.Subtract(ts));
    }

    public static IEnumerable<object[]> Subtract_DateTimeOffset_TestData()
    {
        var dateTimeOffset1 = new DateTimeOffset(new DateTime(1996, 6, 3, 22, 15, 0, DateTimeKind.Utc));
        var dateTimeOffset2 = new DateTimeOffset(new DateTime(1996, 12, 6, 13, 2, 0, DateTimeKind.Utc));
        var dateTimeOffset3 = new DateTimeOffset(new DateTime(1996, 10, 12, 8, 42, 0, DateTimeKind.Utc));

        yield return new object[] { dateTimeOffset2, dateTimeOffset1, new TimeSpan(185, 14, 47, 0) };
        yield return new object[] { dateTimeOffset1, dateTimeOffset2, new TimeSpan(-185, -14, -47, 0) };
        yield return new object[] { dateTimeOffset1, dateTimeOffset2, new TimeSpan(-185, -14, -47, 0) };
    }

    [Theory]
    [MemberData(nameof(Subtract_DateTimeOffset_TestData))]
    public static void TestSubtract_DateTimeOffset(DateTimeOffset dt1, DateTimeOffset dt2, TimeSpan expected)
    {
        Assert.Equal(expected, dt1 - dt2);
        Assert.Equal(expected, dt1.Subtract(dt2));
    }

    [Fact]
    public static void TestAddition()
    {
        var dateTimeOffset = new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70));
        Assert.Equal(17, dateTimeOffset.AddDays(2).Day);
        Assert.Equal(13, dateTimeOffset.AddDays(-2).Day);

        Assert.Equal(10, dateTimeOffset.AddMonths(2).Month);
        Assert.Equal(6, dateTimeOffset.AddMonths(-2).Month);

        Assert.Equal(1996, dateTimeOffset.AddYears(10).Year);
        Assert.Equal(1976, dateTimeOffset.AddYears(-10).Year);

        Assert.Equal(13, dateTimeOffset.AddHours(3).Hour);
        Assert.Equal(7, dateTimeOffset.AddHours(-3).Hour);

        Assert.Equal(25, dateTimeOffset.AddMinutes(5).Minute);
        Assert.Equal(15, dateTimeOffset.AddMinutes(-5).Minute);

        Assert.Equal(35, dateTimeOffset.AddSeconds(30).Second);
        Assert.Equal(2, dateTimeOffset.AddSeconds(-3).Second);

        Assert.Equal(80, dateTimeOffset.AddMilliseconds(10).Millisecond);
        Assert.Equal(60, dateTimeOffset.AddMilliseconds(-10).Millisecond);
    }

    [Fact]
    public static void TestDayOfWeek()
    {
        var dateTimeOffset = new DateTimeOffset(new DateTime(2012, 6, 18));
        Assert.Equal(DayOfWeek.Monday, dateTimeOffset.DayOfWeek);
    }

    [Fact]
    public static void TestToFromFileTime()
    {
        var today = new DateTimeOffset(DateTime.Today);

        long dateTimeRaw = today.ToFileTime();
        Assert.Equal(today, DateTimeOffset.FromFileTime(dateTimeRaw));
    }

    [Fact]
    public static void TestUtcDateTime()
    {
        DateTime now = DateTime.Now;
        var dateTimeOffset = new DateTimeOffset(now);
        Assert.Equal(DateTime.Today, dateTimeOffset.Date);
        Assert.Equal(now, dateTimeOffset.DateTime);
        Assert.Equal(now.ToUniversalTime(), dateTimeOffset.UtcDateTime);
    }

    private static IEnumerable<object[]> UnixTime_TestData()
    {
        yield return new object[] { TestTime.FromMilliseconds(DateTimeOffset.MinValue, -62135596800000) };
        yield return new object[] { TestTime.FromMilliseconds(DateTimeOffset.MaxValue, 253402300799999) };
        yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero), 0) };
        yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(2014, 6, 13, 17, 21, 50, TimeSpan.Zero), 1402680110000) };
        yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(2830, 12, 15, 1, 23, 45, TimeSpan.Zero), 27169089825000) };
        yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(2830, 12, 15, 1, 23, 45, 399, TimeSpan.Zero), 27169089825399) };
        yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(9999, 12, 30, 23, 24, 25, TimeSpan.Zero), 253402212265000) };
        yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(1907, 7, 7, 7, 7, 7, TimeSpan.Zero), -1971967973000) };
        yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(1907, 7, 7, 7, 7, 7, 1, TimeSpan.Zero), -1971967972999) };
        yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(1907, 7, 7, 7, 7, 7, 777, TimeSpan.Zero), -1971967972223) };
        yield return new object[] { TestTime.FromMilliseconds(new DateTimeOffset(601636288270011234, TimeSpan.Zero), -1971967972999) };
    }

    [Theory]
    [MemberData(nameof(UnixTime_TestData))]
    public static void TestToUnixTimeMilliseconds(TestTime test)
    {
        long expectedMilliseconds = test.UnixTimeMilliseconds;
        long actualMilliseconds = test.DateTimeOffset.ToUnixTimeMilliseconds();
        Assert.Equal(expectedMilliseconds, actualMilliseconds);
    }

    [Theory]
    [MemberData(nameof(UnixTime_TestData))]
    public static void TesToUnixTimeMilliseconds_RountTrip(TestTime test)
    {
        long unixTimeMilliseconds = test.DateTimeOffset.ToUnixTimeMilliseconds();
        TestFromUnixTimeMilliseconds(TestTime.FromMilliseconds(test.DateTimeOffset, unixTimeMilliseconds));
    }

    [Theory]
    [MemberData(nameof(UnixTime_TestData))]
    public static void TestToUnixTimeSeconds(TestTime test)
    {
        long expectedSeconds = test.UnixTimeSeconds;
        long actualSeconds = test.DateTimeOffset.ToUnixTimeSeconds();
        Assert.Equal(expectedSeconds, actualSeconds);
    }

    [Theory]
    [MemberData(nameof(UnixTime_TestData))]
    public static void TesToUnixTimeSeconds_RoundTrip(TestTime test)
    {
        long unixTimeSeconds = test.DateTimeOffset.ToUnixTimeSeconds();
        TestFromUnixTimeSeconds(TestTime.FromSeconds(test.DateTimeOffset, unixTimeSeconds));
    }

    [Theory]
    [MemberData(nameof(UnixTime_TestData))]
    public static void TestFromUnixTimeMilliseconds(TestTime test)
    {
        // Only assert that expected == actual up to millisecond precision for conversion from milliseconds
        long expectedTicks = (test.DateTimeOffset.UtcTicks / TimeSpan.TicksPerMillisecond) * TimeSpan.TicksPerMillisecond;
        long actualTicks = DateTimeOffset.FromUnixTimeMilliseconds(test.UnixTimeMilliseconds).UtcTicks;
        Assert.Equal(expectedTicks, actualTicks);
    }

    [Fact]
    public static void TestFromUnixTimeMilliseconds_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("milliseconds", () => DateTimeOffset.FromUnixTimeMilliseconds(-62135596800001)); // Milliseconds < DateTimeOffset.MinValue
        Assert.Throws<ArgumentOutOfRangeException>("milliseconds", () => DateTimeOffset.FromUnixTimeMilliseconds(253402300800000)); // Milliseconds > DateTimeOffset.MaxValue

        Assert.Throws<ArgumentOutOfRangeException>("milliseconds", () => DateTimeOffset.FromUnixTimeMilliseconds(long.MinValue)); // Milliseconds < DateTimeOffset.MinValue
        Assert.Throws<ArgumentOutOfRangeException>("milliseconds", () => DateTimeOffset.FromUnixTimeMilliseconds(long.MaxValue)); // Milliseconds > DateTimeOffset.MaxValue
    }

    [Theory]
    [MemberData(nameof(UnixTime_TestData))]
    public static void TestFromUnixTimeSeconds(TestTime test)
    {
        // Only assert that expected == actual up to second precision for conversion from seconds
        long expectedTicks = (test.DateTimeOffset.UtcTicks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
        long actualTicks = DateTimeOffset.FromUnixTimeSeconds(test.UnixTimeSeconds).UtcTicks;
        Assert.Equal(expectedTicks, actualTicks);
    }

    [Fact]
    public static void TestFromUnixTimeSeconds_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>("seconds", () => DateTimeOffset.FromUnixTimeSeconds(-62135596801));// Seconds < DateTimeOffset.MinValue
        Assert.Throws<ArgumentOutOfRangeException>("seconds", () => DateTimeOffset.FromUnixTimeSeconds(253402300800)); // Seconds > DateTimeOffset.MaxValue

        Assert.Throws<ArgumentOutOfRangeException>("seconds", () => DateTimeOffset.FromUnixTimeSeconds(long.MinValue)); // Seconds < DateTimeOffset.MinValue
        Assert.Throws<ArgumentOutOfRangeException>("seconds", () => DateTimeOffset.FromUnixTimeSeconds(long.MaxValue)); // Seconds < DateTimeOffset.MinValue
    }

    [Theory]
    [MemberData(nameof(UnixTime_TestData))]
    public static void TestFromUnixTimeMilliseconds_RoundTrip(TestTime test)
    {
        DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeMilliseconds(test.UnixTimeMilliseconds);
        TestToUnixTimeMilliseconds(TestTime.FromMilliseconds(dateTime, test.UnixTimeMilliseconds));
    }

    [Theory]
    [MemberData(nameof(UnixTime_TestData))]
    public static void TestFromUnixTimeSeconds_RoundTrip(TestTime test)
    {
        DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeSeconds(test.UnixTimeSeconds);
        TestToUnixTimeSeconds(TestTime.FromSeconds(dateTime, test.UnixTimeSeconds));
    }

    [Fact]
    public static void TestParse_String()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString();

        DateTimeOffset result = DateTimeOffset.Parse(expectedString);
        Assert.Equal(expectedString, result.ToString());
    }

    [Fact]
    public static void TestParse_String_FormatProvider()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString();

        DateTimeOffset result = DateTimeOffset.Parse(expectedString, null);
        Assert.Equal(expectedString, result.ToString());
    }

    [Fact]
    public static void TestParse_String_FormatProvider_DateTimeStyles()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString();

        DateTimeOffset result = DateTimeOffset.Parse(expectedString, null, DateTimeStyles.None);
        Assert.Equal(expectedString, result.ToString());
    }

    [Fact]
    public static void TestParse_Japanese()
    {
        var expected = new DateTimeOffset(new DateTime(2012, 12, 21, 10, 8, 6));
        var cultureInfo = new CultureInfo("ja-JP");

        string expectedString = string.Format(cultureInfo, "{0}", expected);
        Assert.Equal(expected, DateTimeOffset.Parse(expectedString, cultureInfo));
    }

    [Fact]
    public static void TestTryParse_String()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString("u");

        DateTimeOffset result;
        Assert.True(DateTimeOffset.TryParse(expectedString, out result));
        Assert.Equal(expectedString, result.ToString("u"));
    }

    [Fact]
    public static void TestTryParse_String_FormatProvider_DateTimeStyles_U()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString("u");

        DateTimeOffset result;
        Assert.True(DateTimeOffset.TryParse(expectedString, null, DateTimeStyles.None, out result));
        Assert.Equal(expectedString, result.ToString("u"));
    }

    [Fact]
    public static void TestTryParse_String_FormatProvider_DateTimeStyles_G()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString("g");

        DateTimeOffset result;
        Assert.True(DateTimeOffset.TryParse(expectedString, null, DateTimeStyles.AssumeUniversal, out result));
        Assert.Equal(expectedString, result.ToString("g"));
    }

    [Fact]
    public static void TestTryParse_TimeDesignators()
    {
        DateTimeOffset result;
        Assert.True(DateTimeOffset.TryParse("4/21 5am", new CultureInfo("en-US"), DateTimeStyles.None, out result));
        Assert.Equal(4, result.Month);
        Assert.Equal(21, result.Day);
        Assert.Equal(5, result.Hour);

        Assert.True(DateTimeOffset.TryParse("4/21 5pm", new CultureInfo("en-US"), DateTimeStyles.None, out result));
        Assert.Equal(4, result.Month);
        Assert.Equal(21, result.Day);
        Assert.Equal(17, result.Hour);
    }

    [Fact]
    public static void TestParseExact_String_String_FormatProvider()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString("u");

        DateTimeOffset result = DateTimeOffset.ParseExact(expectedString, "u", null);
        Assert.Equal(expectedString, result.ToString("u"));
    }

    [Fact]
    public static void TestParseExact_String_String_FormatProvider_DateTimeStyles_U()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString("u");

        DateTimeOffset result = DateTimeOffset.ParseExact(expectedString, "u", null, DateTimeStyles.None);
        Assert.Equal(expectedString, result.ToString("u"));
    }

    [Fact]
    public static void TestParseExact_String_String_FormatProvider_DateTimeStyles_G()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString("g");

        DateTimeOffset result = DateTimeOffset.ParseExact(expectedString, "g", null, DateTimeStyles.AssumeUniversal);
        Assert.Equal(expectedString, result.ToString("g"));
    }

    [Fact]
    public static void TestParseExact_String_String_FormatProvider_DateTimeStyles_O()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString("o");

        DateTimeOffset result = DateTimeOffset.ParseExact(expectedString, "o", null, DateTimeStyles.None);
        Assert.Equal(expectedString, result.ToString("o"));
    }

    [Fact]
    public static void TestParseExact_String_String_FormatProvider_DateTimeStyles_CustomFormatProvider()
    {
        var formatter = new MyFormatter();
        string dateBefore = DateTime.Now.ToString();

        DateTimeOffset dateAfter = DateTimeOffset.ParseExact(dateBefore, "G", formatter, DateTimeStyles.AssumeUniversal);
        Assert.Equal(dateBefore, dateAfter.DateTime.ToString());
    }

    [Fact]
    public static void TestParseExact_String_StringArray_FormatProvider_DateTimeStyles()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString("g");

        var formats = new string[] { "g" };
        DateTimeOffset result = DateTimeOffset.ParseExact(expectedString, formats, null, DateTimeStyles.AssumeUniversal);
        Assert.Equal(expectedString, result.ToString("g"));
    }

    [Fact]
    public static void TestTryParseExact_String_String_FormatProvider_DateTimeStyles_NullFormatProvider()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString("g");

        DateTimeOffset resulted;
        Assert.True(DateTimeOffset.TryParseExact(expectedString, "g", null, DateTimeStyles.AssumeUniversal, out resulted));
        Assert.Equal(expectedString, resulted.ToString("g"));
    }

    [Fact]
    public static void TestTryParseExact_String_StringArray_FormatProvider_DateTimeStyles()
    {
        DateTimeOffset expected = DateTimeOffset.MaxValue;
        string expectedString = expected.ToString("g");

        var formats = new string[] { "g" };
        DateTimeOffset result;
        Assert.True(DateTimeOffset.TryParseExact(expectedString, formats, null, DateTimeStyles.AssumeUniversal, out result));
        Assert.Equal(expectedString, result.ToString("g"));
    }

    private static void VerifyDateTimeOffset(DateTimeOffset dateTimeOffset, int year, int month, int day, int hour, int minute, int second, int millisecond, TimeSpan? offset)
    {
        Assert.Equal(year, dateTimeOffset.Year);
        Assert.Equal(month, dateTimeOffset.Month);
        Assert.Equal(day, dateTimeOffset.Day);
        Assert.Equal(hour, dateTimeOffset.Hour);
        Assert.Equal(minute, dateTimeOffset.Minute);
        Assert.Equal(second, dateTimeOffset.Second);
        Assert.Equal(millisecond, dateTimeOffset.Millisecond);

        if (offset.HasValue)
        {
            Assert.Equal(offset.Value, dateTimeOffset.Offset);
        }
    }

    private class MyFormatter : IFormatProvider
    {
        public object GetFormat(Type formatType)
        {
            return typeof(IFormatProvider) == formatType ? this : null;
        }
    }

    public class TestTime
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
}
