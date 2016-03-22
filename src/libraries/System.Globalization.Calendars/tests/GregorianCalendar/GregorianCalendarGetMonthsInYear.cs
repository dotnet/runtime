// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetMonthsInYear
    {
        private const int MonthsInAYear = 12;
        private const int AdEra = 1;
        private const int CurrentEra = 0;
        private readonly RandomDataGenerator s_RandomDataGenerator = new RandomDataGenerator();
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        public static IEnumerable<object[]> GetMonthsInYear_TestData()
        {
            // Leap year
            yield return new object[] { RandomLeapYear(), AdEra };
            yield return new object[] { RandomLeapYear(), CurrentEra };

            // Common year
            yield return new object[] { RandomCommonYear(), AdEra };
            yield return new object[] { RandomCommonYear(), CurrentEra };

            // Any year
            yield return new object[] { RandomYear(), AdEra };
            yield return new object[] { RandomYear(), CurrentEra };

            // Maximum supported year
            yield return new object[] { s_calendar.MaxSupportedDateTime.Year, AdEra };
            yield return new object[] { s_calendar.MaxSupportedDateTime.Year, CurrentEra };

            // Minimum supported year
            yield return new object[] { s_calendar.MinSupportedDateTime.Year, AdEra };
            yield return new object[] { s_calendar.MinSupportedDateTime.Year, CurrentEra };
        }

        [Theory]
        [MemberData(nameof(GetMonthsInYear_TestData))]
        public void GetMonthsInYear(int year, int era)
        {
            Assert.Equal(MonthsInAYear, s_calendar.GetMonthsInYear(year, era));
        }
    }
}
