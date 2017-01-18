// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyGroupSizes
    {
        public static IEnumerable<object[]> CurrencyGroupSizes_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, new int[] { 3 } };
            yield return new object[] { new CultureInfo("en-US").NumberFormat, new int[] { 3 } };

            // TODO: when dotnet/corefx#2103 is addressed, we should also check fr-FR
            if (!PlatformDetection.IsUbuntu1510 && !PlatformDetection.IsUbuntu1604 && !PlatformDetection.IsUbuntu1610
                && !PlatformDetection.IsWindows7 && !PlatformDetection.IsWindows8x && !PlatformDetection.IsFedora24)
            {
                yield return new object[] { new CultureInfo("ur-IN").NumberFormat, new int[] { 3, 2 } };
            }
        }

        [Theory]
        [MemberData(nameof(CurrencyGroupSizes_TestData))]
        public void CurrencyGroupSizes_Get(NumberFormatInfo format, int[] expected)
        {
            Assert.Equal(expected, format.CurrencyGroupSizes);
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 2, 3, 4 })]
        [InlineData(new int[] { 2, 3, 4, 0 })]
        [InlineData(new int[] { 0 })]
        public void CurrencyGroupSizes_Set(int[] newCurrencyGroupSizes)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencyGroupSizes = newCurrencyGroupSizes;
            Assert.Equal(newCurrencyGroupSizes, format.CurrencyGroupSizes);
        }

        [Fact]
        public void CurrencyGroupSizes_Set_Invalid()
        {
            Assert.Throws<ArgumentNullException>("CurrencyGroupSizes", () => new NumberFormatInfo().CurrencyGroupSizes = null);
            Assert.Throws<ArgumentException>("CurrencyGroupSizes", () => new NumberFormatInfo().CurrencyGroupSizes = new int[] { -1, 1, 2 });
            Assert.Throws<ArgumentException>("CurrencyGroupSizes", () => new NumberFormatInfo().CurrencyGroupSizes = new int[] { 98, 99, 100 });
            Assert.Throws<ArgumentException>("CurrencyGroupSizes", () => new NumberFormatInfo().CurrencyGroupSizes = new int[] { 0, 1, 2 });

            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencyGroupSizes = new int[] { 1, 2, 3 });
        }
    }
}
