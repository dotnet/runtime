// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarIsLeapMonth
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> IsLeapMonth_TestData()
        {
            yield return new object[] { 1, 1 };
            yield return new object[] { 9999, 12 };
            yield return new object[] { 2000, 2 };
            yield return new object[] { s_randomDataGenerator.GetInt16(-55) % 9999, s_randomDataGenerator.GetInt16(-55) % 12 + 1 };
        }

        [Theory]
        [MemberData(nameof(IsLeapMonth_TestData))]
        public void IsLeapMonth(int year, int month)
        {
            ThaiBuddhistCalendar calendar = new ThaiBuddhistCalendar();
            bool expected = new GregorianCalendar().IsLeapMonth(year, month);
            Assert.Equal(expected, calendar.IsLeapMonth(year + 543, month));
            Assert.Equal(expected, calendar.IsLeapMonth(year + 543, month, 0));
            Assert.Equal(expected, calendar.IsLeapMonth(year + 543, month, 1));
        }
    }
}
