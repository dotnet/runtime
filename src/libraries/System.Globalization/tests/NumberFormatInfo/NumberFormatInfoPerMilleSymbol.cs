// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPerMilleSymbol
    {
        [Fact]
        public void PerMilleSymbol_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("\u2030", NumberFormatInfo.InvariantInfo.PerMilleSymbol);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void PerMilleSymbol_Set_GetReturnsExpected(string newPerMilleSymbol)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PerMilleSymbol = newPerMilleSymbol;
            Assert.Equal(newPerMilleSymbol, format.PerMilleSymbol);
        }

        [Fact]
        public void PerMilleSymbol_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "PerMilleSymbol", () => format.PerMilleSymbol = null);
        }

        [Fact]
        public void PerMilleSymbol_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PerMilleSymbol = "");
        }
    }
}
