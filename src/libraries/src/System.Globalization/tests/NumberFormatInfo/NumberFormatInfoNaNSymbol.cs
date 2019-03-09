// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNaNSymbol
    {
        [Fact]
        public void NaNSymbol_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("NaN", NumberFormatInfo.InvariantInfo.NaNSymbol);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void NaNSymbol_Set_GetReturnsExpected(string newNaNSymbol)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NaNSymbol = newNaNSymbol;
            Assert.Equal(newNaNSymbol, format.NaNSymbol);
        }

        [Fact]
        public void NaNSymbol_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "NaNSymbol", () => format.NaNSymbol = null);
        }

        [Fact]
        public void NaNSymbol_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NaNSymbol = "");
        }
    }
}
