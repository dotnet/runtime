// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortDatePattern
    {
        public static IEnumerable<object[]> ShortDatePattern_Get_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "MM/dd/yyyy", "invariant" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "M/d/yyyy", "en-US" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "dd/MM/yyyy", "fr-FR" };
        }

        public static IEnumerable<object[]> ShortDatePattern_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "d\u200f/M\u200f/yyyy" }; // "d\u200f/M\u200f/yyyy g"
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "d.MM.yyyy г." }; // "d.MM.yyyy 'г'."
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, "M/d/yyyy" };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, "M/d/yyyy" };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, "yyyy-MM-dd" };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, PlatformDetection.IsNodeJS ? "dd/MM/yyyy" : "dd.MM.yyyy" }; // NodeJS responds like dotnet
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, "M/d/yyyy" };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, "M/d/yyyy" };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, "M/d/yyyy" };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, "d/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, "M/d/yyyy" }; // "dd/MM/yyyy"
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, "M/d/yyyy" };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, "yyyy-MM-dd" };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, "M/d/yyyy" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "M/d/yyyy" };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, "M/d/yyyy" };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, "yyyy/MM/dd" };
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "yyyy/M/d" }; // "yyyy/M/d"
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, "d.M.yyyy" };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, "M/d/yyyy" }; 
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, "d/MM/yyyy" };
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, "yyyy-MM-dd" };
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, "d.M.yyyy" };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, "d. M. yyyy." };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "dd. MM. yyyy." };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "yyyy. MM. dd." };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "yyyy/MM/dd" };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "yyyy. M. d." };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "yyyy-MM-dd" };
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, "d/MM/yyyy" };
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, "d/MM/yyyy" };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, "d/MM/yyyy" };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("no").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, "dd-MM-yyyy" };
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, "d/MM/yyyy" };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, "dd-MM-yyyy" };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, "d.MM.yyyy" }; // "dd.MM.yyyy"
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, "d. M. yyyy" };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, "d. MM. yyyy" };
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, "d.M.yyyy." };
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "d.M.yyyy." };
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, "yyyy-MM-dd" };
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, "yyyy-MM-dd" };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, "dd-MM-yyyy" };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, "d.MM.yyyy" };
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, "d.MM.yyyy" };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, "dd.MM.yyyy" };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, "yyyy/M/d" };
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, "dd/MM/yyyy" };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, "d/M/yyyy" };
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "yyyy/M/d" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnOSX))]
        [MemberData(nameof(ShortDatePattern_Get_TestData))]
        public void ShortDatePattern_Get_ReturnsExpected(DateTimeFormatInfo format, string expected, string cultureName)
        {
            Assert.True(expected == format.ShortDatePattern, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {format.ShortDatePattern}");
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(ShortDatePattern_Get_TestData_HybridGlobalization))]
        public void ShortDatePattern_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.ShortDatePattern);
        }

        [Fact]
        public void ShortDatePattern_InvariantInfo()
        {
            Assert.Equal("MM/dd/yyyy", DateTimeFormatInfo.InvariantInfo.ShortDatePattern);
        }

        public static IEnumerable<object[]> ShortDatePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "MM/dd/yyyy" };
            yield return new object[] { "MM-DD-yyyy" };
            yield return new object[] { "d" };
        }

        [Theory]
        [MemberData(nameof(ShortDatePattern_Set_TestData))]
        public void ShortDatePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.ShortDatePattern = value;
            Assert.Equal(value, format.ShortDatePattern);
        }

        [Fact]
        public void ShortDatePattern_Set_InvalidatesDerivedPatterns()
        {
            const string Pattern = "#$";
            var format = new DateTimeFormatInfo();
            var d = DateTimeOffset.Now;
            d.ToString("G", format); // GeneralLongTimePattern
            d.ToString("g", format); // GeneralShortTimePattern
            d.ToString(format); // DateTimeOffsetPattern
            format.ShortDatePattern = Pattern;
            Assert.Contains(Pattern, d.ToString("G", format));
            Assert.Contains(Pattern, d.ToString("g", format));
            Assert.Contains(Pattern, d.ToString(format));
        }

        [Fact]
        public void ShortDatePattern_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.ShortDatePattern = null);
        }

        [Fact]
        public void ShortDatePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortDatePattern = "MM/dd/yyyy");
        }
    }
}
