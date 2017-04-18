// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPerMilleSymbol
    {
        [Fact]
        public void PerMilleSymbol_Get_InvariantInfo()
        {
            Assert.Equal("\u2030", NumberFormatInfo.InvariantInfo.PerMilleSymbol);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void PerMilleSymbol_Set(string newPerMilleSymbol)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PerMilleSymbol = newPerMilleSymbol;
            Assert.Equal(newPerMilleSymbol, format.PerMilleSymbol);
        }

        [Fact]
        public void PerMilleSymbol_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("PerMilleSymbol", () => new NumberFormatInfo().PerMilleSymbol = null);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PerMilleSymbol = "");
        }
    }
}
