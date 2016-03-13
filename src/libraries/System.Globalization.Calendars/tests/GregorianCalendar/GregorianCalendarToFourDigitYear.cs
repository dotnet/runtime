// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    // GregorianCalendar.ToFourDigitYear(Int32) [v-yaduoj]
    public class GregorianCalendarToFourDigitYear
    {
        private const int c_MAX_TWO_DIGIT_YEAR = 99;
        private const int c_MIN_TWO_DIGIT_YEAR = 0;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive tests
        // PosTest1: random two-digit year between 0 and 99
        [Fact]
        public void Test1()
        {
            PosTest1(GregorianCalendarTypes.Arabic);
            PosTest1(GregorianCalendarTypes.Localized);
            PosTest1(GregorianCalendarTypes.MiddleEastFrench);
            PosTest1(GregorianCalendarTypes.TransliteratedEnglish);
            PosTest1(GregorianCalendarTypes.TransliteratedFrench);
            PosTest1(GregorianCalendarTypes.USEnglish);
        }

        // PosTest2: two-digit year is maximum supported two-digit year
        [Fact]
        public void Test2()
        {
            PosTest2(GregorianCalendarTypes.Arabic);
            PosTest2(GregorianCalendarTypes.Localized);
            PosTest2(GregorianCalendarTypes.MiddleEastFrench);
            PosTest2(GregorianCalendarTypes.TransliteratedEnglish);
            PosTest2(GregorianCalendarTypes.TransliteratedFrench);
            PosTest2(GregorianCalendarTypes.USEnglish);
        }

        // PosTest3: two-digit year is mininum supported two-digit year
        [Fact]
        public void Test3()
        {
            PosTest3(GregorianCalendarTypes.Arabic);
            PosTest3(GregorianCalendarTypes.Localized);
            PosTest3(GregorianCalendarTypes.MiddleEastFrench);
            PosTest3(GregorianCalendarTypes.TransliteratedEnglish);
            PosTest3(GregorianCalendarTypes.TransliteratedFrench);
            PosTest3(GregorianCalendarTypes.USEnglish);
        }

        private void PosTest1(GregorianCalendarTypes calendarType)
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(calendarType);
            int twoDigitYear;
            int expectedFourDigitYear, actualFourDigitYear;
            twoDigitYear = _generator.GetInt32(-55) % (c_MAX_TWO_DIGIT_YEAR + 1);
            expectedFourDigitYear = GetExpectedFourDigitYear(myCalendar, twoDigitYear);
            actualFourDigitYear = myCalendar.ToFourDigitYear(twoDigitYear);
            Assert.Equal(expectedFourDigitYear, actualFourDigitYear);
        }

        private void PosTest2(GregorianCalendarTypes calendarType)
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(calendarType);
            int twoDigitYear;
            int expectedFourDigitYear, actualFourDigitYear;
            twoDigitYear = c_MAX_TWO_DIGIT_YEAR;
            expectedFourDigitYear = GetExpectedFourDigitYear(myCalendar, twoDigitYear);
            actualFourDigitYear = myCalendar.ToFourDigitYear(twoDigitYear);
            Assert.Equal(expectedFourDigitYear, actualFourDigitYear);
        }

        private void PosTest3(GregorianCalendarTypes calendarType)
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(calendarType);
            int twoDigitYear;
            int expectedFourDigitYear, actualFourDigitYear;
            twoDigitYear = c_MIN_TWO_DIGIT_YEAR;
            expectedFourDigitYear = GetExpectedFourDigitYear(myCalendar, twoDigitYear);
            actualFourDigitYear = myCalendar.ToFourDigitYear(twoDigitYear);
            Assert.Equal(expectedFourDigitYear, actualFourDigitYear);
        }

        #endregion
        #region Helper methods for positve tests
        private int GetExpectedFourDigitYear(Calendar calendar, int twoDigitYear)
        {
            int expectedFourDigitYear;
            expectedFourDigitYear = calendar.TwoDigitYearMax - calendar.TwoDigitYearMax % 100 + twoDigitYear;
            if (expectedFourDigitYear > calendar.TwoDigitYearMax)
            {
                expectedFourDigitYear -= 100;
            }

            return expectedFourDigitYear;
        }
        #endregion
    }
}
