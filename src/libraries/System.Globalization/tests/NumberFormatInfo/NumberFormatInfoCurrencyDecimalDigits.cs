// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public static IEnumerable<object[]> CurrencyDecimalDigits_TestData_WasmLocales()
        {
            yield return new object[] { CultureInfo.GetCultureInfo("ar-SA").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("am-ET").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("bg-BG").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("bn-BD").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("bn-IN").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("ca-AD").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("ca-ES").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("cs-CZ").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("da-DK").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("de-DE").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("el-CY").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("el-GR").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-BI").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-CM").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-MG").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-RW").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-SL").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-UG").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-VU").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("es-ES").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("et-EE").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("fa-IR").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("fi-FI").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("fil-PH").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("fr-FR").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("he-IL").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("hi-IN").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("hr-HR").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("hu-HU").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("id-ID").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("it-IT").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("ja-JP").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("kn-IN").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("lt-LT").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("lv-LV").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("ml-IN").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("mr-IN").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("ms-SG").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("nb-NO").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("nl-NL").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("pl-PL").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("pt-PT").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("ro-RO").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("ru-RU").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("sk-SK").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("sl-SI").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("sr-Cyrl-RS").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("sr-Latn-RS").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("sv-AX").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("sv-SE").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("sw-UG").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("ta-IN").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("te-IN").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("th-TH").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("tr-TR").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("uk-UA").NumberFormat, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("vi-VN").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("zh-CN").NumberFormat, 2 };
        }

        [Theory]
        [MemberData(nameof(CurrencyDecimalDigits_TestData))]
        public void CurrencyDecimalDigits_Get_ReturnsExpected(NumberFormatInfo format, int expectedNls, int expectedIcu)
        {
            int expected = PlatformDetection.IsNlsGlobalization ? expectedNls : expectedIcu;
            Assert.Equal(expected, format.CurrencyDecimalDigits);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(CurrencyDecimalDigits_TestData_WasmLocales))]
        public void CurrencyDecimalDigits_Get_ReturnsExpected_WasmLocales(NumberFormatInfo format, int expected)
        {
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
