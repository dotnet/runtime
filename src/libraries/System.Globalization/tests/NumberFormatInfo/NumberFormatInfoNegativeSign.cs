// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNegativeSign
    {
        public static IEnumerable<object[]> NegativeSign_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, "-" };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, "-" }; // \u002d
            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                // remaining locales return hyphen-minus, as "en-US"
                yield return new object[] { CultureInfo.GetCultureInfo("ar-SA").NumberFormat, "\u061c\u002d" };
                yield return new object[] { CultureInfo.GetCultureInfo("et-EE").NumberFormat, "\u2212" };
                yield return new object[] { CultureInfo.GetCultureInfo("fa").NumberFormat, "\u2212" };
                yield return new object[] { CultureInfo.GetCultureInfo("fa-IR").NumberFormat, "\u200e\u2212" };
                yield return new object[] { CultureInfo.GetCultureInfo("fi-FI").NumberFormat, "\u2212" };
                yield return new object[] { CultureInfo.GetCultureInfo("he-IL").NumberFormat, "\u200e\u002d" };
                yield return new object[] { CultureInfo.GetCultureInfo("lt-LT").NumberFormat, "\u2212" };
                yield return new object[] { CultureInfo.GetCultureInfo("nb-NO").NumberFormat, "\u2212" };
                yield return new object[] { CultureInfo.GetCultureInfo("no").NumberFormat, "\u2212" };
                yield return new object[] { CultureInfo.GetCultureInfo("no-NO").NumberFormat, "\u2212" };
                yield return new object[] { CultureInfo.GetCultureInfo("sl-SI").NumberFormat, "\u2212" };
                yield return new object[] { CultureInfo.GetCultureInfo("sv-AX").NumberFormat, "\u2212" };
                yield return new object[] { CultureInfo.GetCultureInfo("sv-SE").NumberFormat, "\u2212" };
            }
        }

        [Theory]
        [MemberData(nameof(NegativeSign_TestData))]
        public void NegativeSign_Get_ReturnsExpected(NumberFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.NegativeSign);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void NegativeSign_Set_GetReturnsExpected(string newNegativeSign)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NegativeSign = newNegativeSign;
            Assert.Equal(newNegativeSign, format.NegativeSign);
        }

        [Fact]
        public void NegativeSign_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "NegativeSign", () => format.NegativeSign = null);
        }

        [Fact]
        public void NegativeSign_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NegativeSign = "");
        }
    }
}
