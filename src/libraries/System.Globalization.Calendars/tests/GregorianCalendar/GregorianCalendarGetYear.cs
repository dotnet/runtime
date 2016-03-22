// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetYear
    {
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        public static IEnumerable<object[]> GetYear_TestData()
        {
            // February in a leap year
            int randomLeapYearFebruary = RandomLeapYear();
            yield return new object[] { s_calendar.ToDateTime(randomLeapYearFebruary, 2, 29, 10, 30, 12, 0), randomLeapYearFebruary };

            // Any month other than February in a leap year
            int randomLeapYearNotFebruary = RandomLeapYear();
            yield return new object[] { s_calendar.ToDateTime(randomLeapYearNotFebruary, RandomMonthNotFebruary(), 28, 10, 30, 12, 0), randomLeapYearNotFebruary };

            // February in a common year
            int randomCommonYearFebruary = RandomCommonYear();
            yield return new object[] { s_calendar.ToDateTime(randomCommonYearFebruary, 2, 28, 10, 30, 12, 0), randomCommonYearFebruary };

            // Any month other than February in a common year
            int randomCommonYearNotFebruary = RandomCommonYear();
            yield return new object[] { s_calendar.ToDateTime(randomCommonYearNotFebruary, RandomMonthNotFebruary(), 28, 10, 30, 12, 0), randomCommonYearNotFebruary };
            
            // Any month in the minimum supported year
            int minYear = s_calendar.MinSupportedDateTime.Year;
            yield return new object[] { s_calendar.ToDateTime(minYear, RandomMonth(), 20, 8, 20, 30, 0), minYear };

            // Any month in the minimum supported year
            int maxYear = s_calendar.MaxSupportedDateTime.Year;
            yield return new object[] { s_calendar.ToDateTime(maxYear, RandomMonth(), 20, 8, 20, 30, 0), maxYear };

            // Minimum month in any year
            int randomMinMonthYear = RandomYear();
            yield return new object[] { s_calendar.ToDateTime(randomMinMonthYear, 1, 20, 8, 20, 30, 0), randomMinMonthYear };

            // Maximum month in any year
            int randomMaxMonthYear = RandomYear();
            yield return new object[] { s_calendar.ToDateTime(randomMaxMonthYear, 12, 20, 8, 20, 30, 0), randomMaxMonthYear };

            // Any month in any year
            int randomYear = RandomYear();
            yield return new object[] { s_calendar.ToDateTime(randomYear, RandomMonth(), 20, 8, 20, 30, 0), randomYear };
        }

        [Theory]
        [MemberData(nameof(GetYear_TestData))]
        public void GetYear(DateTime time, int expected)
        {
            Assert.Equal(expected, s_calendar.GetYear(time));
        }
    }
}
