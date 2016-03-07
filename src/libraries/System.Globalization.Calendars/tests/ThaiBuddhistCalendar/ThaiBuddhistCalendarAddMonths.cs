// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Globalization.Tests;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.AddMonths(DateTime,System.Int32)
    public class ThaiBuddhistCalendarAddMonths
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();
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
        // PosTest1: Verify the add months greater than zero
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.GetYear(tbc.MinSupportedDateTime), tbc.GetYear(tbc.MaxSupportedDateTime));
            int month = rand.Next(1, 12);
            int day;
            if (IsLeapYearThaiBuddhist(year))
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_LEAP_YEAR[month] + 1);
            }
            else
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month] + 1);
            }

            DateTime dt = tbc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int addvalue = rand.Next(1, (tbc.MaxSupportedDateTime.Year - year + 543) * 12);
            VerificationHelper(dt, addvalue);
        }

        // PosTest2: Verify the add months less than zero
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.GetYear(tbc.MinSupportedDateTime), tbc.GetYear(tbc.MaxSupportedDateTime));
            int month = rand.Next(1, 12);
            int day;
            if (IsLeapYearThaiBuddhist(year))
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_LEAP_YEAR[month] + 1);
            }
            else
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month] + 1);
            }

            DateTime dt = tbc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int addvalue = rand.Next(1, (tbc.MaxSupportedDateTime.Year - year + 543) * 12);
            VerificationHelper(dt, addvalue);
        }

        // PosTest3: Verify the add months is zero
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.GetYear(tbc.MinSupportedDateTime), tbc.GetYear(tbc.MaxSupportedDateTime));
            int month = rand.Next(1, 12);
            int day;
            if (IsLeapYearThaiBuddhist(year))
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_LEAP_YEAR[month] + 1);
            }
            else
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month] + 1);
            }

            DateTime dt = tbc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int addvalue = 0;
            DateTime actualDT = tbc.AddMonths(dt, addvalue);
            Assert.Equal(dt, actualDT);
        }

        // PosTest4: Verify the DateTime is ThaiBuddhistCalendar MaxSupportDateTime
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MaxSupportedDateTime;
            int i = 0;
            VerificationHelper(dt, i);
        }

        // PosTest5: Verify the DateTime is ThaiBuddhistCalendar MinSupportedDateTime
        [Fact]
        public void PosTest5()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MinSupportedDateTime;
            int i = 0;
            VerificationHelper(dt, i);
        }
        #endregion

        [Fact]
        public void AddMonths_Invalid()
        {
            CalendarHelpers.AddMonths_Invalid(new ThaiBuddhistCalendar());
        }

        #region Helper Methods
        private bool IsLeapYearThaiBuddhist(int year)
        {
            year = year - 543;
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        }

        private bool IsLeapYearGregorian(int year)
        {
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        }

        private void VerificationHelper(DateTime value, int addValue)
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime newDate = tbc.AddMonths(value, addValue);
            int totalYear = addValue / 12;
            int leftMonth = addValue % 12;
            int day = value.Day;
            int month = value.Month + leftMonth;
            int year = value.Year + totalYear;
            if (month > 12)
            {
                month = month % 12;
                year++;
            }

            int dayInMonth = 0;
            if (IsLeapYearGregorian(year))
            {
                dayInMonth = _DAYS_PER_MONTHS_IN_LEAP_YEAR[month];
            }
            else
            {
                dayInMonth = _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month];
            }

            day = day > dayInMonth ? dayInMonth : day;
            DateTime desiredDate = new DateTime(year, month, day, value.Hour, value.Minute, value.Second, value.Millisecond);
            _errorNo++;

            Assert.Equal(desiredDate.Year, newDate.Year);
            Assert.Equal(desiredDate.Month, newDate.Month);
            Assert.Equal(desiredDate.Day, newDate.Day);
            Assert.Equal(desiredDate.Hour, newDate.Hour);
            Assert.Equal(desiredDate.Minute, newDate.Minute);
            Assert.Equal(desiredDate.Second, newDate.Second);
            Assert.Equal(desiredDate.Millisecond, newDate.Millisecond);
        }
        #endregion
    }
}
