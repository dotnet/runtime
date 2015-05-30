// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    // GregorianCalendar.IsLeapMonth(Int32, Int32, Int32, Int32)
    public class GregorianCalendarIsLeapMonth
    {
        private const int c_DAYS_IN_LEAP_YEAR = 366;
        private const int c_DAYS_IN_COMMON_YEAR = 365;

        #region Positive tests
        // PosTest1: February in leap year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            bool expectedValue;
            bool actualValue;
            year = GetALeapYear(myCalendar);
            month = 2;
            expectedValue = false;
            actualValue = myCalendar.IsLeapMonth(year, month, 1);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2: february in common year
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            bool expectedValue;
            bool actualValue;
            year = GetACommonYear(myCalendar);
            month = 2;
            expectedValue = false;
            actualValue = myCalendar.IsLeapMonth(year, month, 1);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3: any month, any year
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            bool expectedValue;
            bool actualValue;
            year = GetAYear(myCalendar);
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            expectedValue = false;
            actualValue = myCalendar.IsLeapMonth(year, month, 1);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4: any month in maximum supported year
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            bool expectedValue;
            bool actualValue;
            year = myCalendar.MaxSupportedDateTime.Year;
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            expectedValue = false;
            actualValue = myCalendar.IsLeapMonth(year, month, 1);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest5: any month in minimum supported year
        [Fact]
        public void PosTest5()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            bool expectedValue;
            bool actualValue;
            year = myCalendar.MinSupportedDateTime.Year;
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            expectedValue = false;
            actualValue = myCalendar.IsLeapMonth(year, month, 1);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion

        #region Negative Tests
        // NegTest1: year is greater than maximum supported value
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            year = myCalendar.MaxSupportedDateTime.Year + 100;
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapMonth(year, month, 1);
            });
        }

        // NegTest2: year is less than minimum supported value
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            year = myCalendar.MinSupportedDateTime.Year - 100;
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapMonth(year, month, 1);
            });
        }

        // NegTest3: era is outside the range supported by the calendar
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int era;
            year = this.GetAYear(myCalendar);
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            era = 2 + TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - 1);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapMonth(year, month, era);
            });
        }

        // NegTest4: month is less than the minimum value supported by the calendar
        [Fact]
        public void NegTest4()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int era;
            year = this.GetAYear(myCalendar);
            month = -1 * TestLibrary.Generator.GetInt32(-55);
            era = TestLibrary.Generator.GetInt32(-55) & 1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapMonth(year, month, era);
            });
        }

        // NegTest5: month is greater than the maximum value supported by the calendar
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int era;
            year = this.GetAYear(myCalendar);
            month = 13 + TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - 12);
            era = TestLibrary.Generator.GetInt32(-55) & 1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.IsLeapMonth(year, month, era);
            });
        }
        #endregion

        #region Helper methods for all the tests
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

        //Get text represntation of the input parmeters
        private string GetParamsInfo(int year, int month)
        {
            string str;
            str = string.Format("\nThe specified date is {0:04}-{1:02} (yyyy-mm).", year, month);
            return str;
        }
        #endregion
    }
}