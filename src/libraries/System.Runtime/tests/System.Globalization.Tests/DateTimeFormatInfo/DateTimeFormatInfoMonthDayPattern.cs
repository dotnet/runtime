// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMonthDayPattern
    {
        [Fact]
        public void MonthDayPattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("MMMM dd", DateTimeFormatInfo.InvariantInfo.MonthDayPattern);
        }

        public static IEnumerable<object[]> MonthDayPattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "MMMM" };
            yield return new object[] { "MMM dd" };
            yield return new object[] { "M" };
            yield return new object[] { "dd MMMM" };
            yield return new object[] { "MMMM dd" };
            yield return new object[] { "m" };
        }

        public static IEnumerable<object[]> MonthDayPattern_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "d de MMMM" }; // d MMMM
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, "d de MMMM" }; // d MMMM
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, "MMMM d" }; // "d MMMM"
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "d de MMMM" }; // d 'de' MMMM
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "d de MMMM" }; // d 'de' MMMM
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "d de MMMM" }; // d 'de' MMMM
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "d MMMM" }; //  "MMMM d"
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, "d בMMMM" };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "MMMM d." };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "M月d日" };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "MMMM d일" };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "MMMM d d." }; // MMMM d 'd'.
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "MMMM d" };
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("no").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, "d de MMMM" }; // d 'de' MMMM
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "d de MMMM" }; // d 'de' MMMM
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "d. MMMM" };
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "d MMMM" };
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, "M月d日" };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, "M月d日" };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, "M月d日" };
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "M月d日" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(MonthDayPattern_Get_TestData_HybridGlobalization))]
        public void MonthDayPattern_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.MonthDayPattern);
        }

        [Theory]
        [MemberData(nameof(MonthDayPattern_Set_TestData))]
        public void MonthDayPattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.MonthDayPattern = value;
            Assert.Equal(value, format.MonthDayPattern);
        }

        [Fact]
        public void MonthDayPattern_SetNull_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.MonthDayPattern = null);
        }

        [Fact]
        public void MonthDayPattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.MonthDayPattern = "MMMM dd");
        }
    }
}
