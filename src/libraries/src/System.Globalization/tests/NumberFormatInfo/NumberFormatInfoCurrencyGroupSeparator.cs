// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyGroupSeparator
    {
        [Fact]
        public void CurrencyGroupSeparator_Get_InvariantInfo()
        {
            Assert.Equal(",", NumberFormatInfo.InvariantInfo.CurrencyGroupSeparator);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("    ")]
        [InlineData("")]
        public void CurrencyGroupSeparator_Set(string newCurrencyGroupSeparator)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencyGroupSeparator = newCurrencyGroupSeparator;
            Assert.Equal(newCurrencyGroupSeparator, format.CurrencyGroupSeparator);
        }

        [Fact]
        public void CurrencyGroupSeparator_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("CurrencyGroupSeparator", () => new NumberFormatInfo().CurrencyGroupSeparator = null);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencyGroupSeparator = "string");
        }
    }
}
