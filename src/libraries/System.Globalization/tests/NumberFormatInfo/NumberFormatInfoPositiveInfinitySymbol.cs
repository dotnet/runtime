// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPositiveInfinitySymbol
    {
        public static IEnumerable<object[]> PositiveInfinitySymbol_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat };
            yield return new object[] { CultureInfo.GetCultureInfo("fr-FR").NumberFormat };
        }

        [Theory]
        [MemberData(nameof(PositiveInfinitySymbol_TestData))]
        public void PositiveInfinitySymbol_Get(NumberFormatInfo format)
        {
            Assert.Equal(float.PositiveInfinity.ToString(format), format.PositiveInfinitySymbol);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void PositiveInfinitySymbol_Set(string newPositiveInfinitySymbol)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PositiveInfinitySymbol = newPositiveInfinitySymbol;
            Assert.Equal(newPositiveInfinitySymbol, format.PositiveInfinitySymbol);
        }

        [Fact]
        public void PositiveInfinitySymbol_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("PositiveInfinitySymbol", () => new NumberFormatInfo().PositiveInfinitySymbol = null);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PositiveInfinitySymbol = "");
        }
    }
}
