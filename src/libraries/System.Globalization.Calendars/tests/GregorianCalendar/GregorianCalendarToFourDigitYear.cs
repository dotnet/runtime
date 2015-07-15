// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        #region Negative tests
        //NegTest1: two-digit year is a negative value
        [Fact]
        public void Test4()
        {
            NegTest1(GregorianCalendarTypes.Arabic);
            NegTest1(GregorianCalendarTypes.Localized);
            NegTest1(GregorianCalendarTypes.MiddleEastFrench);
            NegTest1(GregorianCalendarTypes.TransliteratedEnglish);
            NegTest1(GregorianCalendarTypes.TransliteratedFrench);
            NegTest1(GregorianCalendarTypes.USEnglish);
        }

        // NegTest2: two-digit year is a value greater than maximum two-digit year
        [Fact]
        public void Test5()
        {
            NegTest2(GregorianCalendarTypes.Arabic);
            NegTest2(GregorianCalendarTypes.Localized);
            NegTest2(GregorianCalendarTypes.MiddleEastFrench);
            NegTest2(GregorianCalendarTypes.TransliteratedEnglish);
            NegTest2(GregorianCalendarTypes.TransliteratedFrench);
            NegTest2(GregorianCalendarTypes.USEnglish);
        }

        private void NegTest1(GregorianCalendarTypes calendarType)
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(calendarType);
            int twoDigitYear;
            twoDigitYear = -1 * _generator.GetInt32(-55);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.ToFourDigitYear(twoDigitYear);
            });
        }

        private void NegTest2(GregorianCalendarTypes calendarType)
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(calendarType);
            int twoDigitYear;
            twoDigitYear = c_MAX_TWO_DIGIT_YEAR + _generator.GetInt32(-55) % (int.MaxValue - c_MAX_TWO_DIGIT_YEAR);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.ToFourDigitYear(twoDigitYear);
            });
        }

        #endregion
    }
}