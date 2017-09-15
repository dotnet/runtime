// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentSymbol
    {
        public static IEnumerable<object[]> PercentSymbol_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, "%" };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, "%" };
        }

        [Theory]
        [MemberData(nameof(PercentSymbol_TestData))]
        public void PercentSymbol_Get(NumberFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.PercentSymbol);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void PercentSymbol_Set(string newPercentSymbol)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentSymbol = newPercentSymbol;
            Assert.Equal(newPercentSymbol, format.PercentSymbol);
        }

        [Fact]
        public void PercentSymbol_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("PercentSymbol", () => new NumberFormatInfo().PercentSymbol = null);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentSymbol = "");
        }
    }
}
