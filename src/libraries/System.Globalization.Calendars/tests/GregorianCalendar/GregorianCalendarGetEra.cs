// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetEra
    {
        public static IEnumerable<object[]> GetEra_TestData()
        {
            // February in a leap year
            yield return new object[] { new DateTime(RandomLeapYear(), 2, 29, 10, 30, 12) };

            // Month other than February in a leap year
            yield return new object[] { new DateTime(RandomLeapYear(), RandomMonthNotFebruary(), 28, 10, 30, 20) };

            // February in a common year
            yield return new object[] { new DateTime(RandomCommonYear(), 2, 28, 10, 20, 30) };

            // Month other than February in a common year
            yield return new object[] { new DateTime(RandomCommonYear(), RandomMonthNotFebruary(), 28, 10, 20, 30) };

            // Any month in the maximum supported year
            yield return new object[] { new DateTime(9999, RandomMonth(), 20, 8, 20, 30) };

            // Any month in the minimum supported year
            yield return new object[] { new DateTime(1, RandomMonth(), 20, 8, 20, 30) };

            // Maximum month in the maximum supported year
            yield return new object[] { new DateTime(9999, 12, 20, 8, 20, 30) };

            // Minimum month in the minimum supported year
            yield return new object[] { new DateTime(1, 1, 20, 8, 20, 30) };

            // Any month in any year
            yield return new object[] { new DateTime(RandomYear(), RandomMonth(), 20, 8, 20, 30) };
        }

        [Theory]
        [MemberData(nameof(GetEra_TestData))]
        public void GetEra(DateTime time)
        {
            Assert.Equal(1, new GregorianCalendar().GetEra(time));
        }
    }
}
