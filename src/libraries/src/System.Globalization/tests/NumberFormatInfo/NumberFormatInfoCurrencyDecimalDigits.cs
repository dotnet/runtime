// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        public void CurrencyDecimalDigits_Get(NumberFormatInfo format, int expectedWindows, int expectedIcu)
        {
            int expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? expectedWindows : expectedIcu;
            Assert.Equal(expected, format.CurrencyDecimalDigits);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(99)]
        public void CurrencyDecimalDigits_Set(int newCurrencyDecimalDigits)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencyDecimalDigits = newCurrencyDecimalDigits;
            Assert.Equal(newCurrencyDecimalDigits, format.CurrencyDecimalDigits);
        }

        [Fact]
        public void CurrencyDecimalDigits_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("CurrencyDecimalDigits", () => new NumberFormatInfo().CurrencyDecimalDigits = -1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("CurrencyDecimalDigits", () => new NumberFormatInfo().CurrencyDecimalDigits = 100);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencyDecimalDigits = 2);
        }
    }
}
