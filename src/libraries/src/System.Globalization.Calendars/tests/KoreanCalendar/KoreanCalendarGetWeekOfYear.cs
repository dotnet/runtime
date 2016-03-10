// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.GetWeekOfYear(System.DateTime,System.Globalization.CalendarWeekRule,System.DayOfWeek)
    public class KoreanCalendarGetWeekOfYear
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Test Logic
        // PosTest1:invoke the method with min dateTime
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(1, 1, 1, 0, 0, 0, 0);
            int expectedValue = gC.GetWeekOfYear(dateTime, new CultureInfo("en-US").DateTimeFormat.CalendarWeekRule, new CultureInfo("en-US").DateTimeFormat.FirstDayOfWeek);
            int actualValue;
            actualValue = kC.GetWeekOfYear(kC.ToDateTime(dateTime.Year + 2333, dateTime.Month, dateTime.Day, 0, 0, 0, 0), new CultureInfo("ko-KR").DateTimeFormat.CalendarWeekRule, new CultureInfo("ko-KR").DateTimeFormat.FirstDayOfWeek);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:invoke the method with max dateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(9999, 12, 31, 0, 0, 0, 0);
            int expectedValue = gC.GetWeekOfYear(dateTime, new CultureInfo("en-US").DateTimeFormat.CalendarWeekRule, new CultureInfo("en-US").DateTimeFormat.FirstDayOfWeek);
            int actualValue;
            actualValue = kC.GetWeekOfYear(kC.ToDateTime(dateTime.Year + 2333, dateTime.Month, dateTime.Day, 0, 0, 0, 0), new CultureInfo("ko-KR").DateTimeFormat.CalendarWeekRule, new CultureInfo("ko-KR").DateTimeFormat.FirstDayOfWeek);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:invoke the method with leap year dateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(2008, 2, 29, 0, 0, 0, 0);
            int expectedValue = gC.GetWeekOfYear(dateTime, new CultureInfo("en-US").DateTimeFormat.CalendarWeekRule, new CultureInfo("en-US").DateTimeFormat.FirstDayOfWeek);
            int actualValue;
            actualValue = kC.GetWeekOfYear(kC.ToDateTime(dateTime.Year + 2333, dateTime.Month, dateTime.Day, 0, 0, 0, 0), new CultureInfo("ko-KR").DateTimeFormat.CalendarWeekRule, new CultureInfo("ko-KR").DateTimeFormat.FirstDayOfWeek);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4:invoke the method with random dateTime
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            Int64 ticks = _generator.GetInt64(-55) % DateTime.MaxValue.Ticks + 1;
            DateTime dateTime = new DateTime(ticks);
            dateTime = gC.ToDateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            int expectedValue = gC.GetWeekOfYear(dateTime, new CultureInfo("en-US").DateTimeFormat.CalendarWeekRule, new CultureInfo("en-US").DateTimeFormat.FirstDayOfWeek);
            int actualValue;
            actualValue = kC.GetWeekOfYear(kC.ToDateTime(dateTime.Year + 2333, dateTime.Month, dateTime.Day, 0, 0, 0, 0), new CultureInfo("ko-KR").DateTimeFormat.CalendarWeekRule, new CultureInfo("ko-KR").DateTimeFormat.FirstDayOfWeek);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}
