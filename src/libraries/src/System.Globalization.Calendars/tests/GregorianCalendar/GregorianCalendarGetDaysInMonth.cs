// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetDaysInMonth
    {
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        private static readonly int[] s_daysInMonthInLeapYear = new int[] { 0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        private static readonly int[] s_daysInMonthInCommonYear = new int[] { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        public static IEnumerable<object[]> GetDaysInMonth_TestData()
        {
            int randomMonthNotFebruary = RandomMonthNotFebruary();

            // Leap year, month that isn't February
            yield return new object[] { RandomLeapYear(), randomMonthNotFebruary, s_daysInMonthInLeapYear[randomMonthNotFebruary] };

            // Leap year, February
            yield return new object[] { RandomLeapYear(), 2, s_daysInMonthInLeapYear[2] };

            // Common year, February
            yield return new object[] { RandomCommonYear(), 2, s_daysInMonthInCommonYear[2] };

            // Common year, month that isn't February
            yield return new object[] { RandomCommonYear(), randomMonthNotFebruary, s_daysInMonthInCommonYear[randomMonthNotFebruary] };

            // Max supported year
            int maxYear = s_calendar.MaxSupportedDateTime.Year;
            int maxYearRandomMonth = RandomMonth();
            int maxYearExpected = IsLeapYear(maxYear) ? s_daysInMonthInLeapYear[maxYearRandomMonth] : s_daysInMonthInCommonYear[maxYearRandomMonth];
            yield return new object[] { maxYear, maxYearRandomMonth, maxYearExpected };

            // Min supported year
            int minYear = s_calendar.MinSupportedDateTime.Year;
            int minYearRandomMonth = RandomMonth();
            int minYearExpected = IsLeapYear(minYear) ? s_daysInMonthInLeapYear[minYearRandomMonth] : s_daysInMonthInCommonYear[minYearRandomMonth];
            yield return new object[] { minYear, minYearRandomMonth, minYearExpected };

            // Random year, random month
            int randomYear1 = RandomYear();
            int randomMonth1 = RandomMonth();
            int randomYearExpected1 = IsLeapYear(randomYear1) ? s_daysInMonthInLeapYear[randomMonth1] : s_daysInMonthInCommonYear[randomMonth1];
            yield return new object[] { randomYear1, randomMonth1, randomYearExpected1 };

            // Random year, minimum month
            int randomYear2 = RandomYear();
            int randomYearExpected2 = IsLeapYear(randomYear2) ? s_daysInMonthInLeapYear[1] : s_daysInMonthInCommonYear[1];
            yield return new object[] { randomYear2, 1, randomYearExpected2 };

            // Random year, maximum month
            int randomYear3 = RandomYear();
            int randomYearExpected3 = IsLeapYear(randomYear3) ? s_daysInMonthInLeapYear[12] : s_daysInMonthInCommonYear[12];
            yield return new object[] { randomYear3, 12, randomYearExpected3 };
        }

        [Theory]
        [MemberData(nameof(GetDaysInMonth_TestData))]
        public void GetDaysInMonth(int year, int month, int expected)
        {
            Assert.Equal(expected, s_calendar.GetDaysInMonth(year, month));
        }
    }
}
