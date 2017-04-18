// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNaNSymbol
    {
        [Fact]
        public void NaNSymbol_Get_InvariantInfo()
        {
            Assert.Equal("NaN", NumberFormatInfo.InvariantInfo.NaNSymbol);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void NaNSymbol_Set(string newNaNSymbol)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NaNSymbol = newNaNSymbol;
            Assert.Equal(newNaNSymbol, format.NaNSymbol);
        }

        [Fact]
        public void NaNSymbol_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("NaNSymbol", () => new NumberFormatInfo().NaNSymbol = null);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NaNSymbol = "");
        }
    }
}
