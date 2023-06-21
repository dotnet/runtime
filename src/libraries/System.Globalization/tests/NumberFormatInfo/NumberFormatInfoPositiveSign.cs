// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPositiveSign
    {
        public static IEnumerable<object[]> PositiveSign_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, "+" };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, "+" }; // \u002b
            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                // remaining locales return plus-sign, as "en-US"
                yield return new object[] { CultureInfo.GetCultureInfo("ar-SA").NumberFormat, "\u061c\u002b" };
                yield return new object[] { CultureInfo.GetCultureInfo("fa-IR").NumberFormat, "\u200e\u002b" };
                yield return new object[] { CultureInfo.GetCultureInfo("he-IL").NumberFormat, "\u200e\u002b" };
            }
        }

        [Theory]
        [MemberData(nameof(PositiveSign_TestData))]
        public void PositiveSign_Get_ReturnsExpected(NumberFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.PositiveSign);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void PositiveSign_Set_GetReturnsExpected(string newPositiveSign)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PositiveSign = newPositiveSign;
            Assert.Equal(newPositiveSign, format.PositiveSign);
        }

        [Fact]
        public void PositiveSign_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "PositiveSign", () => format.PositiveSign = null);
        }

        [Fact]
        public void PositiveSign_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PositiveSign = "");
        }
    }
}
