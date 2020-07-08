// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarIsLeapYear
    {
        public static IEnumerable<object[]> IsLeapYear_TestData()
        {
            // Random years
            yield return new object[] { RandomLeapYear() };
            yield return new object[] { RandomCommonYear() };
            yield return new object[] { RandomYear() };

            // Boundary years
            yield return new object[] { 9999 };
            yield return new object[] { 1 };
        }

        [Theory]
        [MemberData(nameof(IsLeapYear_TestData))]
        public void IsLeapYearTest(int year)
        {
            GregorianCalendar calendar = new GregorianCalendar();
            bool expected = IsLeapYear(year);
            Assert.Equal(expected, calendar.IsLeapYear(year));
            Assert.Equal(expected, calendar.IsLeapYear(year, 0));
            Assert.Equal(expected, calendar.IsLeapYear(year, 1));
        }
    }
}
