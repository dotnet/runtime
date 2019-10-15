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
        private static readonly int[] s_daysInMonthInLeapYear = new int[] { 0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        private static readonly int[] s_daysInMonthInCommonYear = new int[] { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        public static IEnumerable<object[]> GetDaysInMonth_TestData()
        {
            // Leap year, month that isn't February
            yield return new object[] { RandomLeapYear(), RandomMonthNotFebruary() };

            // Leap year, February
            yield return new object[] { RandomLeapYear(), 2 };

            // Common year, February
            yield return new object[] { RandomCommonYear(), 2 };

            // Common year, month that isn't February
            yield return new object[] { RandomCommonYear(), RandomMonthNotFebruary() };

            // Max supported year
            yield return new object[] { 9999, RandomMonth() };

            // Min supported year
            yield return new object[] { 1, RandomMonth() };

            // Random year, random month
            yield return new object[] { RandomYear(), RandomMonth() };

            // Random year, minimum month
            yield return new object[] { RandomYear(), 1 };

            // Random year, maximum month
            yield return new object[] { RandomYear(), 12 };
        }

        [Theory]
        [MemberData(nameof(GetDaysInMonth_TestData))]
        public void GetDaysInMonth(int year, int month)
        {
            GregorianCalendar calendar = new GregorianCalendar();
            int expected = IsLeapYear(year) ? s_daysInMonthInLeapYear[month] : s_daysInMonthInCommonYear[month];
            Assert.Equal(expected, calendar.GetDaysInMonth(year, month));
            Assert.Equal(expected, calendar.GetDaysInMonth(year, month, 0));
            Assert.Equal(expected, calendar.GetDaysInMonth(year, month, 1));
        }
    }
}
