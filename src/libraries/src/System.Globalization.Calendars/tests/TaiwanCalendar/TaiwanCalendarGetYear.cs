// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.GetYear(DateTime)
    public class TaiwanCalendarGetYear
    {
        private readonly int[] _DAYS_PER_MONTHS_IN_LEAP_YEAR = new int[13] { 0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        private readonly int[] _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR = new int[13] { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        #region Positive Tests
        // PosTest1: Verify the DateTime is a random Date
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year + 1);
            int month = rand.Next(1, 12);
            int day;
            if (IsLeapYear(year))
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_LEAP_YEAR[month] + 1);
            }
            else
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month] + 1);
            }

            DateTime dt = new DateTime(year, month, day);

            int actualYear = dt.Year - 1911;
            Assert.Equal(tc.GetYear(dt), actualYear);
        }

        // PosTest2: Verify the DateTime is TaiwanCalendar MaxSupportDateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MaxSupportedDateTime;
            Assert.Equal(tc.GetYear(dt), 8088);
        }

        // PosTest3: Verify the DateTime is TaiwanCalendar MinSupportedDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MinSupportedDateTime;
            Assert.Equal(tc.GetYear(dt), 1);
        }
        #endregion

        #region Helper Methods
        private bool IsLeapYear(int year)
        {
            return ((year % 4) == 0) && !(((year % 100) == 0) || ((year % 400) == 0));
        }
        #endregion
    }
}

