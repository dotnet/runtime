// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortDatePattern
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public void ShortDatePattern_InvariantInfo()
        {
            Assert.Equal("MM/dd/yyyy", DateTimeFormatInfo.InvariantInfo.ShortDatePattern);
        }

        public static IEnumerable<object[]> ShortDatePattern_TestData()
        {
            yield return new object[] { "MM/dd/yyyy" };
            yield return new object[] { "MM-DD-yyyy" };
            yield return new object[] { "d" };
            yield return new object[] { s_randomDataGenerator.GetString(-55, false, 1, 256) };
        }

        [Theory]
        [MemberData(nameof(ShortDatePattern_TestData))]
        public void ShortDatePattern_Set(string newShortDatePattern)
        {
            var format = new DateTimeFormatInfo();
            format.ShortDatePattern = newShortDatePattern;
            Assert.Equal(newShortDatePattern, format.ShortDatePattern);
        }

        [Fact]
        public void ShortDatePattern_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().ShortDatePattern = null); // Value is null
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortDatePattern = "MM/dd/yyyy"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
