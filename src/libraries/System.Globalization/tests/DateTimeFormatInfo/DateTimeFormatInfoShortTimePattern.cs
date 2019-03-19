// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortTimePattern
    {
        public void ShortTimePattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("HH:mm", DateTimeFormatInfo.InvariantInfo.ShortTimePattern);
        }

        public static IEnumerable<object[]> ShortTimePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "HH:mm" };
            yield return new object[] { "t" };
        }

        [Theory]
        [MemberData(nameof(ShortTimePattern_Set_TestData))]
        public void ShortTimePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.ShortTimePattern = value;
            Assert.Equal(value, format.ShortTimePattern);
        }

        [Fact]
        public void ShortTimePattern_SetNull_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.ShortTimePattern = null);
        }

        [Fact]
        public void ShortTimePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortTimePattern = "HH:mm");
        }
    }
}
