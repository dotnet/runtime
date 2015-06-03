// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.GetDayOfMonth(DateTime)
    public class TaiwanCalendarGetDayOfMonth
    {
        #region PositiveTesting
        // PosTest1: Verify the day is a random Date
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 12);
            int day;
            if (tc.IsLeapYear(year))
            {
                day = rand.Next(1, 30);
            }
            else
            {
                day = rand.Next(1, 29);
            }

            DateTime dt = tc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int actualDays = dt.Day;
            Assert.Equal(tc.GetDayOfMonth(dt), actualDays);
        }

        // PosTest2: Verify the DateTime is a leap year
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = 2000;
            int month = 2;
            int day;
            DateTime dt;
            for (day = 1; day < 30; day++)
            {
                dt = new DateTime(year, month, day);
                Assert.Equal(tc.GetDayOfMonth(dt), day);
            }
        }

        // PosTest3: Verify the DateTime is TaiwanCalendar MaxSupportDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MaxSupportedDateTime;
            Assert.Equal(31, tc.GetDayOfMonth(dt));
        }

        // PosTest4: Verify the DateTime is TaiwanCalendar MinSupportedDateTime
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MinSupportedDateTime;
            Assert.Equal(1, tc.GetDayOfMonth(dt));
        }
        #endregion
    }
}