// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarIsLeapYear
    {
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        public static IEnumerable<object[]> IsLeapYear_TestData()
        {
            // Leap year
            yield return new object[] { RandomLeapYear(), true };

            // Common year
            yield return new object[] { RandomCommonYear(), false };

            // Random year
            int randomYear = RandomYear();
            yield return new object[] { randomYear, IsLeapYear(randomYear) };

            // Maximum supported year
            int maxYear = s_calendar.MaxSupportedDateTime.Year;
            yield return new object[] { maxYear, IsLeapYear(maxYear) };

            // Minimum supported year
            int minYear = s_calendar.MinSupportedDateTime.Year;
            yield return new object[] { minYear, IsLeapYear(minYear) };
        }

        [Theory]
        [MemberData(nameof(IsLeapYear_TestData))]
        public void IsLeapYearTest(int year, bool expected)
        {
            Assert.Equal(expected, s_calendar.IsLeapYear(year, 1));
        }
    }
}
