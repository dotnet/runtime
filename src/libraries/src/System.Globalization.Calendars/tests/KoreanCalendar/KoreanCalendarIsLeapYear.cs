// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarIsLeapYear
    {
        public static IEnumerable<object[]> IsLeapYear_TestData()
        {
            yield return new object[] { 1, 1 };
            yield return new object[] { 9999, 1 };
            yield return new object[] { 1900, 1 };
            yield return new object[] { 1200, 1 };
        }

        [Theory]
        [MemberData(nameof(IsLeapYear_TestData))]
        public void IsLeapYear(int year, int era)
        {
            bool expected = new GregorianCalendar().IsLeapYear(year, era);
            Assert.Equal(expected, new KoreanCalendar().IsLeapYear(year + 2333, era));
        }
    }
}
