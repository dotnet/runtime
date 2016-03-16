// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarGetMonthsInYear
    {
        public static IEnumerable<object[]> GetMonthsInYear_TestData()
        {
            yield return new object[] { 1912, 1 };
            yield return new object[] { 8088, 1 };
            yield return new object[] { TaiwanCalendarUtilities.RandomYear(), 1 };
        }

        [Theory]
        [MemberData(nameof(GetMonthsInYear_TestData))]
        public void GetMonthsInYear(int year, int era)
        {
            Assert.Equal(12, new TaiwanCalendar().GetMonthsInYear(year, era));
        }
    }
}
