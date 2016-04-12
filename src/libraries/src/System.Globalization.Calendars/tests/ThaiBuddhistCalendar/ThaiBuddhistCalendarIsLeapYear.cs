// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarIsLeapYear
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> IsLeapYear_TestData()
        {
            yield return new object[] { 1, 1 };
            yield return new object[] { 9999, 1 };
            yield return new object[] { 2000, 1 };
            yield return new object[] { s_randomDataGenerator.GetInt16(-55) % 9999, 1 };
        }

        [Theory]
        [MemberData(nameof(IsLeapYear_TestData))]
        public void IsLeapYear(int year, int era)
        {
            bool expected = new GregorianCalendar().IsLeapYear(year, era);
            Assert.Equal(expected, new ThaiBuddhistCalendar().IsLeapYear(year + 543, era));
        }
    }
}
