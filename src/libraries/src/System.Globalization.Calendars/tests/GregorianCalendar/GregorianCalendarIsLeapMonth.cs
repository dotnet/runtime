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
        public static IEnumerable<object[]> IsLeapMonth_TestData()
        {
            // February in a leap year
            yield return new object[] { RandomLeapYear(), 2 };

            // February in a common year
            yield return new object[] { RandomCommonYear(), 2 };

            // Any month, any year
            yield return new object[] { RandomYear(), RandomMonth() };

            // Any month in the maximum supported year
            yield return new object[] { 9999, RandomMonth() };

            // Any month in the minimum supported year
            yield return new object[] { 1, RandomMonth() };
        }

        [Theory]
        [MemberData(nameof(IsLeapMonth_TestData))]
        public void IsLeapMonth(int year, int month)
        {
            GregorianCalendar calendar = new GregorianCalendar();
            Assert.False(calendar.IsLeapMonth(year, month));
            Assert.False(calendar.IsLeapMonth(year, month, 0));
            Assert.False(calendar.IsLeapMonth(year, month, 1));
        }
    }
}
