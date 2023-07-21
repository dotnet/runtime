// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoRFC1123Pattern
    {
        public static IEnumerable<object[]> RFC1123Pattern_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };

            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("no").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
                yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
            }
        }

        [Theory]
        [MemberData(nameof(RFC1123Pattern_TestData))]
        public void RFC1123Pattern_Get_ReturnsExpected(DateTimeFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.RFC1123Pattern);
        }
    }
}
