// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    // GregorianCalendar.TwoDigitYearMax
    public class GregorianCalendarTwoDigitYearMax
    {
        private const int c_DEFAULT_TWO_DIGIT_YEAR_MAX = 2029;
        private const int c_MAX_YEAR = 9999;
        private const int c_MIN_TWO_DIGIT_YEAR = 99;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive tests
        // PosTest1: get the TwoDigitYearMax of Gregorian calendar
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

        // PosTest2: set the TwoDigitYearMax of Gregorian caleandar
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

        private void PosTest1(GregorianCalendarTypes calendarType)
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(calendarType);
            int expectedTwoDigitYearMax, actualTwoDigitYearMax;
            expectedTwoDigitYearMax = c_DEFAULT_TWO_DIGIT_YEAR_MAX;
            actualTwoDigitYearMax = myCalendar.TwoDigitYearMax;
            Assert.Equal(expectedTwoDigitYearMax, actualTwoDigitYearMax);
        }

        private void PosTest2(GregorianCalendarTypes calendarType)
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(calendarType);
            int expectedTwoDigitYearMax, actualTwoDigitYearMax;
            expectedTwoDigitYearMax = c_MIN_TWO_DIGIT_YEAR + _generator.GetInt32(-55) % (c_MAX_YEAR - c_MIN_TWO_DIGIT_YEAR + 1);
            myCalendar.TwoDigitYearMax = expectedTwoDigitYearMax;
            actualTwoDigitYearMax = myCalendar.TwoDigitYearMax;
            Assert.Equal(expectedTwoDigitYearMax, actualTwoDigitYearMax);
        }
        #endregion
    }
}