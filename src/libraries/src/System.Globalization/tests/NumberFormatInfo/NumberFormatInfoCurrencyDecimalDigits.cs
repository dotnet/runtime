// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyDecimalDigits
    {
        public static IEnumerable<object[]> CurrencyDecimalDigits_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, 2, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, 2, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("ko").NumberFormat, 0, 2 };
        }

        [Theory]
        [MemberData(nameof(CurrencyDecimalDigits_TestData))]
        public void CurrencyDecimalDigits_Get_ReturnsExpected(NumberFormatInfo format, int expectedWindows, int expectedIcu)
        {
            int expected = PlatformDetection.IsWindows ? expectedWindows : expectedIcu;
            Assert.Equal(expected, format.CurrencyDecimalDigits);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(99)]
        public void CurrencyDecimalDigits_Set_GetReturnsExpected(int newCurrencyDecimalDigits)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencyDecimalDigits = newCurrencyDecimalDigits;
            Assert.Equal(newCurrencyDecimalDigits, format.CurrencyDecimalDigits);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(100)]
        public void CurrencyDecimalDigits_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", "CurrencyDecimalDigits", () => format.CurrencyDecimalDigits = value);
        }

        [Fact]
        public void CurrencyDecimalDigits_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencyDecimalDigits = 2);
        }
    }
}
