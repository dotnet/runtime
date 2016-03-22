// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetMonth
    {
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        public static IEnumerable<object[]> GetMonth_TestData()
        {
            int randomMonth = RandomMonth();
            int randomMonthNotFebruary = RandomMonthNotFebruary();

            // February in a leap year
            yield return new object[] { s_calendar.ToDateTime(RandomLeapYear(), 2, 29, 10, 30, 12, 0), 2 };

            // Month other than february in a leap year
            yield return new object[] { s_calendar.ToDateTime(RandomLeapYear(), randomMonthNotFebruary, 28, 10, 30, 12, 0), randomMonthNotFebruary };

            // February in a common year
            yield return new object[] { s_calendar.ToDateTime(RandomCommonYear(), 2, 28, 10, 30, 12, 0), 2 };

            // Month other than february in a common year
            yield return new object[] { s_calendar.ToDateTime(RandomCommonYear(), randomMonthNotFebruary, 28, 10, 30, 12, 0), randomMonthNotFebruary };

            // Any month in the maximum supported year
            yield return new object[] { s_calendar.ToDateTime(s_calendar.MaxSupportedDateTime.Year, randomMonth, 20, 8, 20, 30, 0), randomMonth };

            // Any month in the minimum supported year
            yield return new object[] { s_calendar.ToDateTime(s_calendar.MinSupportedDateTime.Year, randomMonth, 20, 8, 20, 30, 0), randomMonth };

            // Minimum month in minimum year
            yield return new object[] { s_calendar.ToDateTime(s_calendar.MinSupportedDateTime.Year, 1, 20, 8, 20, 30, 0), 1 };

            // Maximum month in maximum year
            yield return new object[] { s_calendar.ToDateTime(s_calendar.MaxSupportedDateTime.Year, 12, 20, 8, 20, 30, 0), 12 };

            // Any month in any year
            yield return new object[] { s_calendar.ToDateTime(RandomYear(), randomMonth, 20, 8, 20, 30, 0), randomMonth };
        }

        [Theory]
        [MemberData(nameof(GetMonth_TestData))]
        public void GetMonth(DateTime time, int expected)
        {
            Assert.Equal(expected, s_calendar.GetMonth(time));
        }
    }
}
