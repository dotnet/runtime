// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarIsLeapYear
    {
        public static IEnumerable<object[]> IsLeapYear_TestData()
        {
            yield return new object[] { TaiwanCalendarUtilities.RandomYear(), 1 };
            yield return new object[] { 2000 - 1911, 1 };
        }

        [Theory]
        [MemberData(nameof(IsLeapYear_TestData))]
        public void IsLeapYear(int year, int era)
        {
            bool expected = new GregorianCalendar().IsLeapYear(year + 1911, era);
            Assert.Equal(expected, new TaiwanCalendar().IsLeapYear(year, era));
        }
    }
}
