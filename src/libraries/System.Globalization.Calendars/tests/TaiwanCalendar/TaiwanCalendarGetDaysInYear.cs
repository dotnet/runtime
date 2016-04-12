// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarGetDaysInYear
    {
        public static IEnumerable<object[]> GetDaysInYear_TestData()
        {
            yield return new object[] { 1912, 1 };
            yield return new object[] { 8088, 1 };
            yield return new object[] { 2000, 1 };
            yield return new object[] { TaiwanCalendarUtilities.RandomYear(), 1 };
        }

        [Theory]
        [MemberData(nameof(GetDaysInYear_TestData))]
        public void GetDaysInYear(int year, int era)
        {
            TaiwanCalendar calendar = new TaiwanCalendar();
            int expected;
            if (calendar.IsLeapYear(year, era))
            {
                expected = 366;
            }
            else
            {
                expected = 365;
            }
            Assert.Equal(expected, new TaiwanCalendar().GetDaysInYear(year, era));
        }
    }
}
