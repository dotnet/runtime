// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    // GregorianCalendar.GetDaysInYear(Int32)
    public class GregorianCalendarGetDaysInYear
    {
        private const int c_DAYS_IN_LEAP_YEAR = 366;
        private const int c_DAYS_IN_COMMON_YEAR = 365;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive tests
        // PosTest1: leap year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedDays, actualDays;
            year = GetALeapYear(myCalendar);
            expectedDays = c_DAYS_IN_LEAP_YEAR;
            actualDays = myCalendar.GetDaysInYear(year);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest2: common year
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedDays, actualDays;
            year = GetACommonYear(myCalendar);
            expectedDays = c_DAYS_IN_COMMON_YEAR;
            actualDays = myCalendar.GetDaysInYear(year);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest3: any year
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedDays, actualDays;
            year = GetAYear(myCalendar);
            expectedDays = (IsLeapYear(year)) ? c_DAYS_IN_LEAP_YEAR : c_DAYS_IN_COMMON_YEAR;
            actualDays = myCalendar.GetDaysInYear(year);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest4: Maximum supported year
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedDays, actualDays;
            year = myCalendar.MaxSupportedDateTime.Year;
            expectedDays = (IsLeapYear(year)) ? c_DAYS_IN_LEAP_YEAR : c_DAYS_IN_COMMON_YEAR;
            actualDays = myCalendar.GetDaysInYear(year);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest5: Minimum supported year
        [Fact]
        public void PosTest5()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedDays, actualDays;
            year = myCalendar.MinSupportedDateTime.Year;
            expectedDays = (IsLeapYear(year)) ? c_DAYS_IN_LEAP_YEAR : c_DAYS_IN_COMMON_YEAR;
            actualDays = myCalendar.GetDaysInYear(year);
            Assert.Equal(expectedDays, actualDays);
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
        private string GetParamsInfo(int year)
        {
            string str;
            str = string.Format("\nThe specified year is {0:04}(yyyy).", year);
            return str;
        }
        #endregion
    }
}
