// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoFullDateTimePattern
    {
        private static readonly RandomDataGenerator s_generator = new RandomDataGenerator();

        [Fact]
        public void FullDateTimePattern_InvariantInfo()
        {
            Assert.Equal("dddd, dd MMMM yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo.FullDateTimePattern);
        }

        public static IEnumerable<object[]> FullDFullDateTimePattern_Set_TestData()
        {
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "dddd" };
            yield return new object[] { "F" };
            yield return new object[] { "HH:mm:ss dddd, dd MMMM yyyy" };
            yield return new object[] { s_generator.GetString(-55, false, 1, 256) };
        }

        [Theory]
        [MemberData(nameof(FullDFullDateTimePattern_Set_TestData))]
        public void FullDateTimePattern_Set(string newFullDateTimePattern)
        {
            var format = new DateTimeFormatInfo();
            format.FullDateTimePattern = newFullDateTimePattern;
            Assert.Equal(newFullDateTimePattern, format.FullDateTimePattern);
        }

        [Fact]
        public void FullDateTimePattern_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().FullDateTimePattern = null); // Value is null
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.FullDateTimePattern = "dddd, dd MMMM yyyy HH:mm:ss"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
