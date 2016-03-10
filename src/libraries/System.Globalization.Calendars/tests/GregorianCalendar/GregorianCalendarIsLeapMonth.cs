// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

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
            month = _generator.GetInt32(-55) % 12 + 1;
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
            month = _generator.GetInt32(-55) % 12 + 1;
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
            month = _generator.GetInt32(-55) % 12 + 1;
            expectedValue = false;
            actualValue = myCalendar.IsLeapMonth(year, month, 1);
            Assert.Equal(expectedValue, actualValue);
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
            retVal = minYear + _generator.GetInt32(-55) % (maxYear + 1 - minYear);
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
