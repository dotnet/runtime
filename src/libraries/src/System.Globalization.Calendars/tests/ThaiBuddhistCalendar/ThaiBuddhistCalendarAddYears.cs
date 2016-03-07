// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Globalization.Tests;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.AddYears(DateTime,System.Int32)
    public class ThaiBuddhistCalendarAddYears
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

        #region Positive Tests
        // PosTest1: Verify the add years greater than zero
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year + 543, tbc.MaxSupportedDateTime.Year + 543);
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

            DateTime dt = tbc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int addvalue = rand.Next(1, (tbc.MaxSupportedDateTime.Year - year + 543));
            VerificationHelper(dt, addvalue);
        }

        // PosTest2: Verify the add years less than zero
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year + 543, tbc.MaxSupportedDateTime.Year + 543);
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

            DateTime dt = tbc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int addvalue = rand.Next((tbc.MinSupportedDateTime.Year - year + 543), 0);
            VerificationHelper(dt, addvalue);
        }

        // PosTest3: Verify the DateTime is ThaiBuddhistCalendar MaxSupportDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MaxSupportedDateTime;
            int i = 0;
            VerificationHelper(dt, i);
        }

        // PosTest4: Verify the DateTime is ThaiBuddhistCalendar MinSupportedDateTime
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MinSupportedDateTime;
            int i = 0;
            VerificationHelper(dt, i);
        }

        // PosTest5: Verify the add years is zero
        [Fact]
        public void PosTest5()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year + 543, tbc.MaxSupportedDateTime.Year + 543);
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

            DateTime dt = tbc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int i = 0;
            VerificationHelper(dt, i);
        }
        #endregion
        
        [Fact]
        public void AddYears_Invalid()
        {
            CalendarHelpers.AddYears_Invalid(new ThaiBuddhistCalendar());
        }

        #region Helper Methods
        private bool IsLeapYear(int i)
        {
            int year = i - 543;
            return ((year % 4) == 0) && !(((year % 100) == 0) || ((year % 400) == 0));
        }

        private void VerificationHelper(DateTime value, int addValue)
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime newDate = tbc.AddYears(value, addValue);
            _errorNo++;
            Assert.Equal(newDate.Year, (value.Year + addValue));

            if (value.Month == 2)
            {
                if ((IsLeapYear(value.Year) && value.Day == 29) || (!IsLeapYear(value.Year) && value.Day == 28))
                {
                    if (IsLeapYear(newDate.Year))
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
