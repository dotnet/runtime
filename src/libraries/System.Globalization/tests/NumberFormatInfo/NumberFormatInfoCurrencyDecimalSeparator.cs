// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyDecimalSeparator
    {
        [Fact]
        public void CurrencyDecimalSeparator_Get_InvariantInfo()
        {
            Assert.Equal(".", NumberFormatInfo.InvariantInfo.CurrencyDecimalSeparator);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("    ")]
        public void CurrencyDecimalSeparator_Set(string newCurrencyDecimalSeparator)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencyDecimalSeparator = newCurrencyDecimalSeparator;
            Assert.Equal(newCurrencyDecimalSeparator, format.CurrencyDecimalSeparator);
        }

        [Fact]
        public void CurrencyDecimalSeparator_Set_Invalid()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "CurrencyDecimalSeparator", () => format.CurrencyDecimalSeparator = null);
        }

        [Fact]
        public void CurrencyDecimalSeparator_SetEmpty_ThrowsArgumentException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", null, () => format.CurrencyDecimalSeparator = "");
        }

        [Fact]
        public void CurrencyDecimalSeparator_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencyDecimalSeparator = "string");
        }
    }
}
