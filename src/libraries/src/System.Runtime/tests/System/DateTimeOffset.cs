// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public class DateTimeOffsetTests
{
    [Fact]
    public static void TestConstructors()
    {
        DateTimeOffset dt = new DateTimeOffset(new DateTime(2012, 6, 11, 0, 0, 0, DateTimeKind.Utc));
        ValidateYearMonthDay(dt, 2012, 6, 11);

        dt = new DateTimeOffset(new DateTime(2012, 12, 31, 13, 50, 10), TimeSpan.Zero);
        ValidateYearMonthDay(dt, 2012, 12, 31, 13, 50, 10);

        dt = new DateTimeOffset(1973, 10, 6, 14, 30, 0, 500, TimeSpan.Zero);
        ValidateYearMonthDay(dt, 1973, 10, 6, 14, 30, 0, 500);

        dt = new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, DateTimeKind.Local));
        ValidateYearMonthDay(dt, 1986, 8, 15, 10, 20, 5);

        dt = new DateTimeOffset(DateTime.MinValue, TimeSpan.FromHours(-14));
        ValidateYearMonthDay(dt, 1, 1, 1, 0, 0, 0);

        dt = new DateTimeOffset(DateTime.MaxValue, TimeSpan.FromHours(14));
        ValidateYearMonthDay(dt, 9999, 12, 31, 23, 59, 59);

        Assert.Throws<ArgumentException>(() => new DateTimeOffset(DateTime.Now, TimeSpan.FromHours(-15)));
        Assert.Throws<ArgumentException>(() => new DateTimeOffset(DateTime.Now, TimeSpan.FromHours(15)));
    }

    [Fact]
    public static void TestDateTimeLimits()
    {
        DateTimeOffset dt = DateTimeOffset.MaxValue;
        ValidateYearMonthDay(dt, 9999, 12, 31);

        dt = DateTimeOffset.MinValue;
        ValidateYearMonthDay(dt, 1, 1, 1);
    }

    [Fact]
    public static void TestAddition()
    {
        DateTimeOffset dt = new DateTimeOffset(new DateTime(1986, 8, 15, 10, 20, 5, 70));
        Assert.Equal(17, dt.AddDays(2).Day);
        Assert.Equal(13, dt.AddDays(-2).Day);

        Assert.Equal(10, dt.AddMonths(2).Month);
        Assert.Equal(6, dt.AddMonths(-2).Month);

        Assert.Equal(1996, dt.AddYears(10).Year);
        Assert.Equal(1976, dt.AddYears(-10).Year);

        Assert.Equal(13, dt.AddHours(3).Hour);
        Assert.Equal(7, dt.AddHours(-3).Hour);

        Assert.Equal(25, dt.AddMinutes(5).Minute);
        Assert.Equal(15, dt.AddMinutes(-5).Minute);

        Assert.Equal(35, dt.AddSeconds(30).Second);
        Assert.Equal(2, dt.AddSeconds(-3).Second);

        Assert.Equal(80, dt.AddMilliseconds(10).Millisecond);
        Assert.Equal(60, dt.AddMilliseconds(-10).Millisecond);
    }

    [Fact]
    public static void TestDayOfWeek()
    {
        DateTimeOffset dt = new DateTimeOffset(new DateTime(2012, 6, 18));
        Assert.Equal(DayOfWeek.Monday, dt.DayOfWeek);
    }

    [Fact]
    public static void TestTimeSpan()
    {
        DateTimeOffset dt = new DateTimeOffset(new DateTime(2012, 6, 18, 10, 5, 1, 0));
        TimeSpan ts = dt.TimeOfDay;
        DateTimeOffset newDate = dt.Subtract(ts);
        Assert.Equal(new DateTimeOffset(new DateTime(2012, 6, 18, 0, 0, 0, 0)).Ticks, newDate.Ticks);
        Assert.Equal(dt.Ticks, newDate.Add(ts).Ticks);
    }

    [Fact]
    public static void TestToday()
    {
        DateTimeOffset today = new DateTimeOffset(DateTime.Today);
        DateTimeOffset now = DateTimeOffset.Now;
        ValidateYearMonthDay(today, now.Year, now.Month, now.Day);

        today = new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc));
        Assert.Equal(TimeSpan.Zero, today.Offset);
        Assert.Equal(false, today.UtcDateTime.IsDaylightSavingTime());
    }

    [Fact]
    public static void TestConversion()
    {
        DateTimeOffset today = new DateTimeOffset(DateTime.Today);

        long dateTimeRaw = today.ToFileTime();
        Assert.Equal(today, DateTimeOffset.FromFileTime(dateTimeRaw));
    }

    [Fact]
    public static void TestRoundTripDateTime()
    {
        DateTime now = DateTime.Now;
        DateTimeOffset dto = new DateTimeOffset(now);
        Assert.Equal(DateTime.Today, dto.Date);
        Assert.Equal(now, dto.DateTime);
        Assert.Equal(now.ToUniversalTime(), dto.UtcDateTime);
    }

    [Fact]
    public static void TestOperators()
    {
        DateTimeOffset date1 = new DateTimeOffset(new DateTime(1996, 6, 3, 22, 15, 0, DateTimeKind.Utc));
        DateTimeOffset date2 = new DateTimeOffset(new DateTime(1996, 12, 6, 13, 2, 0, DateTimeKind.Utc));
        DateTimeOffset date3 = new DateTimeOffset(new DateTime(1996, 10, 12, 8, 42, 0, DateTimeKind.Utc));

        // diff1 gets 185 days, 14 hours, and 47 minutes.
        TimeSpan diff1 = date2.Subtract(date1);
        Assert.Equal(new TimeSpan(185, 14, 47, 0), diff1);

        // date4 gets 4/9/1996 5:55:00 PM.
        DateTimeOffset date4 = date3.Subtract(diff1);
        Assert.Equal(new DateTimeOffset(new DateTime(1996, 4, 9, 17, 55, 0, DateTimeKind.Utc)), date4);

        // diff2 gets 55 days 4 hours and 20 minutes.
        TimeSpan diff2 = date2 - date3;
        Assert.Equal(new TimeSpan(55, 4, 20, 0), diff2);

        // date5 gets 4/9/1996 5:55:00 PM.
        DateTimeOffset date5 = date1 - diff2;
        Assert.Equal(new DateTimeOffset(new DateTime(1996, 4, 9, 17, 55, 0, DateTimeKind.Utc)), date5);
    }

    [Fact]
    public static void TestParsingDateTimeWithTimeDesignator()
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

    public class MyFormatter : IFormatProvider
    {
        public object GetFormat(Type formatType)
        {
            if (typeof(IFormatProvider) == formatType)
            {
                return this;
            }
            else
            {
                return null;
            }
        }
    }

    [Fact]
    public static void TestParseWithAssumeUniversal()
    {
        var formatter = new MyFormatter();
        var dateBefore = DateTime.Now.ToString();
        var dateAfter = DateTimeOffset.ParseExact(dateBefore, "G", formatter, DateTimeStyles.AssumeUniversal);
        Assert.Equal(dateBefore, dateAfter.DateTime.ToString());
    }

    [Fact]
    public static void TestFormatParse()
    {
        DateTimeOffset dt = new DateTimeOffset(new DateTime(2012, 12, 21, 10, 8, 6));
        CultureInfo ci = new CultureInfo("ja-JP");
        string s = string.Format(ci, "{0}", dt);
        Assert.Equal(dt, DateTimeOffset.Parse(s, ci));
    }

    [Fact]
    public static void TestParse1()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString();
        DateTimeOffset in_1 = DateTimeOffset.Parse(s);
        String actual = in_1.ToString();
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParse2()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString();
        DateTimeOffset in_1 = DateTimeOffset.Parse(s, null);
        String actual = in_1.ToString();
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParse3()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString();
        DateTimeOffset in_1 = DateTimeOffset.Parse(s, null, DateTimeStyles.None);
        String actual = in_1.ToString();
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParseExact2()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString("u");
        DateTimeOffset in_1 = DateTimeOffset.ParseExact(s, "u", null, DateTimeStyles.None);
        String actual = in_1.ToString("u");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParseExact3()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString("g");
        DateTimeOffset in_1 = DateTimeOffset.ParseExact(s, "g", null, DateTimeStyles.AssumeUniversal);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParseExact4()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString("o");
        DateTimeOffset in_1 = DateTimeOffset.ParseExact(s, "o", null, DateTimeStyles.None);
        String actual = in_1.ToString("o");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParseExact4a()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString("g");
        String[] formats = { "g" };
        DateTimeOffset in_1 = DateTimeOffset.ParseExact(s, formats, null, DateTimeStyles.AssumeUniversal);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestTryParse1()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString("u");
        DateTimeOffset in_1;
        bool b = DateTimeOffset.TryParse(s, out in_1);
        Assert.True(b);
        String actual = in_1.ToString("u");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestTryParse2()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString("u");
        DateTimeOffset in_1;
        bool b = DateTimeOffset.TryParse(s, out in_1);
        Assert.True(b);
        String actual = in_1.ToString("u");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestTryParse4()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString("u");
        DateTimeOffset in_1;
        bool b = DateTimeOffset.TryParse(s, null, DateTimeStyles.None, out in_1);
        Assert.True(b);
        String actual = in_1.ToString("u");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestTryParse4a()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString("g");
        DateTimeOffset in_1;
        bool b = DateTimeOffset.TryParse(s, null, DateTimeStyles.AssumeUniversal, out in_1);
        Assert.True(b);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestTryParseExact()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString("g");
        DateTimeOffset in_1;
        bool b = DateTimeOffset.TryParseExact(s, "g", null, DateTimeStyles.AssumeUniversal, out in_1);
        Assert.True(b);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestTryParseExactA()
    {
        DateTimeOffset src = DateTimeOffset.MaxValue;
        String s = src.ToString("g");
        String[] formats = { "g" };
        DateTimeOffset in_1;
        bool b = DateTimeOffset.TryParseExact(s, formats, null, DateTimeStyles.AssumeUniversal, out in_1);
        Assert.True(b);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    internal static void ValidateYearMonthDay(DateTimeOffset dt, int year, int month, int day)
    {
        Assert.Equal(year, dt.Year);
        Assert.Equal(month, dt.Month);
        Assert.Equal(day, dt.Day);
    }

    internal static void ValidateYearMonthDay(DateTimeOffset dt, int year, int month, int day, int hour, int minute, int second)
    {
        ValidateYearMonthDay(dt, year, month, day);
        Assert.Equal(hour, dt.Hour);
        Assert.Equal(minute, dt.Minute);
        Assert.Equal(second, dt.Second);
    }

    internal static void ValidateYearMonthDay(DateTimeOffset dt, int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        ValidateYearMonthDay(dt, year, month, day, hour, minute, second);
        Assert.Equal(dt.Millisecond, millisecond);
    }
}