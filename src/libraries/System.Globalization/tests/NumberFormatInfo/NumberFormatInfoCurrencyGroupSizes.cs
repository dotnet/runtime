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
            yield return new object[] { NumberFormatInfo.InvariantInfo, new int[] { 3 } };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, new int[] { 3 } };

            if (PlatformDetection.IsNotUsingLimitedCultures && !PlatformDetection.IsUbuntu && !PlatformDetection.IsWindows7 && !PlatformDetection.IsWindows8x && !PlatformDetection.IsFedora)
            {
                yield return new object[] { CultureInfo.GetCultureInfo("ur-IN").NumberFormat, new int[] { 3, 2 } };
            }
            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { CultureInfo.GetCultureInfo("ar-SA").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("am-ET").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("bg-BG").NumberFormat, new int[] { 0 } };
                yield return new object[] { CultureInfo.GetCultureInfo("bn-BD").NumberFormat, new int[] { 3, 2 } };
                yield return new object[] { CultureInfo.GetCultureInfo("ca-AD").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("cs-CZ").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("da-DK").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("de-DE").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("el-GR").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("es-MX").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("et-EE").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("fa-IR").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("fi-FI").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("fil-PH").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("fr-CA").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("gu-IN").NumberFormat, new int[] { 3, 2 } };
                yield return new object[] { CultureInfo.GetCultureInfo("he-IL").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("hi-IN").NumberFormat, new int[] { 3, 2 } };
                yield return new object[] { CultureInfo.GetCultureInfo("hr-HR").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("hu-HU").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("id-ID").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("it-CH").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("ja-JP").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("kn-IN").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("ko-KR").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("lt-LT").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("lv-LV").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("ml-IN").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("mr-IN").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("ms-BN").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("no-NO").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("nl-NL").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("pl-PL").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("pt-BR").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("ro-RO").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("ru-RU").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("sk-SK").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("sl-SI").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("sv-SE").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("sw-UG").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("ta-SG").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("te-IN").NumberFormat, new int[] { 3, 2 } };
                yield return new object[] { CultureInfo.GetCultureInfo("th-TH").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("tr-CY").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("uk-UA").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("vi-VN").NumberFormat, new int[] { 3 } };
                yield return new object[] { CultureInfo.GetCultureInfo("zh-CN").NumberFormat, new int[] { 3 } };
            }
        }

        [Theory]
        [MemberData(nameof(CurrencyGroupSizes_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/79867", typeof(PlatformDetection), nameof(PlatformDetection.IsArm64Process), nameof(PlatformDetection.IsWindows))]
        public void CurrencyGroupSizes_Get_ReturnsExpected(NumberFormatInfo format, int[] expected)
        {
            Assert.Equal(expected, format.CurrencyGroupSizes);
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
