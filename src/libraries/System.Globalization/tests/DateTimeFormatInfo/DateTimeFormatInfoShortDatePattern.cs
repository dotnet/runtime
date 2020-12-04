// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortDatePattern
    {
        [Fact]
        public void ShortDatePattern_InvariantInfo()
        {
            Assert.Equal("MM/dd/yyyy", DateTimeFormatInfo.InvariantInfo.ShortDatePattern);
        }

        public static IEnumerable<object[]> ShortDatePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "MM/dd/yyyy" };
            yield return new object[] { "MM-DD-yyyy" };
            yield return new object[] { "d" };
        }

        [Theory]
        [MemberData(nameof(ShortDatePattern_Set_TestData))]
        public void ShortDatePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.ShortDatePattern = value;
            Assert.Equal(value, format.ShortDatePattern);
        }

        [Fact]
        public void ShortDatePattern_Set_InvalidatesDerivedPatterns()
        {
            const string Pattern = "#$";
            var format = new DateTimeFormatInfo();
            var d = DateTimeOffset.Now;
            d.ToString("G", format); // GeneralLongTimePattern
            d.ToString("g", format); // GeneralShortTimePattern
            d.ToString(format); // DateTimeOffsetPattern
            format.ShortDatePattern = Pattern;
            Assert.Contains(Pattern, d.ToString("G", format));
            Assert.Contains(Pattern, d.ToString("g", format));
            Assert.Contains(Pattern, d.ToString(format));
        }

        [Fact]
        public void ShortDatePattern_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.ShortDatePattern = null);
        }

        [Fact]
        public void ShortDatePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortDatePattern = "MM/dd/yyyy");
        }
    }
}
