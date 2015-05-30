// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    // GregorianCalendar.GetEra(DateTime)
    public class GregorianCalendarGetEra
    {
        private const int c_DAYS_IN_LEAP_YEAR = 366;
        private const int c_DAYS_IN_COMMON_YEAR = 365;
        private const int c_AD_ERA = 1;
        private const int c_CURRENT_ERA = 0;

        #region Positive tests
        // PosTest1: the speicified time is in leap year, February
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            DateTime time;
            int year, month;
            int expectedEra, actualEra;
            year = GetALeapYear(myCalendar);
            month = 2;
            time = myCalendar.ToDateTime(year, month, 29, 10, 30, 12, 0);
            expectedEra = c_AD_ERA;
            actualEra = myCalendar.GetEra(time);
            Assert.Equal(expectedEra, actualEra);
        }

        // PosTest2: the speicified time is in leap year, any month other than February
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            DateTime time;
            int expectedEra, actualEra;
            year = GetALeapYear(myCalendar);
            //Get a random value beween 1 and 12 not including 2.
            do
            {
                month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            } while (2 == month);
            time = myCalendar.ToDateTime(year, month, 28, 10, 30, 20, 0);
            expectedEra = c_AD_ERA;
            actualEra = myCalendar.GetEra(time);
            Assert.Equal(expectedEra, actualEra);
        }

        // PosTest3: the speicified time is in common year, February
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            DateTime time;
            int expectedEra, actualEra;
            year = GetACommonYear(myCalendar);
            month = 2;
            time = myCalendar.ToDateTime(year, month, 28, 10, 20, 30, 0);
            expectedEra = c_AD_ERA;
            actualEra = myCalendar.GetEra(time);
            Assert.Equal(expectedEra, actualEra);
        }

        // PosTest4: the speicified time is in common year, any month other than February
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            DateTime time;
            int expectedEra, actualEra;
            year = GetACommonYear(myCalendar);
            //Get a random value beween 1 and 12 not including 2.
            do
            {
                month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            } while (2 == month);
            time = myCalendar.ToDateTime(year, month, 28, 10, 30, 20, 0);
            expectedEra = c_AD_ERA;
            actualEra = myCalendar.GetEra(time);
            Assert.Equal(expectedEra, actualEra);
        }

        // PosTest5: the speicified time is in minimum supported year, any month
        [Fact]
        public void PosTest5()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            DateTime time;
            int expectedEra, actualEra;
            year = myCalendar.MinSupportedDateTime.Year;
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            time = myCalendar.ToDateTime(year, month, 20, 8, 20, 30, 0);
            expectedEra = c_AD_ERA;
            actualEra = myCalendar.GetEra(time);
            Assert.Equal(expectedEra, actualEra);
        }

        // PosTest6: the speicified time is in maximum supported year, any month
        [Fact]
        public void PosTest6()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            DateTime time;
            int expectedEra, actualEra;
            year = myCalendar.MaxSupportedDateTime.Year;
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            time = myCalendar.ToDateTime(year, month, 20, 8, 20, 30, 0);
            expectedEra = c_AD_ERA;
            actualEra = myCalendar.GetEra(time);
            Assert.Equal(expectedEra, actualEra);
        }

        // PosTest7: the speicified time is in any year, minimum month
        [Fact]
        public void PosTest7()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            DateTime time;
            int expectedEra, actualEra;
            year = myCalendar.MinSupportedDateTime.Year;
            month = 1;
            time = myCalendar.ToDateTime(year, month, 20, 8, 20, 30, 0);
            expectedEra = c_AD_ERA;
            actualEra = myCalendar.GetEra(time);
            Assert.Equal(expectedEra, actualEra);
        }

        // PosTest8: the speicified time is in any year, maximum month
        [Fact]
        public void PosTest8()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            DateTime time;
            int expectedEra, actualEra;
            year = myCalendar.MaxSupportedDateTime.Year;
            month = 12;
            time = myCalendar.ToDateTime(year, month, 20, 8, 20, 30, 0);
            expectedEra = c_AD_ERA;
            actualEra = myCalendar.GetEra(time);
            Assert.Equal(expectedEra, actualEra);
        }

        // PosTest9: the speicified time is in any year, any month
        [Fact]
        public void PosTest9()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            DateTime time;
            int expectedEra, actualEra;
            year = GetAYear(myCalendar);
            month = TestLibrary.Generator.GetInt32(-55) % 12 + 1;
            time = myCalendar.ToDateTime(year, month, 20, 8, 20, 30, 0);
            expectedEra = c_AD_ERA;
            actualEra = myCalendar.GetEra(time);
            Assert.Equal(expectedEra, actualEra);
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

        //Get text represntation of the input parmeters
        private string GetParamsInfo(int year)
        {
            string str;
            str = string.Format("\nThe specified year is {0:04}(yyyy).", year);
            return str;
        }

        //Get text represntation of the input parmeters
        private string GetParamsInfo(DateTime time)
        {
            string str;
            str = string.Format("\nThe specified time is ({0}).", time);
            return str;
        }
        #endregion
    }
}