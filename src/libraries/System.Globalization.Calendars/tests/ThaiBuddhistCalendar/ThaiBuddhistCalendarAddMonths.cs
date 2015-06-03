// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.AddMonths(DateTime,System.Int32)
    public class ThaiBuddhistCalendarAddMonths
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

        #region Negative tests
        // NegTest1: The resulting DateTime is greater than the supported range
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MaxSupportedDateTime;
            int addValue = TestLibrary.Generator.GetInt32(-55);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tbc.AddMonths(dt, addValue);
            });
        }

        // NegTest2: The resulting DateTime is less than the supported range
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MinSupportedDateTime;
            int addValue = TestLibrary.Generator.GetInt32(-55);
            addValue = -addValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tbc.AddMonths(dt, addValue);
            });
        }

        // NegTest3: the add months is less than -120000
        [Fact]
        public void NegTest3()
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
            int addValue = rand.Next(Int32.MinValue, -120000);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tbc.AddMonths(dt, addValue);
            });
        }

        // NegTest4: the add months is greater than 120000
        [Fact]
        public void NegTest4()
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
            int addValue = rand.Next(120000, Int32.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tbc.AddMonths(dt, addValue);
            });
        }
        #endregion

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