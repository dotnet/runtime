// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetAbbreviatedEraName
    {
        public static IEnumerable<object[]> GetAbbreviatedEraName_TestData()
        {
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, 0, DateTimeFormatInfoData.EnUSAbbreviatedEraName() };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, 1, DateTimeFormatInfoData.EnUSAbbreviatedEraName() };
            yield return new object[] { new DateTimeFormatInfo(), 0, "AD" };
            yield return new object[] { new DateTimeFormatInfo(), 1, "AD" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, 1, DateTimeFormatInfoData.JaJPAbbreviatedEraName() };

            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                // see the comments on the right to check the non-Hybrid result, if it differs
                yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, 1, "هـ" };
                yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, 1, "ዓ/ም" };
                yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, 1, "сл.Хр." };
                yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, 1, "খৃষ্টাব্দ" };
                yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, 1, PlatformDetection.IsNodeJS ? "খৃষ্টাব্দ" : "খ্রিঃ" }; // NodeJS responses like dotnet
                yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, 1, "dC" };
                yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, 1, "dC" };
                yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, 1, "n.l." };
                yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, 1, "eKr" };
                yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, 1, "n. Chr." };
                yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, 1, "n. Chr." };
                yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, 1, "n. Chr." };
                yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, 1, "n. Chr." };
                yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, 1, "n. Chr." };
                yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, 1, "n. Chr." };
                yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, 1, "n. Chr." };
                yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, 1, "μ.Χ." };
                yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, 1, "μ.Χ." };
                yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, 1, "A" };
                yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, 1, "A" };
                yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, 1, "A" };
                yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, 1, "A" };
                yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, 1, "A" };
                yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, 1, "A" };
                yield return new object[] { new CultureInfo("en-US").DateTimeFormat, 1, "A" };
                yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, 1, "A" };
                yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, 1, "A" }; // AD
                yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, 1, "d. C." };
                string spanishEra = PlatformDetection.IsNodeJS ? "d. C." : "d.C."; // NodeJS responses like dotnet
                yield return new object[] { new CultureInfo("es-419").DateTimeFormat, 1, spanishEra };
                yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, 1, spanishEra };
                yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, 1, "pKr" };
                yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, 1, "ه.ش" }; // ه‍.ش.
                yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, 1, "jKr" };
                yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, 1, "AD" };
                yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, 1, "ap. J.-C." };
                yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, 1, "ap. J.-C." };
                yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, 1, "ap. J.-C." };
                yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, 1, "ap. J.-C." };
                yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, 1, "ઇસ" };
                yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, 1, "אחריי" }; // לספירה
                yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, 1, "ईस्वी" };
                yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, 1, "AD" }; // po. Kr.
                yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, 1, "AD" };
                yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, 1, "isz." };
                yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, 1, "M" };
                yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, 1, "dC" }; // d.C.
                yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, 1, "dC" };
                yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, 1, "AD" };
                yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, 1, "ಕ್ರಿ.ಶ" };
                yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, 1, "AD" };
                yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, 1, "po Kr." };
                yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, 1, "m.ē." };
                yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, 1, "എഡി" };
                yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, 1, "इ. स." };
                yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, 1, "TM" };
                yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, 1, "TM" };
                yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, 1, "TM" };
                yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, 1, "e.Kr." };
                yield return new object[] { new CultureInfo("no").DateTimeFormat, 1, "e.Kr." };
                yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, 1, "e.Kr." };
                yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, 1, "n.C." };
                yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, 1, "n.C." }; // n.Chr.
                yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, 1, "n.C." };
                yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, 1, "n.e." };
                yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, 1, "d.C." };
                yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, 1, "d.C." };
                yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, 1, "d.Hr." };
                yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, 1, "н.э." };
                yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, 1, "po Kr." };
                yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, 1, "po Kr." };
                yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, 1, "н.е." };
                yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, 1, "n.e." };
                yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, 1, "e.Kr." };
                yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, 1, "e.Kr." };
                yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, 1, "BK" };
                yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, 1, "BK" };
                yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, 1, "BK" };
                yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, 1, "BK" };
                yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, 1, "கி.பி." };
                yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, 1, "கி.பி." };
                yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, 1, "கி.பி." };
                yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, 1, "கி.பி." };
                yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, 1, "క్రీశ" };
                yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, 1, "พ.ศ." };
                yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, 1, "MS" };
                yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, 1, "MS" };
                yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, 1, "н.е." };
                yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, 1, "sau CN" };
                yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, 1, "公元" };
                yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, 1, "公元" };
                yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, 1, "公元" };
                yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, 1, "公元" };
                yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, 1, "西元" };
            }
        }

        [Theory]
        [MemberData(nameof(GetAbbreviatedEraName_TestData))]
        public void GetAbbreviatedEraName_Invoke_ReturnsExpected(DateTimeFormatInfo format, int era, string expected)
        {
            Assert.Equal(expected, format.GetAbbreviatedEraName(era));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void GetAbbreviatedEraName_Invalid(int era)
        {
            var format = new CultureInfo("en-US").DateTimeFormat;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("era", () => format.GetAbbreviatedEraName(era));
        }
    }
}
