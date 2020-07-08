// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetDaysInYear
    {
        public static IEnumerable<object[]> GetDaysInYear_TestData()
        {
            // Random years
            yield return new object[] { RandomLeapYear() };
            yield return new object[] { RandomCommonYear() };
            yield return new object[] { RandomYear() };
            yield return new object[] { 100 };
            yield return new object[] { 400 };

            // Boundary years
            yield return new object[] { 9999  };
            yield return new object[] { 1 };
        }

        [Theory]
        [MemberData(nameof(GetDaysInYear_TestData))]
        public void GetDaysInYear(int year)
        {
            GregorianCalendar calendar = new GregorianCalendar();
            int expected = IsLeapYear(year) ? 366 : 365;
            Assert.Equal(expected, calendar.GetDaysInYear(year));
            Assert.Equal(expected, calendar.GetDaysInYear(year, 0));
            Assert.Equal(expected, calendar.GetDaysInYear(year, 1));
        }
    }
}
