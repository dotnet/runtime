// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNegativeInfinitySymbol
    {
        public static IEnumerable<object[]> NegativeInfinitySymbol_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat };
            yield return new object[] { CultureInfo.GetCultureInfo("fr-FR").NumberFormat };
        }

        [Theory]
        [MemberData(nameof(NegativeInfinitySymbol_TestData))]
        public void NegativeInfinitySymbol_Get(NumberFormatInfo format)
        {
            Assert.Equal(float.NegativeInfinity.ToString(format), format.NegativeInfinitySymbol);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void NegativeInfinitySymbol_Set(string newNegativeInfinitySymbol)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NegativeInfinitySymbol = newNegativeInfinitySymbol;
            Assert.Equal(newNegativeInfinitySymbol, format.NegativeInfinitySymbol);
        }

        [Fact]
        public void NegativeInfinitySymbol_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("NegativeInfinitySymbol", () => new NumberFormatInfo().NegativeInfinitySymbol = null);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NegativeInfinitySymbol = "");
        }
    }
}
