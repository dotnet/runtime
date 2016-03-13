// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    // GregorianCalendar.GetDaysInMonth(Int32, Int32)
    public class GregorianCalendarGetDaysInMonth
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private static readonly int[] s_daysInMonth365 =
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        private static readonly int[] s_daysInMonth366 =
        {
            0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        #region Positive tests
        // PosTest1: leap year, any month other than February
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int expectedDays, actualDays;
            year = GetALeapYear(myCalendar);
            //Get a random value beween 1 and 12 not including 2.
            do
            {
                month = _generator.GetInt32(-55) % 12 + 1;
            } while (2 == month);
            expectedDays = s_daysInMonth366[month];

            actualDays = myCalendar.GetDaysInMonth(year, month);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest2: leap year, February
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int expectedDays, actualDays;
            year = GetALeapYear(myCalendar);
            month = 2;
            expectedDays = s_daysInMonth366[month];
            actualDays = myCalendar.GetDaysInMonth(year, month);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest3: common year, February
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int expectedDays, actualDays;
            year = GetACommonYear(myCalendar);
            month = 2;
            expectedDays = s_daysInMonth365[month];
            actualDays = myCalendar.GetDaysInMonth(year, month);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest4: common year, any month other than February
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int expectedDays, actualDays;
            year = GetACommonYear(myCalendar);
            //Get a random value beween 1 and 12 not including 2.
            do
            {
                month = _generator.GetInt32(-55) % 12 + 1;
            } while (2 == month);
            expectedDays = s_daysInMonth365[month];
            actualDays = myCalendar.GetDaysInMonth(year, month);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest5: Maximum supported year, any month
        [Fact]
        public void PosTest5()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int expectedDays, actualDays;
            year = myCalendar.MaxSupportedDateTime.Year;
            //Get a random month whose value is beween 1 and 12
            month = _generator.GetInt32(-55) % 12 + 1;
            expectedDays = (IsLeapYear(year)) ? s_daysInMonth366[month] : s_daysInMonth365[month];
            actualDays = myCalendar.GetDaysInMonth(year, month);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest6: Minimum supported year, any month
        [Fact]
        public void PosTest6()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int expectedDays, actualDays;
            year = myCalendar.MaxSupportedDateTime.Year;
            //Get a random month whose value is beween 1 and 12
            month = _generator.GetInt32(-55) % 12 + 1;
            expectedDays = (IsLeapYear(year)) ? s_daysInMonth366[month] : s_daysInMonth365[month];
            actualDays = myCalendar.GetDaysInMonth(year, month);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest7: Any year, any month
        [Fact]
        public void PosTest7()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int expectedDays, actualDays;
            year = GetAYear(myCalendar);
            //Get a random month whose value is beween 1 and 12
            month = _generator.GetInt32(-55) % 12 + 1;
            expectedDays = (IsLeapYear(year)) ? s_daysInMonth366[month] : s_daysInMonth365[month];
            actualDays = myCalendar.GetDaysInMonth(year, month);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest8: Any year,  the minimum month
        [Fact]
        public void PosTest8()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int expectedDays, actualDays;
            year = myCalendar.MaxSupportedDateTime.Year;
            month = 1;
            expectedDays = (IsLeapYear(year)) ? s_daysInMonth366[month] : s_daysInMonth365[month];
            actualDays = myCalendar.GetDaysInMonth(year, month);
            Assert.Equal(expectedDays, actualDays);
        }

        // PosTest9: Any year,  the maximum month
        [Fact]
        public void PosTest9()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month;
            int expectedDays, actualDays;
            year = myCalendar.MaxSupportedDateTime.Year;
            month = 12;
            expectedDays = (IsLeapYear(year)) ? s_daysInMonth366[month] : s_daysInMonth365[month];
            actualDays = myCalendar.GetDaysInMonth(year, month);
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
        private string GetParamsInfo(int year, int month)
        {
            string str;
            str = string.Format("\nThe specified month is in {0}-{1}(year-month).", year, month);
            return str;
        }
        #endregion
    }
}
