// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortTimePattern
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public void ShortTimePattern_InvariantInfo()
        {
            Assert.Equal("HH:mm", DateTimeFormatInfo.InvariantInfo.ShortTimePattern);
        }

        public static IEnumerable<object[]> ShortTimePattern_TestData()
        {
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "HH:mm" };
            yield return new object[] { "t" };
            yield return new object[] { s_randomDataGenerator.GetString(-55, false, 1, 256) };
        }

        [Theory]
        [MemberData(nameof(ShortTimePattern_TestData))]
        public void ShortTimePattern_Set(string newShortTimePattern)
        {
            var format = new DateTimeFormatInfo();
            format.ShortTimePattern = newShortTimePattern;
            Assert.Equal(newShortTimePattern, format.ShortTimePattern);
        }

        [Fact]
        public void ShortTimePattern_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().ShortTimePattern = null); // Value is null
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortTimePattern = "HH:mm"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
