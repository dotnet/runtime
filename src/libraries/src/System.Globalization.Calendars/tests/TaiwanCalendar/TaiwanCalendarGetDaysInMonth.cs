// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.GetDayOfYear(Int32,Int32,Int32)
    public class TaiwanCalendarGetDaysInMonth
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
        // PosTest1: Verify the day is a random Date
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 12);
            int era;
            int actualDays;
            if (tc.IsLeapYear(year))
            {
                actualDays = _DAYS_PER_MONTHS_IN_LEAP_YEAR[month];
            }
            else
            {
                actualDays = _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month];
            }

            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.Equal(tc.GetDaysInMonth(year, month, era), actualDays);
            }
        }

        // PosTest2: Verify year is TaiwanCalendar MinSupportedDateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MinSupportedDateTime;
            int year = dt.Year;
            int month = dt.Month;
            int era;
            int actualDays;
            if (tc.IsLeapYear(year))
            {
                actualDays = _DAYS_PER_MONTHS_IN_LEAP_YEAR[month];
            }
            else
            {
                actualDays = _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month];
            }

            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.Equal(tc.GetDaysInMonth(year, month, era), actualDays);
            }
        }

        // PosTest3: Verify the DateTime is TaiwanCalendar MaxSupportDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MaxSupportedDateTime;
            int year = dt.Year - 1911;
            int month = dt.Month;
            int era;
            int actualDays;
            if (tc.IsLeapYear(year))
            {
                actualDays = _DAYS_PER_MONTHS_IN_LEAP_YEAR[month];
            }
            else
            {
                actualDays = _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month];
            }

            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.Equal(tc.GetDaysInMonth(year, month, era), actualDays);
            }
        }

        // PosTest4: Verify the DateTime is leap year
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = new DateTime(2000, 12, 31);
            int year = dt.Year;
            int month = dt.Month;
            int era;
            int actualDays;
            if (tc.IsLeapYear(year))
            {
                actualDays = _DAYS_PER_MONTHS_IN_LEAP_YEAR[month];
            }
            else
            {
                actualDays = _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month];
            }
            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.Equal(tc.GetDaysInMonth(year, month, era), actualDays);
            }
        }
        #endregion
    }
}
