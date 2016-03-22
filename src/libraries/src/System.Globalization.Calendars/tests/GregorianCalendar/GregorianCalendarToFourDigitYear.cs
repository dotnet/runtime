// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarToFourDigitYear
    {
        private const int MaxTwoDigitYear = 99;
        private const int MinTwoDigitYear = 0;

        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> ToFourDigitYear_TestData()
        {
            GregorianCalendarTypes[] calendarTypes = new GregorianCalendarTypes[]
            {
                GregorianCalendarTypes.Arabic,
                GregorianCalendarTypes.Localized,
                GregorianCalendarTypes.MiddleEastFrench,
                GregorianCalendarTypes.TransliteratedEnglish,
                GregorianCalendarTypes.TransliteratedFrench,
                GregorianCalendarTypes.USEnglish
            };

            foreach (GregorianCalendarTypes calendarType in calendarTypes)
            {
                // 0-99
                yield return new object[] { calendarType, s_randomDataGenerator.GetInt32(-55) % (MaxTwoDigitYear + 1) };

                // Min two digit year
                yield return new object[] { calendarType, MinTwoDigitYear };

                // Max two digit year
                yield return new object[] { calendarType, MaxTwoDigitYear };
            }
        }

        [Theory]
        [MemberData(nameof(ToFourDigitYear_TestData))]
        private void ToFourDigitYear(GregorianCalendarTypes calendarType, int year)
        {
            Calendar calendar = new GregorianCalendar(calendarType);
            int expected = GetExpectedFourDigitYear(calendar, year);
            Assert.Equal(expected, calendar.ToFourDigitYear(year));
        }

        private static int GetExpectedFourDigitYear(Calendar calendar, int twoDigitYear)
        {
            int expectedFourDigitYear = calendar.TwoDigitYearMax - calendar.TwoDigitYearMax % 100 + twoDigitYear;
            if (expectedFourDigitYear > calendar.TwoDigitYearMax)
            {
                expectedFourDigitYear -= 100;
            }
            return expectedFourDigitYear;
        }
    }
}
