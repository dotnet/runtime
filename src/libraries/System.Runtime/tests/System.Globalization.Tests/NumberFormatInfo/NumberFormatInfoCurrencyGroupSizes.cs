// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyGroupSizes
    {
        public static IEnumerable<object[]> CurrencyGroupSizes_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, new int[] { 3 }, null };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, new int[] { 3 }, null };

            if (PlatformDetection.IsNotUsingLimitedCultures && !PlatformDetection.IsUbuntu && !PlatformDetection.IsWindows7 && !PlatformDetection.IsWindows8x && !PlatformDetection.IsFedora)
            {
                yield return new object[] { CultureInfo.GetCultureInfo("ur-IN").NumberFormat, new int[] { 3, 2 },  new int[] { 3 }};
            }
        }

        [Theory]
        [MemberData(nameof(CurrencyGroupSizes_TestData))]
        public void CurrencyGroupSizes_Get_ReturnsExpected(NumberFormatInfo format, int[] expected, int [] expectedAlternative)
        {
            Assert.True(format.CurrencyGroupSizes.AsSpan().SequenceEqual(expected.AsSpan()) || format.CurrencyGroupSizes.AsSpan().SequenceEqual(expectedAlternative.AsSpan()),
            $"Expected {string.Join(", ", expected)} or {string.Join(", ", expectedAlternative ?? Array.Empty<int>())}, got {string.Join(", ", format.CurrencyGroupSizes)}");
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 2, 3, 4 })]
        [InlineData(new int[] { 2, 3, 4, 0 })]
        [InlineData(new int[] { 0 })]
        public void CurrencyGroupSizes_Set_GetReturnsExpected(int[] newCurrencyGroupSizes)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencyGroupSizes = newCurrencyGroupSizes;
            Assert.Equal(newCurrencyGroupSizes, format.CurrencyGroupSizes);
        }

        [Fact]
        public void CurrencyGroupSizes_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "CurrencyGroupSizes", () => format.CurrencyGroupSizes = null);
        }

        [Theory]
        [InlineData(new int[] { -1, 1, 2 })]
        [InlineData(new int[] { 98, 99, 100 })]
        [InlineData(new int[] { 0, 1, 2 })]
        public void CurrencyGroupSizes_SetInvalid_ThrowsArgumentException(int[] value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", "CurrencyGroupSizes", () => format.CurrencyGroupSizes = value);
        }

        [Fact]
        public void CurrencyGroupSizes_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencyGroupSizes = new int[] { 1, 2, 3 });
        }
    }
}
