// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarGetMonthsInYear
    {
        public static IEnumerable<object[]> GetMonthsInYear_TestData()
        {
            yield return new object[] { 1912 };
            yield return new object[] { 8088 };
            yield return new object[] { TaiwanCalendarUtilities.RandomYear() };
        }

        [Theory]
        [MemberData(nameof(GetMonthsInYear_TestData))]
        public void GetMonthsInYear(int year)
        {
            TaiwanCalendar calendar = new TaiwanCalendar();
            Assert.Equal(12, calendar.GetMonthsInYear(year));
            Assert.Equal(12, calendar.GetMonthsInYear(year, 0));
            Assert.Equal(12, calendar.GetMonthsInYear(year, 1));
        }
    }
}
