// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarTwoDigitYearMax
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        private const int DefaultTwoDigitMax = 2029;
        private const int MaxYear = 9999;
        private const int MinTwoDigitYear = 99;

        [Theory]
        [InlineData(GregorianCalendarTypes.Arabic)]
        [InlineData(GregorianCalendarTypes.Localized)]
        [InlineData(GregorianCalendarTypes.MiddleEastFrench)]
        [InlineData(GregorianCalendarTypes.TransliteratedEnglish)]
        [InlineData(GregorianCalendarTypes.TransliteratedFrench)]
        [InlineData(GregorianCalendarTypes.USEnglish)]
        public void TwoDigitYearMax(GregorianCalendarTypes calendarType)
        {
            Calendar calendar = new GregorianCalendar(calendarType);
            Assert.Equal(DefaultTwoDigitMax, calendar.TwoDigitYearMax);

            int randomTwoDigitYearMax = MinTwoDigitYear + s_randomDataGenerator.GetInt32(-55) % (MaxYear - MinTwoDigitYear + 1);
            calendar.TwoDigitYearMax = randomTwoDigitYearMax;
            Assert.Equal(randomTwoDigitYearMax, calendar.TwoDigitYearMax);
        }
    }
}
