// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarIsLeapDay
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        public static IEnumerable<object[]> IsLeapDay_TestData()
        {
            int randomYear = RandomYear();
            int randomMonth = RandomMonth();
            int randomDay =  RandomDay(randomYear, randomMonth);

            // 29th February on a leap year
            yield return new object[] { RandomLeapYear(), 2, 29, true };

            // 28th February on a common year
            yield return new object[] { RandomCommonYear(), 2, 28, false };

            // Any day, any month, any day
            bool randomExpected = IsLeapYear(randomYear) && randomMonth == 2 && randomDay == 29;
            yield return new object[] { randomYear, randomMonth, randomDay, randomExpected };

            // Any day, any month in the maximum supported year
            int maxYear = s_calendar.MaxSupportedDateTime.Year;
            int maxRandomDay = RandomDay(maxYear, randomMonth);
            bool maxExpected = IsLeapYear(maxYear) && randomMonth == 2 && maxRandomDay == 29;
            yield return new object[] { maxYear, randomMonth, maxRandomDay, maxExpected };

            // Any day, any month in the minimum supported year
            int minYear = s_calendar.MaxSupportedDateTime.Year;
            int minRandomDay = RandomDay(minYear, randomMonth);
            bool minExpected = IsLeapYear(minYear) && randomMonth == 2 && minRandomDay == 29;
            yield return new object[] { minYear, randomMonth, minRandomDay, minExpected };
        }

        [Theory]
        [MemberData(nameof(IsLeapDay_TestData))]
        public void IsLeapDay(int year, int month, int day, bool expected)
        {
            Assert.Equal(expected, s_calendar.IsLeapDay(year, month, day, 1));
        }
    }
}
