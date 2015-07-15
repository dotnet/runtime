// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    // GregorianCalendar.GetMonthsInYear(Int32, Int32)
    public class GregorianCalendarGetMonthsInYear
    {
        private const int c_MONTHS_IN_YEAR = 12;
        private const int c_AD_ERA = 1;
        private const int c_CURRENT_ERA = 0;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive tests
        // PosTest2: leap year, era is anno Domini
        [Fact]
        public void PosTest1()
        {
            int era = c_AD_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedMonths, actualMonths;
            year = GetALeapYear(myCalendar);
            expectedMonths = c_MONTHS_IN_YEAR;
            actualMonths = myCalendar.GetMonthsInYear(year, era);
            Assert.Equal(expectedMonths, actualMonths);
        }

        // PosTest2: leap year, era is current era
        [Fact]
        public void PosTest2()
        {
            int era = c_CURRENT_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedMonths, actualMonths;
            year = GetALeapYear(myCalendar);
            expectedMonths = c_MONTHS_IN_YEAR;
            actualMonths = myCalendar.GetMonthsInYear(year, era);
            Assert.Equal(expectedMonths, actualMonths);
        }

        // PosTest3: common year, era is anno Domini
        [Fact]
        public void PosTest3()
        {
            int era = c_AD_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedMonths, actualMonths;
            year = GetACommonYear(myCalendar);
            expectedMonths = c_MONTHS_IN_YEAR;
            actualMonths = myCalendar.GetMonthsInYear(year, era);
            Assert.Equal(expectedMonths, actualMonths);
        }

        // PosTest4: common year, era is current era
        [Fact]
        public void PosTest4()
        {
            int era = c_CURRENT_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedMonths, actualMonths;
            year = GetACommonYear(myCalendar);
            expectedMonths = c_MONTHS_IN_YEAR;
            actualMonths = myCalendar.GetMonthsInYear(year, era);
            Assert.Equal(expectedMonths, actualMonths);
        }

        // PosTest5: any year, era is anno Domini
        [Fact]
        public void PosTest5()
        {
            int era = c_AD_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedMonths, actualMonths;
            year = GetAYear(myCalendar);
            expectedMonths = c_MONTHS_IN_YEAR;
            actualMonths = myCalendar.GetMonthsInYear(year, era);
            Assert.Equal(expectedMonths, actualMonths);
        }

        // PosTest6: any year, era is current era
        [Fact]
        public void PosTest6()
        {
            int era = c_CURRENT_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedMonths, actualMonths;
            year = GetAYear(myCalendar);
            expectedMonths = c_MONTHS_IN_YEAR;
            actualMonths = myCalendar.GetMonthsInYear(year, era);
            Assert.Equal(expectedMonths, actualMonths);
        }

        // PosTest7: Maximum supported year, era is anno Domini
        [Fact]
        public void PosTest7()
        {
            int era = c_AD_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedMonths, actualMonths;
            year = myCalendar.MaxSupportedDateTime.Year;
            expectedMonths = c_MONTHS_IN_YEAR;
            actualMonths = myCalendar.GetMonthsInYear(year, era);
            Assert.Equal(expectedMonths, actualMonths);
        }

        // PosTest8: Maximum supported year, era is current era
        [Fact]
        public void PosTest8()
        {
            int era = c_CURRENT_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedMonths, actualMonths;
            year = myCalendar.MaxSupportedDateTime.Year;
            expectedMonths = c_MONTHS_IN_YEAR;
            actualMonths = myCalendar.GetMonthsInYear(year, era);
            Assert.Equal(expectedMonths, actualMonths);
        }

        // PosTest9: Minimum supported year, era is anno Domini
        [Fact]
        public void PosTest9()
        {
            int era = c_AD_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedMonths, actualMonths;
            year = myCalendar.MinSupportedDateTime.Year;
            expectedMonths = c_MONTHS_IN_YEAR;
            actualMonths = myCalendar.GetMonthsInYear(year, era);
            Assert.Equal(expectedMonths, actualMonths);
        }

        // PosTest10: Minimum supported year, era is current era
        [Fact]
        public void PosTest10()
        {
            int era = c_CURRENT_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            int expectedMonths, actualMonths;
            year = myCalendar.MinSupportedDateTime.Year;
            expectedMonths = c_MONTHS_IN_YEAR;
            actualMonths = myCalendar.GetMonthsInYear(year, era);
            Assert.Equal(expectedMonths, actualMonths);
        }
        #endregion

        #region Negative Tests
        // NegTest1: year is greater than maximum supported value, era is anno Domini
        [Fact]
        public void NegTest1()
        {
            int era = c_AD_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            year = myCalendar.MaxSupportedDateTime.Year + 100;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.GetMonthsInYear(year, era);
            });
        }

        // NegTest2: year is greater than maximum supported value, era is current era
        [Fact]
        public void NegTest2()
        {
            int era = c_CURRENT_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            year = myCalendar.MaxSupportedDateTime.Year + 100;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.GetMonthsInYear(year, era);
            });
        }

        // NegTest3: year is less than maximum supported value, era is anno Domini
        [Fact]
        public void NegTest3()
        {
            int era = c_AD_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            year = myCalendar.MinSupportedDateTime.Year - 100;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
           {
               myCalendar.GetMonthsInYear(year, era);
           });
        }

        // NegTest4: year is less than maximum supported value, era is current era
        [Fact]
        public void NegTest4()
        {
            int era = c_CURRENT_ERA;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year;
            year = myCalendar.MinSupportedDateTime.Year - 100;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.GetMonthsInYear(year, era);
            });
        }

        // NegTest3: era is outside the range supported by the calendar
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, era;
            year = GetAYear(myCalendar);
            era = 3;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.GetMonthsInYear(year, era);
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
        #endregion
    }
}