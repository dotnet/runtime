// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            AssertExtensions.Throws<ArgumentNullException>("CurrencyDecimalSeparator", () => new NumberFormatInfo().CurrencyDecimalSeparator = null);
            AssertExtensions.Throws<ArgumentException>(null, () => new NumberFormatInfo().CurrencyDecimalSeparator = "");
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencyDecimalSeparator = "string");
        }
    }
}
