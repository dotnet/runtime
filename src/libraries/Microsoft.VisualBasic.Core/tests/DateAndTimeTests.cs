// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class DateAndTimeTests
    {
        [Theory]
        [MemberData(nameof(DateAdd_DateInterval_TestData))]
        public void DateAdd(DateInterval interval, double number, DateTime dateValue, DateTime expected)
        {
            Assert.Equal(expected, DateAndTime.DateAdd(interval, number, dateValue));
        }

        [Theory]
        [MemberData(nameof(DateAdd_DateInterval_ArgumentOutOfRangeException_TestData))]
        public void DateAdd_ArgumentOutOfRangeException(DateInterval interval, double number, DateTime dateValue)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DateAndTime.DateAdd(interval, number, dateValue));
        }

        private static IEnumerable<object[]> DateAdd_DateInterval_TestData()
        {
            var now = DateTime.UtcNow;
            var calendar = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar;

            yield return new object[] { DateInterval.Year, 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { DateInterval.Year, 0.0, now, now };
            yield return new object[] { DateInterval.Year, 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { DateInterval.Year, 2.0, DateTime.MinValue, calendar.AddYears(DateTime.MinValue, 2) };
            yield return new object[] { DateInterval.Year, 2.0, now, calendar.AddYears(now, 2) };
            yield return new object[] { DateInterval.Year, -2.0, now, calendar.AddYears(now, -2) };
            yield return new object[] { DateInterval.Year, -2.0, DateTime.MaxValue, calendar.AddYears(DateTime.MaxValue, -2) };

            yield return new object[] { DateInterval.Quarter, 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { DateInterval.Quarter, 0.0, now, now };
            yield return new object[] { DateInterval.Quarter, 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { DateInterval.Quarter, 2.0, DateTime.MinValue, calendar.AddMonths(DateTime.MinValue, 6) };
            yield return new object[] { DateInterval.Quarter, 2.0, now, calendar.AddMonths(now, 6) };
            yield return new object[] { DateInterval.Quarter, -2.0, now, calendar.AddMonths(now, -6) };
            yield return new object[] { DateInterval.Quarter, -2.0, DateTime.MaxValue, calendar.AddMonths(DateTime.MaxValue, -6) };

            yield return new object[] { DateInterval.Month, 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { DateInterval.Month, 0.0, now, now };
            yield return new object[] { DateInterval.Month, 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { DateInterval.Month, 2.0, DateTime.MinValue, calendar.AddMonths(DateTime.MinValue, 2) };
            yield return new object[] { DateInterval.Month, 2.0, now, calendar.AddMonths(now, 2) };
            yield return new object[] { DateInterval.Month, -2.0, now, calendar.AddMonths(now, -2) };
            yield return new object[] { DateInterval.Month, -2.0, DateTime.MaxValue, calendar.AddMonths(DateTime.MaxValue, -2) };

            yield return new object[] { DateInterval.DayOfYear, 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { DateInterval.DayOfYear, 0.0, now, now };
            yield return new object[] { DateInterval.DayOfYear, 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { DateInterval.DayOfYear, 2.0, DateTime.MinValue, DateTime.MinValue.AddDays(2) };
            yield return new object[] { DateInterval.DayOfYear, 2.0, now, now.AddDays(2) };
            yield return new object[] { DateInterval.DayOfYear, -2.0, now, now.AddDays(-2) };
            yield return new object[] { DateInterval.DayOfYear, -2.0, DateTime.MaxValue, DateTime.MaxValue.AddDays(-2) };

            yield return new object[] { DateInterval.Day, 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { DateInterval.Day, 0.0, now, now };
            yield return new object[] { DateInterval.Day, 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { DateInterval.Day, 2.0, DateTime.MinValue, DateTime.MinValue.AddDays(2) };
            yield return new object[] { DateInterval.Day, 2.0, now, now.AddDays(2) };
            yield return new object[] { DateInterval.Day, -2.0, now, now.AddDays(-2) };
            yield return new object[] { DateInterval.Day, -2.0, DateTime.MaxValue, DateTime.MaxValue.AddDays(-2) };

            yield return new object[] { DateInterval.WeekOfYear, 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { DateInterval.WeekOfYear, 0.0, now, now };
            yield return new object[] { DateInterval.WeekOfYear, 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { DateInterval.WeekOfYear, 2.0, DateTime.MinValue, DateTime.MinValue.AddDays(14) };
            yield return new object[] { DateInterval.WeekOfYear, 2.0, now, now.AddDays(14) };
            yield return new object[] { DateInterval.WeekOfYear, -2.0, now, now.AddDays(-14) };
            yield return new object[] { DateInterval.WeekOfYear, -2.0, DateTime.MaxValue, DateTime.MaxValue.AddDays(-14) };

            yield return new object[] { DateInterval.Weekday, 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { DateInterval.Weekday, 0.0, now, now };
            yield return new object[] { DateInterval.Weekday, 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { DateInterval.Weekday, 2.0, DateTime.MinValue, DateTime.MinValue.AddDays(2) };
            yield return new object[] { DateInterval.Weekday, 2.0, now, now.AddDays(2) };
            yield return new object[] { DateInterval.Weekday, -2.0, now, now.AddDays(-2) };
            yield return new object[] { DateInterval.Weekday, -2.0, DateTime.MaxValue, DateTime.MaxValue.AddDays(-2) };

            yield return new object[] { DateInterval.Hour, 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { DateInterval.Hour, 0.0, now, now };
            yield return new object[] { DateInterval.Hour, 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { DateInterval.Hour, 2.0, DateTime.MinValue, DateTime.MinValue.AddHours(2) };
            yield return new object[] { DateInterval.Hour, 2.0, now, now.AddHours(2) };
            yield return new object[] { DateInterval.Hour, -2.0, now, now.AddHours(-2) };
            yield return new object[] { DateInterval.Hour, -2.0, DateTime.MaxValue, DateTime.MaxValue.AddHours(-2) };

            yield return new object[] { DateInterval.Minute, 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { DateInterval.Minute, 0.0, now, now };
            yield return new object[] { DateInterval.Minute, 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { DateInterval.Minute, 2.0, DateTime.MinValue, DateTime.MinValue.AddMinutes(2) };
            yield return new object[] { DateInterval.Minute, 2.0, now, now.AddMinutes(2) };
            yield return new object[] { DateInterval.Minute, -2.0, now, now.AddMinutes(-2) };
            yield return new object[] { DateInterval.Minute, -2.0, DateTime.MaxValue, DateTime.MaxValue.AddMinutes(-2) };

            yield return new object[] { DateInterval.Second, 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { DateInterval.Second, 0.0, now, now };
            yield return new object[] { DateInterval.Second, 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { DateInterval.Second, 2.0, DateTime.MinValue, DateTime.MinValue.AddSeconds(2) };
            yield return new object[] { DateInterval.Second, 2.0, now, now.AddSeconds(2) };
            yield return new object[] { DateInterval.Second, -2.0, now, now.AddSeconds(-2) };
            yield return new object[] { DateInterval.Second, -2.0, DateTime.MaxValue, DateTime.MaxValue.AddSeconds(-2) };
        }

        private static IEnumerable<object[]> DateAdd_DateInterval_ArgumentOutOfRangeException_TestData()
        {
            yield return new object[] { DateInterval.Year, 2.0, DateTime.MaxValue };
            yield return new object[] { DateInterval.Year, -2.0, DateTime.MinValue };

            yield return new object[] { DateInterval.Day, 2.0, DateTime.MaxValue };
            yield return new object[] { DateInterval.Day, -2.0, DateTime.MinValue };

            yield return new object[] { DateInterval.Second, 2.0, DateTime.MaxValue };
            yield return new object[] { DateInterval.Second, -2.0, DateTime.MinValue };
        }

        [Theory]
        [MemberData(nameof(DateAdd_StringInterval_TestData))]
        public void DateAdd(string interval, double number, object dateValue, DateTime expected)
        {
            Assert.Equal(expected, DateAndTime.DateAdd(interval, number, dateValue));
        }

        [Theory]
        [MemberData(nameof(DateAdd_StringInterval_ArgumentOutOfRangeException_TestData))]
        public void DateAdd_ArgumentOutOfRangeException(string interval, double number, object dateValue)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DateAndTime.DateAdd(interval, number, dateValue));
        }

        private static IEnumerable<object[]> DateAdd_StringInterval_TestData()
        {
            var now = DateTime.UtcNow;
            var calendar = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar;

            yield return new object[] { "YYYY", 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { "YYYY", 0.0, now, now };
            yield return new object[] { "YYYY", 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { "YYYY", 2.0, DateTime.MinValue, calendar.AddYears(DateTime.MinValue, 2) };
            yield return new object[] { "YYYY", 2.0, now, calendar.AddYears(now, 2) };
            yield return new object[] { "YYYY", -2.0, now, calendar.AddYears(now, -2) };
            yield return new object[] { "YYYY", -2.0, DateTime.MaxValue, calendar.AddYears(DateTime.MaxValue, -2) };

            yield return new object[] { "Q", 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { "Q", 0.0, now, now };
            yield return new object[] { "Q", 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { "Q", 2.0, DateTime.MinValue, calendar.AddMonths(DateTime.MinValue, 6) };
            yield return new object[] { "Q", 2.0, now, calendar.AddMonths(now, 6) };
            yield return new object[] { "Q", -2.0, now, calendar.AddMonths(now, -6) };
            yield return new object[] { "Q", -2.0, DateTime.MaxValue, calendar.AddMonths(DateTime.MaxValue, -6) };

            yield return new object[] { "M", 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { "M", 0.0, now, now };
            yield return new object[] { "M", 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { "M", 2.0, DateTime.MinValue, calendar.AddMonths(DateTime.MinValue, 2) };
            yield return new object[] { "M", 2.0, now, calendar.AddMonths(now, 2) };
            yield return new object[] { "M", -2.0, now, calendar.AddMonths(now, -2) };
            yield return new object[] { "M", -2.0, DateTime.MaxValue, calendar.AddMonths(DateTime.MaxValue, -2) };

            yield return new object[] { "Y", 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { "Y", 0.0, now, now };
            yield return new object[] { "Y", 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { "Y", 2.0, DateTime.MinValue, DateTime.MinValue.AddDays(2) };
            yield return new object[] { "Y", 2.0, now, now.AddDays(2) };
            yield return new object[] { "Y", -2.0, now, now.AddDays(-2) };
            yield return new object[] { "Y", -2.0, DateTime.MaxValue, DateTime.MaxValue.AddDays(-2) };

            yield return new object[] { "D", 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { "D", 0.0, now, now };
            yield return new object[] { "D", 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { "D", 2.0, DateTime.MinValue, DateTime.MinValue.AddDays(2) };
            yield return new object[] { "D", 2.0, now, now.AddDays(2) };
            yield return new object[] { "D", -2.0, now, now.AddDays(-2) };
            yield return new object[] { "D", -2.0, DateTime.MaxValue, DateTime.MaxValue.AddDays(-2) };

            yield return new object[] { "WW", 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { "WW", 0.0, now, now };
            yield return new object[] { "WW", 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { "WW", 2.0, DateTime.MinValue, DateTime.MinValue.AddDays(14) };
            yield return new object[] { "WW", 2.0, now, now.AddDays(14) };
            yield return new object[] { "WW", -2.0, now, now.AddDays(-14) };
            yield return new object[] { "WW", -2.0, DateTime.MaxValue, DateTime.MaxValue.AddDays(-14) };

            yield return new object[] { "W", 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { "W", 0.0, now, now };
            yield return new object[] { "W", 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { "W", 2.0, DateTime.MinValue, DateTime.MinValue.AddDays(2) };
            yield return new object[] { "W", 2.0, now, now.AddDays(2) };
            yield return new object[] { "W", -2.0, now, now.AddDays(-2) };
            yield return new object[] { "W", -2.0, DateTime.MaxValue, DateTime.MaxValue.AddDays(-2) };

            yield return new object[] { "H", 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { "H", 0.0, now, now };
            yield return new object[] { "H", 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { "H", 2.0, DateTime.MinValue, DateTime.MinValue.AddHours(2) };
            yield return new object[] { "H", 2.0, now, now.AddHours(2) };
            yield return new object[] { "H", -2.0, now, now.AddHours(-2) };
            yield return new object[] { "H", -2.0, DateTime.MaxValue, DateTime.MaxValue.AddHours(-2) };

            yield return new object[] { "N", 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { "N", 0.0, now, now };
            yield return new object[] { "N", 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { "N", 2.0, DateTime.MinValue, DateTime.MinValue.AddMinutes(2) };
            yield return new object[] { "N", 2.0, now, now.AddMinutes(2) };
            yield return new object[] { "N", -2.0, now, now.AddMinutes(-2) };
            yield return new object[] { "N", -2.0, DateTime.MaxValue, DateTime.MaxValue.AddMinutes(-2) };

            yield return new object[] { "S", 0.0, DateTime.MinValue, DateTime.MinValue };
            yield return new object[] { "S", 0.0, now, now };
            yield return new object[] { "S", 0.0, DateTime.MaxValue, DateTime.MaxValue };
            yield return new object[] { "S", 2.0, DateTime.MinValue, DateTime.MinValue.AddSeconds(2) };
            yield return new object[] { "S", 2.0, now, now.AddSeconds(2) };
            yield return new object[] { "S", -2.0, now, now.AddSeconds(-2) };
            yield return new object[] { "S", -2.0, DateTime.MaxValue, DateTime.MaxValue.AddSeconds(-2) };
        }

        private static IEnumerable<object[]> DateAdd_StringInterval_ArgumentOutOfRangeException_TestData()
        {
            yield return new object[] { "YYYY", 2.0, DateTime.MaxValue };
            yield return new object[] { "YYYY", -2.0, DateTime.MinValue };

            yield return new object[] { "D", 2.0, DateTime.MaxValue };
            yield return new object[] { "D", -2.0, DateTime.MinValue };

            yield return new object[] { "S", 2.0, DateTime.MaxValue };
            yield return new object[] { "S", -2.0, DateTime.MinValue };
        }

        [Theory]
        [MemberData(nameof(DateDiff_DateInterval_TestData))]
        public void DateDiff(DateInterval interval, DateTime dateTime1, DateTime dateTime2, long expected)
        {
            Assert.Equal(expected, DateAndTime.DateDiff(interval, dateTime1, dateTime2));
        }

        private static IEnumerable<object[]> DateDiff_DateInterval_TestData()
        {
            var now = DateTime.UtcNow;

            yield return new object[] { DateInterval.Year, DateTime.MinValue, DateTime.MinValue, 0 };
            yield return new object[] { DateInterval.Year, now, now, 0 };
            yield return new object[] { DateInterval.Year, DateTime.MaxValue, DateTime.MaxValue, 0 };

            yield return new object[] { DateInterval.Quarter, now, now, 0 };

            yield return new object[] { DateInterval.Month, now, now, 0 };

            yield return new object[] { DateInterval.DayOfYear, now, now, 0 };
            yield return new object[] { DateInterval.DayOfYear, now, now.AddDays(2), 2 };
            yield return new object[] { DateInterval.DayOfYear, now, now.AddDays(-2), -2 };

            yield return new object[] { DateInterval.Day, now, now, 0 };
            yield return new object[] { DateInterval.Day, now, now.AddDays(2), 2 };
            yield return new object[] { DateInterval.Day, now, now.AddDays(-2), -2 };

            yield return new object[] { DateInterval.Hour, now, now, 0 };
            yield return new object[] { DateInterval.Hour, now, now.AddHours(2), 2 };
            yield return new object[] { DateInterval.Hour, now, now.AddHours(-2), -2 };

            yield return new object[] { DateInterval.Minute, now, now, 0 };
            yield return new object[] { DateInterval.Minute, now, now.AddMinutes(2), 2 };
            yield return new object[] { DateInterval.Minute, now, now.AddMinutes(-2), -2 };

            yield return new object[] { DateInterval.Second, now, now, 0 };
            yield return new object[] { DateInterval.Second, now, now.AddSeconds(2), 2 };
            yield return new object[] { DateInterval.Second, now, now.AddSeconds(-2), -2 };
        }

        [Theory]
        [MemberData(nameof(DateDiff_StringInterval_TestData))]
        public void DateDiff(string interval, object dateTime1, object dateTime2, long expected)
        {
            Assert.Equal(expected, DateAndTime.DateDiff(interval, dateTime1, dateTime2));
        }

        private static IEnumerable<object[]> DateDiff_StringInterval_TestData()
        {
            var now = DateTime.UtcNow;

            yield return new object[] { "YYYY", DateTime.MinValue, DateTime.MinValue, 0 };
            yield return new object[] { "YYYY", now, now, 0 };
            yield return new object[] { "YYYY", DateTime.MaxValue, DateTime.MaxValue, 0 };

            yield return new object[] { "Q", now, now, 0 };

            yield return new object[] { "M", now, now, 0 };

            yield return new object[] { "Y", now, now, 0 };
            yield return new object[] { "Y", now, now.AddDays(2), 2 };
            yield return new object[] { "Y", now, now.AddDays(-2), -2 };

            yield return new object[] { "D", now, now, 0 };
            yield return new object[] { "D", now, now.AddDays(2), 2 };
            yield return new object[] { "D", now, now.AddDays(-2), -2 };

            yield return new object[] { "H", now, now, 0 };
            yield return new object[] { "H", now, now.AddHours(2), 2 };
            yield return new object[] { "H", now, now.AddHours(-2), -2 };

            yield return new object[] { "N", now, now, 0 };
            yield return new object[] { "N", now, now.AddMinutes(2), 2 };
            yield return new object[] { "N", now, now.AddMinutes(-2), -2 };

            yield return new object[] { "S", now, now, 0 };
            yield return new object[] { "S", now, now.AddSeconds(2), 2 };
            yield return new object[] { "S", now, now.AddSeconds(-2), -2 };
        }

        [Theory]
        [MemberData(nameof(DatePart_DateInterval_TestData))]
        public void DatePart(DateInterval interval, DateTime dateValue, int expected)
        {
            Assert.Equal(expected, DateAndTime.DatePart(interval, dateValue));
        }

        private static IEnumerable<object[]> DatePart_DateInterval_TestData()
        {
            var now = DateTime.UtcNow;
            var calendar = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar;

            yield return new object[] { DateInterval.Year, now, calendar.GetYear(now) };
            yield return new object[] { DateInterval.Month, now, calendar.GetMonth(now) };
            yield return new object[] { DateInterval.Day, now, calendar.GetDayOfMonth(now) };
            yield return new object[] { DateInterval.Hour, now, calendar.GetHour(now) };
            yield return new object[] { DateInterval.Minute, now, calendar.GetMinute(now) };
            yield return new object[] { DateInterval.Second, now, calendar.GetSecond(now) };
        }

        [Theory]
        [MemberData(nameof(DatePart_StringInterval_TestData))]
        public void DatePart(string interval, object dateValue, int expected)
        {
            Assert.Equal(expected, DateAndTime.DatePart(interval, dateValue));
        }

        private static IEnumerable<object[]> DatePart_StringInterval_TestData()
        {
            var now = DateTime.UtcNow;
            var calendar = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar;

            yield return new object[] { "YYYY", now, calendar.GetYear(now) };
            yield return new object[] { "M", now, calendar.GetMonth(now) };
            yield return new object[] { "D", now, calendar.GetDayOfMonth(now) };
            yield return new object[] { "H", now, calendar.GetHour(now) };
            yield return new object[] { "N", now, calendar.GetMinute(now) };
            yield return new object[] { "S", now, calendar.GetSecond(now) };
        }

        // Not tested:
        //   public static DateTime DateSerial(int Year, int Month, int Day) { throw null; }

        [Fact]
        public void DateString()
        {
            string str = DateAndTime.DateString;
            // Should return a date with three non-empty parts.
            string[] parts = str.Split('-');
            Assert.Equal(3, parts.Length);
            foreach (var part in parts)
            {
                Assert.False(string.IsNullOrEmpty(part));
            }
        }

        // Not tested:
        //   public static string DateString { set { } }
        //   public static DateTime DateValue(string StringDate) { throw null; }

        [Fact]
        public void Fields()
        {
            var now = DateTime.UtcNow;
            var calendar = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar;

            Assert.Equal(calendar.GetYear(now), DateAndTime.Year(now));
            Assert.Equal(calendar.GetMonth(now), DateAndTime.Month(now));
            Assert.Equal(calendar.GetDayOfMonth(now), DateAndTime.Day(now));
            Assert.Equal(calendar.GetHour(now), DateAndTime.Hour(now));
            Assert.Equal(calendar.GetMinute(now), DateAndTime.Minute(now));
            Assert.Equal(calendar.GetSecond(now), DateAndTime.Second(now));
        }

        [Fact]
        public void MonthName()
        {
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                Assert.Throws<ArgumentException>(() => DateAndTime.MonthName(0, Abbreviate: false));
                Assert.Throws<ArgumentException>(() => DateAndTime.MonthName(0, Abbreviate: true));

                Assert.Equal("January", DateAndTime.MonthName(1, Abbreviate: false));
                Assert.Equal("Jan", DateAndTime.MonthName(1, Abbreviate: true));

                Assert.Equal("December", DateAndTime.MonthName(12, Abbreviate: false));
                Assert.Equal("Dec", DateAndTime.MonthName(12, Abbreviate: true));

                Assert.Throws<ArgumentException>(() => DateAndTime.MonthName(int.MaxValue, Abbreviate: false));
                Assert.Throws<ArgumentException>(() => DateAndTime.MonthName(int.MaxValue, Abbreviate: true));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        [Fact]
        public void Now()
        {
            var dateTimeNowBefore = DateTime.Now;
            var now = DateAndTime.Now;
            var dateTimeNowAfter = DateTime.Now;

            Assert.InRange(now, dateTimeNowBefore, dateTimeNowAfter);
        }

        // Not tested:
        //   public static DateTime TimeOfDay { get { throw null; } set { } }
        //   public static double Timer { get { throw null; } }
        //   public static DateTime TimeSerial(int Hour, int Minute, int Second) { throw null; }
        //   public static string TimeString { get { throw null; } set { } }
        //   public static DateTime TimeValue(string StringTime) { throw null; }

        [Fact]
        public void Today()
        {
            var dateTimeTodayBefore = DateTime.Today;
            var today = DateAndTime.Today;
            var dateTimeTodayAfter = DateTime.Today;

            Assert.InRange(today, dateTimeTodayBefore, dateTimeTodayAfter);
            Assert.Equal(TimeSpan.Zero, today.TimeOfDay);
        }

        // Not tested:
        //   public static DateTime Today { set { } }
        //   public static int Weekday(System.DateTime DateValue, FirstDayOfWeek DayOfWeek = FirstDayOfWeek.Sunday) { throw null; }

        [Fact]
        public void WeekdayName()
        {
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                Assert.Throws<ArgumentException>(() => DateAndTime.WeekdayName(0, Abbreviate: false));
                Assert.Throws<ArgumentException>(() => DateAndTime.WeekdayName(0, Abbreviate: true));

                Assert.Equal("Sunday", DateAndTime.WeekdayName(1, Abbreviate: false));
                Assert.Equal("Sun", DateAndTime.WeekdayName(1, Abbreviate: true));

                Assert.Equal("Saturday", DateAndTime.WeekdayName(7, Abbreviate: false));
                Assert.Equal("Sat", DateAndTime.WeekdayName(7, Abbreviate: true));

                Assert.Throws<ArgumentException>(() => DateAndTime.WeekdayName(int.MaxValue, Abbreviate: false));
                Assert.Throws<ArgumentException>(() => DateAndTime.WeekdayName(int.MaxValue, Abbreviate: true));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            }
        }
    }
}
