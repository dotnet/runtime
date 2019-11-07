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
            yield return new object[] { 1912 };
            yield return new object[] { 8088 };
            yield return new object[] { 2000 };
            yield return new object[] { TaiwanCalendarUtilities.RandomYear() };
        }

        [Theory]
        [MemberData(nameof(GetDaysInYear_TestData))]
        public void GetDaysInYear(int year)
        {
            TaiwanCalendar calendar = new TaiwanCalendar();
            int expected = calendar.IsLeapYear(year) ? 366 : 365;
            Assert.Equal(expected, calendar.GetDaysInYear(year));
            Assert.Equal(expected, calendar.GetDaysInYear(year, 0));
            Assert.Equal(expected, calendar.GetDaysInYear(year, 1));
        }
    }
}
