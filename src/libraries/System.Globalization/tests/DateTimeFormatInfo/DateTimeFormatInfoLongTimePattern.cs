// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoLongTimePattern
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public void LongTimePattern_InvariantInfo()
        {
            Assert.Equal("HH:mm:ss", DateTimeFormatInfo.InvariantInfo.LongTimePattern);
        }

        public static IEnumerable<object[]> LongTimePattern_TestData()
        {
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "HH" };
            yield return new object[] { "T" };
            yield return new object[] { "HH:mm:ss dddd, dd MMMM yyyy" };
            yield return new object[] { s_randomDataGenerator.GetString(-55, false, 1, 256) };
            yield return new object[] { "HH:mm:ss" };
        }

        [Theory]
        [MemberData(nameof(LongTimePattern_TestData))]
        public void LongTimePattern_Set(string newLongTimePattern)
        {
            var format = new DateTimeFormatInfo();
            format.LongTimePattern = newLongTimePattern;
            Assert.Equal(newLongTimePattern, format.LongTimePattern);
        }

        [Fact]
        public void LongTimePattern_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().LongTimePattern = null); // Value is null
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.LongTimePattern = "HH:mm:ss"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
