// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.AddMonths(DateTime,System.Int32)
    public class TaiwanCalendarAddMonths
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private readonly int[] _DAYS_PER_MONTHS_IN_LEAP_YEAR = new int[13]
        {
            0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };
        private readonly int[] _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR = new int[13]
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        #region Positive Test
        // PosTest1: Verify the add months greater than zero
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

            DateTime dt = new DateTime(year, month, day);
            int addvalue;
            if ((tc.MaxSupportedDateTime.Year - year - 1911) > 1000)
            {
                addvalue = rand.Next(1, 1000 * 12);
            }
            else
            {
                addvalue = rand.Next(1, (tc.MaxSupportedDateTime.Year - year - 1911) * 12);
            }

            VerificationHelper(dt, addvalue);
        }

        // PosTest2: Verify the add months less than zero
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

            DateTime dt = new DateTime(year, month, day);
            int addvalue;
            if ((tc.MinSupportedDateTime.Year - year) < -1000)
            {
                addvalue = rand.Next(-1000 * 12, 0);
            }
            else
            {
                addvalue = rand.Next((tc.MinSupportedDateTime.Year - year) * 12, 0);
            }

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
        #endregion

        #region Negative tests
        // NegTest1: The resulting DateTime is greater than the supported range
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MaxSupportedDateTime;
            int addValue = _generator.GetInt32(-55);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.AddMonths(dt, addValue);
            });
        }

        // NegTest2: The resulting DateTime is less than the supported range
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MinSupportedDateTime;
            int addValue = _generator.GetInt32(-55);
            addValue = -addValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.AddMonths(dt, addValue);
            });
        }

        // NegTest3: the add months is less than -120000
        [Fact]
        public void NegTest3()
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

            DateTime dt = new DateTime(year, month, day);
            int addValue = rand.Next(Int32.MinValue, -120000);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.AddMonths(dt, addValue);
            });
        }

        // NegTest4: the add months is greater than 120000
        [Fact]
        public void NegTest4()
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

            DateTime dt = new DateTime(year, month, day);
            int addValue = rand.Next(120000, Int32.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.AddMonths(dt, addValue);
            });
        }
        #endregion

        #region Helper Methods
        private bool IsLeapYear(int year)
        {
            return ((year % 4) == 0) && !(((year % 100) == 0) || ((year % 400) == 0));
        }

        private void VerificationHelper(DateTime value, int addValue)
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime newDate = tc.AddMonths(value, addValue);
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

            if (month < 1)
            {
                year--;
                month = 12 + month;
            }

            int dayInMonth = 0;
            if (IsLeapYear(year))
            {
                dayInMonth = _DAYS_PER_MONTHS_IN_LEAP_YEAR[month];
            }
            else
            {
                dayInMonth = _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month];
            }

            day = day > dayInMonth ? dayInMonth : day;
            DateTime desiredDate = new DateTime(year, month, day, value.Hour, value.Minute, value.Second, value.Millisecond);

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