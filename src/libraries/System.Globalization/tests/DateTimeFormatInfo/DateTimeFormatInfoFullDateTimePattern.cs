// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoFullDateTimePattern
    {
        [Fact]
        public void FullDateTimePattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("dddd, dd MMMM yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo.FullDateTimePattern);
        }

        public static IEnumerable<object[]> FullDateTimePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "dddd" };
            yield return new object[] { "F" };
            yield return new object[] { "HH:mm:ss dddd, dd MMMM yyyy" };
        }

        [Theory]
        [MemberData(nameof(FullDateTimePattern_Set_TestData))]
        public void FullDateTimePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.FullDateTimePattern = value;
            Assert.Equal(value, format.FullDateTimePattern);
        }

        [Fact]
        public void FullDateTimePattern_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.FullDateTimePattern = null);
        }

        [Fact]
        public void FullDateTimePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.FullDateTimePattern = "dddd, dd MMMM yyyy HH:mm:ss");
        }
    }
}
