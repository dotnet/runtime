// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetEra
    {
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        private const int AdEra = 1;

        public static IEnumerable<object[]> GetEra_TestData()
        {
            int randomMonth = RandomMonth();
            int randomMonthNotFebruary = RandomMonthNotFebruary();

            // February in a leap year
            yield return new object[] { s_calendar.ToDateTime(RandomLeapYear(), 2, 29, 10, 30, 12, 0) };

            // Month other than February in a leap year
            yield return new object[] { s_calendar.ToDateTime(RandomLeapYear(), randomMonthNotFebruary, 28, 10, 30, 20, 0) };

            // February in a common year
            yield return new object[] { s_calendar.ToDateTime(RandomCommonYear(), 2, 28, 10, 20, 30, 0) };

            // Month other than February in a common year
            yield return new object[] { s_calendar.ToDateTime(RandomCommonYear(), randomMonthNotFebruary, 28, 10, 20, 30, 0) };

            // Any month in the maximum supported year
            yield return new object[] { s_calendar.ToDateTime(s_calendar.MaxSupportedDateTime.Year, randomMonth, 20, 8, 20, 30, 0) };

            // Any month in the minimum supported year
            yield return new object[] { s_calendar.ToDateTime(s_calendar.MinSupportedDateTime.Year, randomMonth, 20, 8, 20, 30, 0) };

            // Maximum month in the maximum supported year
            yield return new object[] { s_calendar.ToDateTime(s_calendar.MaxSupportedDateTime.Year, 12, 20, 8, 20, 30, 0) };

            // Minimum month in the minimum supported year
            yield return new object[] { s_calendar.ToDateTime(s_calendar.MinSupportedDateTime.Year, 1, 20, 8, 20, 30, 0) };

            // Any month in any year
            yield return new object[] { s_calendar.ToDateTime(RandomYear(), randomMonth, 20, 8, 20, 30, 0) };
        }

        [Theory]
        [MemberData(nameof(GetEra_TestData))]
        public void GetEra(DateTime time)
        {
            Assert.Equal(AdEra, s_calendar.GetEra(time));
        }
    }
}
