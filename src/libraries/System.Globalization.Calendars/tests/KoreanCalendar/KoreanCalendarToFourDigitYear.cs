// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarToFourDigitYear
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> ToFourDigitYear_TestData()
        {
            yield return new object[] { 0 };
            yield return new object[] { 99 };
            yield return new object[] { s_randomDataGenerator.GetInt16(-55) % 100 };
        }

        [Theory]
        [MemberData(nameof(ToFourDigitYear_TestData))]
        public void ToFourDigitYear(int year)
        {
            KoreanCalendar calendar = new KoreanCalendar();
            int lBound = calendar.TwoDigitYearMax - 99;
            int rBound = calendar.TwoDigitYearMax;
            int expectedValue;
            if (year < (lBound % 100))
            {
                expectedValue = (lBound / 100 + 1) * 100 + year;
            }
            else
            {
                expectedValue = (lBound / 100) * 100 + year;
            }
            Assert.Equal(expectedValue, calendar.ToFourDigitYear(year));
        }

        [Fact]
        public void ToFourDigitYear_InvalidYear_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("year", () => new KoreanCalendar().ToFourDigitYear(100));
        }
    }
}
