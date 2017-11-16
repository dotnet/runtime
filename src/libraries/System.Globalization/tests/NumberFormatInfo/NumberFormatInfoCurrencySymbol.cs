// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencySymbol
    {
        [Theory]
        [InlineData("en-US", "$")]
        [InlineData("en-GB", "\x00a3")] // pound
        [InlineData("", "\x00a4")] // international
        public void CurrencySymbol_Get(string name, string expected)
        {
            Assert.Equal(expected, CultureInfo.GetCultureInfo(name).NumberFormat.CurrencySymbol);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void CurrencySymbol_Set(string newCurrencySymbol)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencySymbol = newCurrencySymbol;
            Assert.Equal(newCurrencySymbol, format.CurrencySymbol);
        }

        [Fact]
        public void CurrencySymbol_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("CurrencySymbol", () => new NumberFormatInfo().CurrencySymbol = null);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencySymbol = "");
        }
    }
}
