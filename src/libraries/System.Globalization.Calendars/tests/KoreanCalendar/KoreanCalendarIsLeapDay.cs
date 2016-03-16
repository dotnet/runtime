// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarIsLeapDay
    {
        public static IEnumerable<object[]> IsLeapDay_TestData()
        {
            yield return new object[] { 1, 1, 1, 1 };
            yield return new object[] { 9999, 12, 31, 1 };
            yield return new object[] { 1900, 2, 28, 1 };
            yield return new object[] { 1200, 2, 29, 1 };
        }

        [Theory]
        [MemberData(nameof(IsLeapDay_TestData))]
        public void IsLeapDay(int year, int month, int day, int era)
        {
            bool expected = new GregorianCalendar().IsLeapDay(year, month, day, era);
            Assert.Equal(expected, new KoreanCalendar().IsLeapDay(year + 2333, month, day, era));
        }
    }
}
