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
        public static IEnumerable<object[]> GetMonthsInYear_TestData()
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
        [MemberData(nameof(GetMonthsInYear_TestData))]
        public void GetMonthsInYear(int year)
        {
            Assert.Equal(12, new GregorianCalendar().GetMonthsInYear(year, 1));
            Assert.Equal(12, new GregorianCalendar().GetMonthsInYear(year, 0));
        }
    }
}
