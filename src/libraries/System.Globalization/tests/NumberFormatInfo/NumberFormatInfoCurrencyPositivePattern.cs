// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyPositivePattern
    {
        public static IEnumerable<object[]> CurrencyPositivePattern_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, 0 };
            yield return new object[] { CultureInfo.GetCultureInfo("fr-FR").NumberFormat, 3 };
            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { CultureInfo.GetCultureInfo("ar-SA").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("am-ET").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("bg-BG").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("bn-BD").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("ca-AD").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("cs-CZ").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("da-DK").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-AT").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-BE").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-CH").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-DE").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-LI").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-LU").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("el-CY").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-CH").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("es-ES").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("es-MX").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("et-EE").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("fa-IR").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("fi-FI").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("fil-PH").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("fr-FR").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("gu-IN").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("he-IL").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("hi-IN").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("hr-HR").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("hu-HU").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("id-ID").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("it-CH").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("it-IT").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("ja-JP").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("kn-IN").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("ko-KR").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("lt-LT").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("lv-LV").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("ml-IN").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("mr-IN").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("ms-BN").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("ms-MY").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("ms-SG").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("no-NO").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("nl-AW").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("nl-NL").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("pl-PL").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("pt-BR").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("pt-PT").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("ro-RO").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("ru-RU").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("sk-SK").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("sl-SI").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("sr-Cyrl-RS").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("sr-Latn-RS").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("sv-SE").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("sw-CD").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("ta-LK").NumberFormat, 2 };
                yield return new object[] { CultureInfo.GetCultureInfo("te-IN").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("th-TH").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("tr-TR").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("uk-UA").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("vi-VN").NumberFormat, 3 };
                yield return new object[] { CultureInfo.GetCultureInfo("zh-CN").NumberFormat, 0 };
            }
        }

        [Theory]
        [MemberData(nameof(CurrencyPositivePattern_TestData))]
        public void CurrencyPositivePattern_Get_ReturnsExpected(NumberFormatInfo format, int expected)
        {
            Assert.Equal(expected, format.CurrencyPositivePattern);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void CurrencyPositivePattern_Set_GetReturnsExpected(int newCurrencyPositivePattern)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencyPositivePattern = newCurrencyPositivePattern;
            Assert.Equal(newCurrencyPositivePattern, format.CurrencyPositivePattern);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void CurrencyPositivePattern_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", "CurrencyPositivePattern", () => format.CurrencyPositivePattern = value);
        }

        [Fact]
        public void CurrencyPositivePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencyPositivePattern = 1);
        }
    }
}
