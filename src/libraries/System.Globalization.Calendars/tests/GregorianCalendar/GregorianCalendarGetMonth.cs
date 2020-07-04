// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetMonth
    {
        public static IEnumerable<object[]> GetMonth_TestData()
        {
            // February in a leap year
            yield return new object[] { new DateTime(RandomLeapYear(), 2, 29, 10, 30, 12) };

            // Month other than february in a leap year
            yield return new object[] { new DateTime(RandomLeapYear(), RandomMonthNotFebruary(), 28, 10, 30, 12) };

            // February in a common year
            yield return new object[] { new DateTime(RandomCommonYear(), 2, 28, 10, 30, 12) };

            // Month other than february in a common year
            yield return new object[] { new DateTime(RandomCommonYear(), RandomMonthNotFebruary(), 28, 10, 30, 12) };

            // Any month in the maximum supported year
            yield return new object[] { new DateTime(9999, RandomMonth(), 20, 8, 20, 30) };

            // Any month in the minimum supported year
            yield return new object[] { new DateTime(1, RandomMonth(), 20, 8, 20, 30) };

            // Minimum month in minimum year
            yield return new object[] { new DateTime(1, 1, 20, 8, 20, 30) };

            // Maximum month in maximum year
            yield return new object[] { new DateTime(9999, 12, 20, 8, 20, 30) };

            // Any month in any year
            yield return new object[] { new DateTime(RandomYear(), RandomMonth(), 20, 8, 20, 30) };
        }

        [Theory]
        [MemberData(nameof(GetMonth_TestData))]
        public void GetMonth(DateTime time)
        {
            Assert.Equal(time.Month, new GregorianCalendar().GetMonth(time));
        }
    }
}
