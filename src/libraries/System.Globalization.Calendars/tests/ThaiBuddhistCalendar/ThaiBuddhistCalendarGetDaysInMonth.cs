// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.GetDaysInMonth(Int32,Int32,Int32)
    public class ThaiBuddhistCalendarGetDaysInMonth
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
        // PosTest1: Verify the month is a random month
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year + 543, tbc.MaxSupportedDateTime.Year + 544);
            int month = rand.Next(1, 12);
            int era;
            int actualDays;
            if (IsLeapYear(year))
            {
                actualDays = _DAYS_PER_MONTHS_IN_LEAP_YEAR[month];
            }
            else
            {
                actualDays = _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month];
            }

            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(actualDays, tbc.GetDaysInMonth(year, month, era));
            }
        }

        // PosTest2: Verify month is ThaiBuddhistCalendar MinSupportedDateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MinSupportedDateTime;
            int year = dt.Year + 543;
            int month = dt.Month;
            int era;
            int actualDays = 31;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(actualDays, tbc.GetDaysInMonth(year, month, era));
            }
        }

        // PosTest3: Verify the month is ThaiBuddhistCalendar MaxSupportDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MaxSupportedDateTime;
            int year = dt.Year - 1911;
            int month = dt.Month;
            int era;
            int actualDays = 31;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(actualDays, tbc.GetDaysInMonth(year, month, era));
            }
        }

        // PosTest4: Verify the DateTime is leap year
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            int year = 2000 + 543;
            int month = 2;
            int era;
            int actualDays = 29;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(actualDays, tbc.GetDaysInMonth(year, month, era));
            }
        }
        #endregion

        #region Negative Tests
        // NegTest5: The era is greater than 1
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year + 543, tbc.MaxSupportedDateTime.Year + 544);
            int month = rand.Next(1, 13);
            int era = 2;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tbc.GetDaysInMonth(year, month, era);
            });
        }
        #endregion

        #region Helper Methods
        private bool IsLeapYear(int i)
        {
            int year = i - 543;
            return ((year % 4) == 0) && !(((year % 100) == 0) || ((year % 400) == 0));
        }
        #endregion
    }
}