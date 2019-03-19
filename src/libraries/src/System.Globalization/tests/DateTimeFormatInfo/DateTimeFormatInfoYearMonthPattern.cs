// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoYearMonthPattern
    {
        public static IEnumerable<object[]> YearMonthPattern_Get_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "yyyy MMMM" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "MMMM yyyy" };
        }

        [Theory]
        [MemberData(nameof(YearMonthPattern_Get_TestData))]
        public void YearMonthPattern(DateTimeFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.YearMonthPattern);
        }

        public static IEnumerable<object[]> YearMonthPattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "yyyy MMMM" };
            yield return new object[] { "y" };
            yield return new object[] { "Y" };
        }

        [Theory]
        [MemberData(nameof(YearMonthPattern_Set_TestData))]
        public void YearMonthPattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.YearMonthPattern = value;
            Assert.Equal(value, format.YearMonthPattern);
        }

        [Fact]
        public void YearMonthPattern_SetNull_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.YearMonthPattern = null);
        }

        [Fact]
        public void YearMonthPattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.YearMonthPattern = "yyyy MMMM"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
