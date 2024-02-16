// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortTimePattern
    {
        [Fact]
        public void ShortTimePattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("HH:mm", DateTimeFormatInfo.InvariantInfo.ShortTimePattern);
        }

        public static IEnumerable<object[]> ShortTimePattern_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "H:mm" }; // HH:mm
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "H:mm" };
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, "H:mm" };
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, "H:mm" };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, "HH.mm" };
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, "HH.mm" };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, "H.mm" };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, "H:mm" };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "H:mm" };
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "HH:mm" }; // H:mm
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "H:mm" };
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, "H.mm" };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, "HH h mm min" }; // HH 'h' mm
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, "hh:mm tt" };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, "H:mm" };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "H:mm" };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, "HH.mm" };
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "H:mm" };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, "hh:mm tt" };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "tt h:mm" };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, "H:mm" };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, "tt h:mm" };
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, "tt h:mm" };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, "tt h:mm" };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, "h:mm tt" };
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "HH:mm" };
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, "HH:mm" }; // tth:mm
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, "tth:mm" };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, "tth:mm" };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, "tth:mm" };
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "tth:mm" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(ShortTimePattern_Get_TestData_HybridGlobalization))]
        public void ShortTimePattern_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string value)
        {
            Assert.Equal(value, format.ShortTimePattern);
        }

        public static IEnumerable<object[]> ShortTimePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "HH:mm" };
            yield return new object[] { "t" };
        }

        [Theory]
        [MemberData(nameof(ShortTimePattern_Set_TestData))]
        public void ShortTimePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.ShortTimePattern = value;
            Assert.Equal(value, format.ShortTimePattern);
        }

        [Fact]
        public void ShortTimePattern_Set_InvalidatesDerivedPattern()
        {
            const string Pattern = "#$";
            var format = new DateTimeFormatInfo();
            var d = DateTimeOffset.Now;
            d.ToString("g", format); // GeneralShortTimePattern
            format.ShortTimePattern = Pattern;
            Assert.Contains(Pattern, d.ToString("g", format));
        }

        [Fact]
        public void ShortTimePattern_SetNull_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.ShortTimePattern = null);
        }

        [Fact]
        public void ShortTimePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortTimePattern = "HH:mm");
        }
    }
}
