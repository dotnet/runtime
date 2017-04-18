// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoLongDatePattern
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public void LongDatePattern_InvariantInfo()
        {
            Assert.Equal("dddd, dd MMMM yyyy", DateTimeFormatInfo.InvariantInfo.LongDatePattern);
        }

        public static IEnumerable<object[]> LongDatePattern_TestData()
        {
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "dddd" };
            yield return new object[] { "D" };
            yield return new object[] { "HH:mm:ss dddd, dd MMMM yyyy" };
            yield return new object[] { s_randomDataGenerator.GetString(-55, false, 1, 256) };
            yield return new object[] { "dddd, dd MMMM yyyy" };
        }

        [Theory]
        [MemberData(nameof(LongDatePattern_TestData))]
        public void LongDatePattern_Set(string newLongDatePattern)
        {
            var format = new DateTimeFormatInfo();
            format.LongDatePattern = newLongDatePattern;
            Assert.Equal(newLongDatePattern, format.LongDatePattern);
        }

        [Fact]
        public void LongDatePattern_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().LongDatePattern = null); // Value is null
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.LongDatePattern = "dddd, dd MMMM yyyy"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
