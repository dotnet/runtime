// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.AddYears(DateTime,System.Int32)
    public class TaiwanCalendarAddYears
    {
        private int _errorNo = 0;
        private readonly int[] _DAYS_PER_MONTHS_IN_LEAP_YEAR = new int[13]
        {
            0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };
        private readonly int[] _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR = new int[13]
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        #region PositiveTesting
        // PosTest1: Verify the add years greater than zero
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
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_LEAP_YEAR[month] + 1);
            }
            else
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month] + 1);
            }

            DateTime dt = tc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int addvalue = rand.Next(1, (tc.MaxSupportedDateTime.Year - year - 1911));
            VerificationHelper(dt, addvalue);
        }

        // PosTest2: Verify the add years less than zero
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 12);
            int day;
            if (tc.IsLeapYear(year))
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_LEAP_YEAR[month] + 1);
            }
            else
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month] + 1);
            }

            DateTime dt = tc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int addvalue = rand.Next((tc.MinSupportedDateTime.Year - year), 0);
            VerificationHelper(dt, addvalue);
        }

        // PosTest3: Verify the DateTime is TaiwanCalendar MaxSupportDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MaxSupportedDateTime;
            int i = 0;
            VerificationHelper(dt, i);
        }

        // PosTest4: Verify the DateTime is TaiwanCalendar MinSupportedDateTime
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MinSupportedDateTime;
            int i = 0;
            VerificationHelper(dt, i);
        }

        // PosTest5: Verify the add years is zero
        [Fact]
        public void PosTest5()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 12);
            int day;
            if (tc.IsLeapYear(year))
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_LEAP_YEAR[month] + 1);
            }
            else
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month] + 1);
            }

            DateTime dt = tc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int i = 0;
            VerificationHelper(dt, i);
        }
        #endregion

        #region Helper Methods
        private void VerificationHelper(DateTime value, int addValue)
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime newDate = tc.AddYears(value, addValue);
            _errorNo++;
            Assert.Equal(newDate.Year, (value.Year + addValue));

            if (value.Month == 2)
            {
                if ((DateTime.IsLeapYear(value.Year) && value.Day == 29) || (!DateTime.IsLeapYear(value.Year) && value.Day == 28))
                {
                    if (DateTime.IsLeapYear(newDate.Year))
                    {
                        Assert.Equal(29, newDate.Day);
                    }
                    else
                    {
                        Assert.Equal(28, newDate.Day);
                    }
                }
            }
        }
        #endregion
    }
}
