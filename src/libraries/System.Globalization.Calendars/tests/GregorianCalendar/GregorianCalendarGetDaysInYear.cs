// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetDaysInYear
    {
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        private const int DaysInLeapYear = 366;
        private const int DaysInCommonYear = 365;

        public static IEnumerable<object[]> GetDaysInYear_TestData()
        {
            // Leap year
            yield return new object[] { RandomLeapYear(), DaysInLeapYear };

            // Common year
            yield return new object[] { RandomCommonYear(), DaysInCommonYear };

            // Random year
            int randomYear = RandomYear();
            yield return new object[] { randomYear, IsLeapYear(randomYear) ? DaysInLeapYear : DaysInCommonYear };

            // Maximum supported year
            int maxYear = s_calendar.MaxSupportedDateTime.Year;
            yield return new object[] { maxYear, IsLeapYear(maxYear) ? DaysInLeapYear : DaysInCommonYear };

            // Minimum supported year
            int minYear = s_calendar.MaxSupportedDateTime.Year;
            yield return new object[] { minYear, IsLeapYear(minYear) ? DaysInLeapYear : DaysInCommonYear };
        }

        [Theory]
        [MemberData(nameof(GetDaysInYear_TestData))]
        public void GetDaysInYear(int year, int expected)
        {
            Assert.Equal(expected, s_calendar.GetDaysInYear(year));
        }

        [Fact]
        public void GetDaysInYear_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => s_calendar.GetDaysInYear(s_calendar.MinSupportedDateTime.Year - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_calendar.GetDaysInYear(s_calendar.MaxSupportedDateTime.Year + 1));
        }
    }
}
