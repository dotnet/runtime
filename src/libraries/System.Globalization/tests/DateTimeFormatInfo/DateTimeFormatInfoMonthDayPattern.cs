// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMonthDayPattern
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public void LongDayPattern_InvariantInfo()
        {
            Assert.Equal("MMMM dd", DateTimeFormatInfo.InvariantInfo.MonthDayPattern);
        }

        public static IEnumerable<object[]> MonthDayPattern_TestData()
        {
            yield return new object[] { "MMMM" };
            yield return new object[] { "MMM dd" };
            yield return new object[] { "M" };
            yield return new object[] { "dd MMMM" };
            yield return new object[] { s_randomDataGenerator.GetString(-55, false, 1, 256) };
            yield return new object[] { "MMMM dd" };
            yield return new object[] { "m" };
        }

        [Theory]
        [MemberData(nameof(MonthDayPattern_TestData))]
        public void MonthDayPattern_Set(string newMonthDayPattern)
        {
            var format = new DateTimeFormatInfo();
            format.MonthDayPattern = newMonthDayPattern;
            Assert.Equal(newMonthDayPattern, format.MonthDayPattern);
        }

        [Fact]
        public void MonthDayPattern_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().MonthDayPattern = null); // Value is null
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.MonthDayPattern = "MMMM dd"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
