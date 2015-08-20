// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public static unsafe class DateTimeTests
{
    [Fact]
    public static void TestConstructors()
    {
        DateTime dt = new DateTime(2012, 6, 11);
        ValidateYearMonthDay(dt, 2012, 6, 11);

        dt = new DateTime(2012, 12, 31, 13, 50, 10);
        ValidateYearMonthDay(dt, 2012, 12, 31, 13, 50, 10);

        dt = new DateTime(1973, 10, 6, 14, 30, 0, 500);
        ValidateYearMonthDay(dt, 1973, 10, 6, 14, 30, 0, 500);

        dt = new DateTime(1986, 8, 15, 10, 20, 5, DateTimeKind.Local);
        ValidateYearMonthDay(dt, 1986, 8, 15, 10, 20, 5);
    }

    [Fact]
    public static void TestDateTimeLimits()
    {
        DateTime dt = DateTime.MaxValue;
        ValidateYearMonthDay(dt, 9999, 12, 31);

        dt = DateTime.MinValue;
        ValidateYearMonthDay(dt, 1, 1, 1);
    }

    [Fact]
    public static void TestLeapYears()
    {
        Assert.Equal(true, DateTime.IsLeapYear(2004));
        Assert.Equal(false, DateTime.IsLeapYear(2005));
    }

    [Fact]
    public static void TestAddition()
    {
        DateTime dt = new DateTime(1986, 8, 15, 10, 20, 5, 70);
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
        DateTime dt = new DateTime(2012, 6, 18);
        Assert.Equal(DayOfWeek.Monday, dt.DayOfWeek);
    }

    [Fact]
    public static void TestTimeSpan()
    {
        DateTime dt = new DateTime(2012, 6, 18, 10, 5, 1, 0);
        TimeSpan ts = dt.TimeOfDay;
        DateTime newDate = dt.Subtract(ts);
        Assert.Equal(new DateTime(2012, 6, 18, 0, 0, 0, 0).Ticks, newDate.Ticks);
        Assert.Equal(dt.Ticks, newDate.Add(ts).Ticks);
    }

    [Fact]
    public static void TestToday()
    {
        DateTime today = DateTime.Today;
        DateTime now = DateTime.Now;
        ValidateYearMonthDay(today, now.Year, now.Month, now.Day);

        today = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
        Assert.Equal(DateTimeKind.Utc, today.Kind);
        Assert.Equal(false, today.IsDaylightSavingTime());
    }

    [Fact]
    public static void TestCoversion()
    {
        DateTime today = DateTime.Today;
        long dateTimeRaw = today.ToBinary();
        Assert.Equal(today, DateTime.FromBinary(dateTimeRaw));

        dateTimeRaw = today.ToFileTime();
        Assert.Equal(today, DateTime.FromFileTime(dateTimeRaw));

        dateTimeRaw = today.ToFileTimeUtc();
        Assert.Equal(today, DateTime.FromFileTimeUtc(dateTimeRaw).ToLocalTime());
    }

    [Fact]
    public static void TestOperators()
    {
        System.DateTime date1 = new System.DateTime(1996, 6, 3, 22, 15, 0);
        System.DateTime date2 = new System.DateTime(1996, 12, 6, 13, 2, 0);
        System.DateTime date3 = new System.DateTime(1996, 10, 12, 8, 42, 0);

        // diff1 gets 185 days, 14 hours, and 47 minutes.
        System.TimeSpan diff1 = date2.Subtract(date1);
        Assert.Equal(new TimeSpan(185, 14, 47, 0), diff1);

        // date4 gets 4/9/1996 5:55:00 PM.
        System.DateTime date4 = date3.Subtract(diff1);
        Assert.Equal(new DateTime(1996, 4, 9, 17, 55, 0), date4);

        // diff2 gets 55 days 4 hours and 20 minutes.
        System.TimeSpan diff2 = date2 - date3;
        Assert.Equal(new TimeSpan(55, 4, 20, 0), diff2);

        // date5 gets 4/9/1996 5:55:00 PM.
        System.DateTime date5 = date1 - diff2;
        Assert.Equal(new DateTime(1996, 4, 9, 17, 55, 0), date5);
    }

    [Fact]
    public static void TestParsingDateTimeWithTimeDesignator()
    {
        DateTime result;
        Assert.True(DateTime.TryParse("4/21 5am", new CultureInfo("en-US"), DateTimeStyles.None, out result));
        Assert.Equal(4, result.Month);
        Assert.Equal(21, result.Day);
        Assert.Equal(5, result.Hour);

        Assert.True(DateTime.TryParse("4/21 5pm", new CultureInfo("en-US"), DateTimeStyles.None, out result));
        Assert.Equal(4, result.Month);
        Assert.Equal(21, result.Day);
        Assert.Equal(17, result.Hour);
    }

    public class MyFormater : IFormatProvider
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
    public static void TestParseWithAdjustToUniversal()
    {
        var formater = new MyFormater();
        var dateBefore = DateTime.Now.ToString();
        var dateAfter = DateTime.ParseExact(dateBefore, "G", formater, DateTimeStyles.AdjustToUniversal);
        Assert.Equal(dateBefore, dateAfter.ToString());
    }

    [Fact]
    public static void TestFormatParse()
    {
        DateTime dt = new DateTime(2012, 12, 21, 10, 8, 6);
        CultureInfo ci = new CultureInfo("ja-JP");
        string s = string.Format(ci, "{0}", dt);
        Assert.Equal(dt, DateTime.Parse(s, ci));
    }

    [Fact]
    public static void TestParse1()
    {
        DateTime src = DateTime.MaxValue;
        String s = src.ToString();
        DateTime in_1 = DateTime.Parse(s);
        String actual = in_1.ToString();
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParse2()
    {
        DateTime src = DateTime.MaxValue;
        String s = src.ToString();
        DateTime in_1 = DateTime.Parse(s, null);
        String actual = in_1.ToString();
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParse3()
    {
        DateTime src = DateTime.MaxValue;
        String s = src.ToString();
        DateTime in_1 = DateTime.Parse(s, null, DateTimeStyles.None);
        String actual = in_1.ToString();
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParseExact3()
    {
        DateTime src = DateTime.MaxValue;
        String s = src.ToString("g");
        DateTime in_1 = DateTime.ParseExact(s, "g", null);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParseExact4()
    {
        DateTime src = DateTime.MaxValue;
        String s = src.ToString("g");
        DateTime in_1 = DateTime.ParseExact(s, "g", null, DateTimeStyles.None);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestParseExact4a()
    {
        DateTime src = DateTime.MaxValue;
        String s = src.ToString("g");
        String[] formats = { "g" };
        DateTime in_1 = DateTime.ParseExact(s, formats, null, DateTimeStyles.None);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestTryParse2()
    {
        DateTime src = DateTime.MaxValue;
        String s = src.ToString("g");
        DateTime in_1;
        bool b = DateTime.TryParse(s, out in_1);
        Assert.True(b);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestTryParse4()
    {
        DateTime src = DateTime.MaxValue;
        String s = src.ToString("g");
        DateTime in_1;
        bool b = DateTime.TryParse(s, null, DateTimeStyles.None, out in_1);
        Assert.True(b);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestTryParseExact()
    {
        DateTime src = DateTime.MaxValue;
        String s = src.ToString("g");
        DateTime in_1;
        bool b = DateTime.TryParseExact(s, "g", null, DateTimeStyles.None, out in_1);
        Assert.True(b);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestTryParseExactA()
    {
        DateTime src = DateTime.MaxValue;
        String s = src.ToString("g");
        String[] formats = { "g" };
        DateTime in_1;
        bool b = DateTime.TryParseExact(s, formats, null, DateTimeStyles.None, out in_1);
        Assert.True(b);
        String actual = in_1.ToString("g");
        Assert.Equal(s, actual);
    }

    [Fact]
    public static void TestGetDateTimeFormats()
    {        
        char[] allStandardFormats =
        {
            'd', 'D', 'f', 'F', 'g', 'G',
            'm', 'M', 'o', 'O', 'r', 'R',
            's', 't', 'T', 'u', 'U', 'y', 'Y',
        };

        DateTime july28 = new DateTime(2009, 7, 28, 5, 23, 15);
        List<string> july28Formats = new List<string>();

        foreach (char format in allStandardFormats)
        {
            string[] dates = july28.GetDateTimeFormats(format);

            Assert.True(dates.Length > 0);

            DateTime parsedDate;
            Assert.True(DateTime.TryParseExact(dates[0], format.ToString(), CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate));

            july28Formats.AddRange(dates);
        }

        List<string> actualJuly28Formats = july28.GetDateTimeFormats().ToList();
        Assert.Equal(july28Formats.OrderBy(t => t), actualJuly28Formats.OrderBy(t => t));

        actualJuly28Formats = july28.GetDateTimeFormats(CultureInfo.CurrentCulture).ToList();
        Assert.Equal(july28Formats.OrderBy(t => t), actualJuly28Formats.OrderBy(t => t));
    }

    [Fact]
    public static void TestGetDateTimeFormats_FormatSpecifier_InvalidFormat()
    {
        DateTime july28 = new DateTime(2009, 7, 28, 5, 23, 15);

        Assert.Throws<FormatException>(() => july28.GetDateTimeFormats('x'));
    }

    internal static void ValidateYearMonthDay(DateTime dt, int year, int month, int day)
    {
        Assert.Equal(dt.Year, year);
        Assert.Equal(dt.Month, month);
        Assert.Equal(dt.Day, day);
    }

    internal static void ValidateYearMonthDay(DateTime dt, int year, int month, int day, int hour, int minute, int second)
    {
        ValidateYearMonthDay(dt, year, month, day);
        Assert.Equal(dt.Hour, hour);
        Assert.Equal(dt.Minute, minute);
        Assert.Equal(dt.Second, second);
    }

    internal static void ValidateYearMonthDay(DateTime dt, int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        ValidateYearMonthDay(dt, year, month, day, hour, minute, second);
        Assert.Equal(dt.Millisecond, millisecond);
    }
}

