// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentSymbol
    {
        public static IEnumerable<object[]> PercentSymbol_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, "%" };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, "%" };
            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            { // remaining WASM locales have the same symbol as "en-US"
                yield return new object[] { CultureInfo.GetCultureInfo("ar-SA").NumberFormat, "\u066A\u061C" };
                yield return new object[] { CultureInfo.GetCultureInfo("fa-IR").NumberFormat, "\u066A" };
            }
        }

        [Theory]
        [MemberData(nameof(PercentSymbol_TestData))]
        public void PercentSymbol_Get_ReturnsExpected(NumberFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.PercentSymbol);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void PercentSymbol_Set_GetReturnsExpected(string newPercentSymbol)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentSymbol = newPercentSymbol;
            Assert.Equal(newPercentSymbol, format.PercentSymbol);
        }

        [Fact]
        public void PercentSymbol_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "PercentSymbol", () => format.PercentSymbol = null);
        }

        [Fact]
        public void PercentSymbol_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentSymbol = "");
        }
    }
}
