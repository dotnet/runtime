// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoYearMonthPattern
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> YearMonthPattern_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "yyyy MMMM" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "MMMM yyyy" };
        }

        [Theory]
        [MemberData(nameof(YearMonthPattern_TestData))]
        public void YearMonthPattern(DateTimeFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.YearMonthPattern);
        }

        public static IEnumerable<object[]> YearMonthPattern_Set_TestData()
        {
            yield return new object[] { "yyyy MMMM" };
            yield return new object[] { "y" };
            yield return new object[] { "Y" };
            yield return new object[] { s_randomDataGenerator.GetString(-55, false, 1, 256) };
        }

        [Theory]
        [MemberData(nameof(YearMonthPattern_Set_TestData))]
        public void YearMonthPattern_Set(string newYearMonthPattern)
        {
            var format = new DateTimeFormatInfo();
            format.YearMonthPattern = newYearMonthPattern;
            Assert.Equal(newYearMonthPattern, format.YearMonthPattern);
        }

        [Fact]
        public void YearMonthPattern_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().YearMonthPattern = null); // Value is null
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.YearMonthPattern = "yyyy MMMM"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
