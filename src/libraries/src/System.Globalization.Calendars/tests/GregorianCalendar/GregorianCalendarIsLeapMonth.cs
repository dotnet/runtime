// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarIsLeapMonth
    {
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        public static IEnumerable<object[]> IsLeapMonth_TestData()
        {
            // February in a leap year
            yield return new object[] { RandomLeapYear(), 2, false };

            // February in a common year
            yield return new object[] { RandomCommonYear(), 2, false };

            // Any month, any year
            yield return new object[] { RandomYear(), RandomMonth(), false };

            // Any month in the maximum supported year
            yield return new object[] { s_calendar.MaxSupportedDateTime.Year, RandomMonth(), false };

            // Any month in the minimum supported year
            yield return new object[] { s_calendar.MinSupportedDateTime.Year, RandomMonth(), false };
        }

        [Theory]
        [MemberData(nameof(IsLeapMonth_TestData))]
        public void IsLeapMonth(int year, int month, bool expected)
        {
            Assert.Equal(expected, s_calendar.IsLeapMonth(year, month, 1));
        }
    }
}
