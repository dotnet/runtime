// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoFullDateTimePattern
    {
        [Fact]
        public void FullDateTimePattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("dddd, dd MMMM yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo.FullDateTimePattern);
        }

        public static IEnumerable<object[]> FullDateTimePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "dddd" };
            yield return new object[] { "F" };
            yield return new object[] { "HH:mm:ss dddd, dd MMMM yyyy" };
        }

        public static IEnumerable<object[]> FullDateTimePattern_Get_TestData_HybridGlobalization()
        {
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "dddd، d MMMM yyyy h:mm:ss tt" }; // dddd، d MMMM yyyy g h:mm:ss tt
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, "yyyy MMMM d, dddd h:mm:ss tt" };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "dddd, d MMMM yyyy г. H:mm:ss ч." }; // dddd, d MMMM yyyy 'г'. H:mm:ss
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, "dddd, d MMMM, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, "dddd, d MMMM, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "dddd, d de MMMM de yyyy H:mm:ss" }; // dddd, d MMMM 'de' yyyy H:mm:ss
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, "dddd, d de MMMM de yyyy H:mm:ss" }; // dddd, d MMMM 'de' yyyy H:mm:ss
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, "dddd d. MMMM yyyy H:mm:ss" };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, "dddd den d. MMMM yyyy HH.mm.ss" }; // dddd 'den' d. MMMM yyyy HH.mm.ss
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, "dddd, d. MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, "dddd, d. MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, "dddd, d. MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, "dddd, d. MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, "dddd, d. MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, "dddd, d. MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, "dddd, d. MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, "dddd d MMMM yyyy h:mm:ss tt" }; // dddd, d MMMM yyyy h:mm:ss tt
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, "dddd d MMMM yyyy h:mm:ss tt" }; // dddd, d MMMM yyyy h:mm:ss tt
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, "dddd, MMMM d, yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" }; // dddd, dd MMMM yyyy HH:mm:ss
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" }; // dddd, dd MMMM yyyy HH:mm:ss
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, "dddd, d MMMM yyyy HH.mm.ss" };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, "dddd, d MMMM yyyy H.mm.ss" };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, "dddd d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, "dddd, d MMMM yyyy H:mm:ss" };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, "dddd, d MMMM, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" }; // dddd, d MMMM yyyy h:mm:ss tt
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" }; // dddd, dd MMMM yyyy HH:mm:ss
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" }; // dddd, dd MMMM yyyy HH:mm:ss
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "dddd, d de MMMM de yyyy HH:mm:ss" }; // dddd, d 'de' MMMM 'de' yyyy HH:mm:ss
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "dddd, d de MMMM de yyyy H:mm:ss" }; // dddd, d 'de' MMMM 'de' yyyy H:mm:ss
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "dddd, d de MMMM de yyyy HH:mm:ss" }; // dddd, d 'de' MMMM 'de' yyyy H:mm:ss
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, "dddd, d. MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "yyyy MMMM d, dddd H:mm:ss" };
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, "dddd d. MMMM yyyy H.mm.ss" };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, "dddd, MMMM d, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, "dddd d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, "dddd d MMMM yyyy HH h mm min ss s" }; // dddd d MMMM yyyy HH 'h' mm 'min' ss 's'
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "dddd d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, "dddd, d MMMM, yyyy hh:mm:ss tt" };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, "dddd, d בMMMM yyyy H:mm:ss" };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, "dddd, d. MMMM yyyy. HH:mm:ss" };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "dddd, d. MMMM yyyy. HH:mm:ss" };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "yyyy. MMMM d., dddd H:mm:ss" };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, "dddd, d MMMM yyyy HH.mm.ss" }; // dddd, dd MMMM yyyy HH.mm.ss
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, "dddd d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "yyyy年M月d日dddd H:mm:ss" };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, "dddd, MMMM d, yyyy hh:mm:ss tt" };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "yyyy년 M월 d일 dddd tt h:mm:ss" };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "yyyy m. MMMM d d., dddd HH:mm:ss" }; // yyyy 'm'. MMMM d 'd'., dddd HH:mm:ss
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "dddd, yyyy. gada d. MMMM HH:mm:ss" }; // dddd, yyyy. 'gada' d. MMMM HH:mm:ss
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "yyyy, MMMM d, dddd h:mm:ss tt" };
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, "dddd, d MMMM, yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" }; // dd MMMM yyyy h:mm:ss tt
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, "dddd, d MMMM yyyy h:mm:ss tt" };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, "dddd d. MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, "dddd d. MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, "dddd d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, "dddd d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, "dddd d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, "dddd, d de MMMM de yyyy HH:mm:ss" }; // dddd, d 'de' MMMM 'de' yyyy HH:mm:ss
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "dddd, d de MMMM de yyyy HH:mm:ss" }; // dddd, d 'de' MMMM 'de' yyyy HH:mm:ss
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "dddd, d MMMM yyyy г. HH:mm:ss" }; // dddd, d MMMM yyyy 'г'. HH:mm:ss
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, "dddd d. MMMM yyyy H:mm:ss" };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, "dddd, d. MMMM yyyy HH:mm:ss" }; // dddd, dd. MMMM yyyy HH:mm:ss
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, "dddd, d. MMMM yyyy. HH:mm:ss" }; // dddd, dd. MMMM yyyy. HH:mm:ss
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "dddd, d. MMMM yyyy. HH:mm:ss" }; // dddd, dd. MMMM yyyy. HH:mm:ss
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, "dddd d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, "dddd d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, "dddd, d MMMM yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, "dddd, d MMMM, yyyy tt h:mm:ss" };
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, "dddd, d MMMM, yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, "dddd, d MMMM, yyyy tt h:mm:ss" };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, "dddd, d MMMM, yyyy tt h:mm:ss" };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, "d, MMMM yyyy, dddd h:mm:ss tt" };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, "ddddที่ d MMMM g yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, "d MMMM yyyy dddd h:mm:ss tt" };
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, "d MMMM yyyy dddd HH:mm:ss" };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, "dddd, d MMMM yyyy р. HH:mm:ss" }; // dddd, d MMMM yyyy 'р'. HH:mm:ss
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "dddd, d MMMM, yyyy HH:mm:ss" };
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, "yyyy年M月d日dddd HH:mm:ss" }; // yyyy年M月d日dddd tth:mm:ss
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, "yyyy年M月d日dddd tth:mm:ss" };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, "yyyy年M月d日dddd tth:mm:ss" };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, "yyyy年M月d日dddd tth:mm:ss" };
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "yyyy年M月d日 dddd tth:mm:ss" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(FullDateTimePattern_Get_TestData_HybridGlobalization))]
        public void FullDateTimePattern_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string value)
        {
            Assert.Equal(value, format.FullDateTimePattern);
        }

        [Theory]
        [MemberData(nameof(FullDateTimePattern_Set_TestData))]
        public void FullDateTimePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.FullDateTimePattern = value;
            Assert.Equal(value, format.FullDateTimePattern);
        }

        [Fact]
        public void FullDateTimePattern_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.FullDateTimePattern = null);
        }

        [Fact]
        public void FullDateTimePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.FullDateTimePattern = "dddd, dd MMMM yyyy HH:mm:ss");
        }
    }
}
