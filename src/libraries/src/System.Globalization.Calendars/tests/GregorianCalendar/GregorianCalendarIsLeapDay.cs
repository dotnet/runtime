// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    // GregorianCalendar.IsLeapDay(Int32, Int32, Int32, Int32)
    public class GregorianCalendarIsLeapDay
    {
        private const int c_DAYS_IN_LEAP_YEAR = 366;
        private const int c_DAYS_IN_COMMON_YEAR = 365;
        private static readonly int[] s_daysInMonth365 =
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };
        private static readonly int[] s_daysInMonth366 =
        {
            0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        #region Positive tests
        // PosTest1: February 29 in leap year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            bool expectedValue;
            bool actualValue;
            year = GetALeapYear(myCalendar);
            month = 2;
            day = 29;
            expectedValue = true;
            actualValue = myCalendar.IsLeapDay(year, month, day, 1);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2: February 28 in common year
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            bool expectedValue;
            bool actualValue;
            year = GetACommonYear(myCalendar);
            month = 2;
            day = 28;
            expectedValue = false;
            actualValue = myCalendar.IsLeapDay(year, month, day, 1);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3: any year, any month, any day
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            bool expectedValue;
            bool actualValue;
            year = GetAYear(myCalendar);
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            day = (this.IsLeapYear(year)) ? TestLibrary.Generator.GetInt32(-55) % s_daysInMonth366[month] + 1 : TestLibrary.Generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            expectedValue = this.IsLeapYear(year) && 2 == month && 29 == day;
            actualValue = myCalendar.IsLeapDay(year, month, day, 1);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4: any day and month in maximum supported year
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            bool expectedValue;
            bool actualValue;
            year = myCalendar.MaxSupportedDateTime.Year;
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            day = (this.IsLeapYear(year)) ? TestLibrary.Generator.GetInt32(-55) % s_daysInMonth366[month] + 1 : TestLibrary.Generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            expectedValue = this.IsLeapYear(year) && 2 == month && 29 == day;
            actualValue = myCalendar.IsLeapDay(year, month, day, 1);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest5: any day and month in minimum supported year
        [Fact]
        public void PosTest5()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            bool expectedValue;
            bool actualValue;
            year = myCalendar.MinSupportedDateTime.Year;
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            day = (this.IsLeapYear(year)) ? TestLibrary.Generator.GetInt32(-55) % s_daysInMonth366[month] + 1 : TestLibrary.Generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            expectedValue = this.IsLeapYear(year) && 2 == month && 29 == day;
            actualValue = myCalendar.IsLeapDay(year, month, day, 1);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion

        #region Negtive Tests
        // NegTest1: year is greater than maximum supported value
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            year = myCalendar.MaxSupportedDateTime.Year + 100;
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            day = (this.IsLeapYear(year)) ? TestLibrary.Generator.GetInt32(-55) % s_daysInMonth366[month] + 1 : TestLibrary.Generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapDay(year, month, day, 1);
            });
        }

        // NegTest2: year is less than minimum supported value
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            year = myCalendar.MinSupportedDateTime.Year - 100;
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            day = (this.IsLeapYear(year)) ? TestLibrary.Generator.GetInt32(-55) % s_daysInMonth366[month] + 1 : TestLibrary.Generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapDay(year, month, day, 1);
            });
        }

        // NegTest3: era is outside the range supported by the calendar
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            int era;
            year = this.GetAYear(myCalendar);
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            day = (this.IsLeapYear(year)) ? TestLibrary.Generator.GetInt32(-55) % s_daysInMonth366[month] + 1 : TestLibrary.Generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            era = 2 + TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - 1);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapDay(year, month, day, era);
            });
        }

        // NegTest4: month is outside the range supported by the calendar
        [Fact]
        public void NegTest4()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            int era;
            year = this.GetAYear(myCalendar);
            month = -1 * TestLibrary.Generator.GetInt32(-55);
            day = 1;
            era = TestLibrary.Generator.GetInt32(-55) & 1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapDay(year, month, day, era);
            });
        }

        // NegTest5: month is outside the range supported by the calendar
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            int era;
            year = this.GetAYear(myCalendar);
            month = 13 + TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - 12);
            day = 1;
            era = TestLibrary.Generator.GetInt32(-55) & 1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapDay(year, month, day, era);
            });
        }

        // NegTest6: day is outside the range supported by the calendar
        [Fact]
        public void NegTest6()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            int era;
            year = this.GetAYear(myCalendar);
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            day = -1 * TestLibrary.Generator.GetInt32(-55);
            era = TestLibrary.Generator.GetInt32(-55) & 1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapDay(year, month, day, era);
            });
        }
        #endregion

        #region Helper methods for all the tests
        //Indicate whether the specified year is leap year or not
        private bool IsLeapYear(int year)
        {
            if (0 == year % 400 || (0 != year % 100 && 0 == (year & 0x3)))
            {
                return true;
            }

            return false;
        }

        //Get a random year beween minmum supported year and maximum supported year of the specified calendar
        private int GetAYear(Calendar calendar)
        {
            int retVal;
            int maxYear, minYear;
            maxYear = calendar.MaxSupportedDateTime.Year;
            minYear = calendar.MinSupportedDateTime.Year;
            retVal = minYear + TestLibrary.Generator.GetInt32(-55) % (maxYear + 1 - minYear);
            return retVal;
        }

        //Get a leap year of the specified calendar
        private int GetALeapYear(Calendar calendar)
        {
            int retVal;
            // A leap year is any year divisible by 4 except for centennial years(those ending in 00)
            // which are only leap years if they are divisible by 400.
            retVal = ~(~GetAYear(calendar) | 0x3); // retVal will be divisible by 4 since the 2 least significant bits will be 0
            retVal = (0 != retVal % 100) ? retVal : (retVal - retVal % 400); // if retVal is divisible by 100 subtract years from it to make it divisible by 400
                                                                             // if retVal was 100, 200, or 300 the above logic will result in 0
            if (0 == retVal)
            {
                retVal = 400;
            }

            return retVal;
        }

        //Get a common year of the specified calendar
        private int GetACommonYear(Calendar calendar)
        {
            int retVal;
            do
            {
                retVal = GetAYear(calendar);
            }
            while ((0 == (retVal & 0x3) && 0 != retVal % 100) || 0 == retVal % 400);
            return retVal;
        }
        #endregion
    }
}