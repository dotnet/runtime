// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoLongTimePattern
    {
        public void LongTimePattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("HH:mm:ss", DateTimeFormatInfo.InvariantInfo.LongTimePattern);
        }

        public static IEnumerable<object[]> LongTimePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "HH" };
            yield return new object[] { "T" };
            yield return new object[] { "HH:mm:ss dddd, dd MMMM yyyy" };
            yield return new object[] { "HH:mm:ss" };
        }

        [Theory]
        [MemberData(nameof(LongTimePattern_Set_TestData))]
        public void LongTimePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.LongTimePattern = value;
            Assert.Equal(value, format.LongTimePattern);
        }

        [Fact]
        public void LongTimePattern_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.LongTimePattern = null);
        }

        [Fact]
        public void LongTimePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.LongTimePattern = "HH:mm:ss");
        }
    }
}
