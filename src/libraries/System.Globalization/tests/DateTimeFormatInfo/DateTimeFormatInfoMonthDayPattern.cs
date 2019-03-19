// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMonthDayPattern
    {
        public void LongDayPattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("MMMM dd", DateTimeFormatInfo.InvariantInfo.MonthDayPattern);
        }

        public static IEnumerable<object[]> MonthDayPattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "MMMM" };
            yield return new object[] { "MMM dd" };
            yield return new object[] { "M" };
            yield return new object[] { "dd MMMM" };
            yield return new object[] { "MMMM dd" };
            yield return new object[] { "m" };
        }

        [Theory]
        [MemberData(nameof(MonthDayPattern_Set_TestData))]
        public void MonthDayPattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.MonthDayPattern = value;
            Assert.Equal(value, format.MonthDayPattern);
        }

        [Fact]
        public void MonthDayPattern_SetNull_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.MonthDayPattern = null);
        }

        [Fact]
        public void MonthDayPattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.MonthDayPattern = "MMMM dd");
        }
    }
}
