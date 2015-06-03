// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.GetYear(DateTime)
    public class ThaiBuddhistCalendarGetYear
    {
        private readonly int[] _DAYS_PER_MONTHS_IN_LEAP_YEAR = new int[13]
        {
            0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };
        private readonly int[] _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR = new int[13]
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };


        #region Positive Tests
        // PosTest1: Verify the DateTime is a random Date
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year, tbc.MaxSupportedDateTime.Year + 1);
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
            int actualYear = dt.Year + 543;
            Assert.Equal(actualYear, tbc.GetYear(dt));
        }

        // PosTest2: Verify the DateTime is ThaiBuddhistCalendar MaxSupportDateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MaxSupportedDateTime;
            Assert.Equal(10542, tbc.GetYear(dt));
        }

        // PosTest3: Verify the DateTime is ThaiBuddhistCalendar MinSupportedDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MinSupportedDateTime;
            Assert.Equal(544, tbc.GetYear(dt));
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