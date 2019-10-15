// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarGetDaysInMonth
    {
        private static readonly int[] s_daysPerMonthLeapYear = new int[13]
        {
            0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        private static readonly int[] s_daysPerMonthCommonYear = new int[13]
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        public static IEnumerable<object[]> GetDaysInMonth_TestData()
        {
            yield return new object[] { 1912, 1 };
            yield return new object[] { 8088, 12 };
            yield return new object[] { 2000, 12 };
            yield return new object[] { TaiwanCalendarUtilities.RandomYear(), TaiwanCalendarUtilities.RandomMonth() };
        }

        [Theory]
        [MemberData(nameof(GetDaysInMonth_TestData))]
        public static void GetDaysInMonth(int year, int month)
        {
            TaiwanCalendar calendar = new TaiwanCalendar();
            int expected;
            if (calendar.IsLeapYear(year))
            {
                expected = s_daysPerMonthLeapYear[month];
            }
            else
            {
                expected = s_daysPerMonthCommonYear[month];
            }
            Assert.Equal(expected, calendar.GetDaysInMonth(year, month));
            Assert.Equal(expected, calendar.GetDaysInMonth(year, month, 0));
            Assert.Equal(expected, calendar.GetDaysInMonth(year, month, 1));
        }
    }
}
