// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoPMDesignator
    {
        [Fact]
        public void PMDesignator_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("PM", DateTimeFormatInfo.InvariantInfo.PMDesignator);
        }

        public static IEnumerable<object[]> PMDesignator_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "م" };
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, "ከሰዓት" };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "сл.об." };
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "p.\u00A0m." };
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, "p.\u00A0m." };
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, "odp." };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, "μ.μ." };
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, "μ.μ." };
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, "p.m." };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, "p.m." };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, "PM" }; // "pm"
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "p.\u00A0m." }; // p.m.
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "p.\u00A0m." }; // p.m.
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "p.\u00A0m." }; // p.m.
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "بعدازظهر" };
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, "ip." };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, "p.m." };
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, "אחה״צ" };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, "pm" };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "du." };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "午後" };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, "ಅಪರಾಹ್ನ" };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "오후" };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "popiet" };
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "pēcpusdienā" };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, "PM" }; // म.उ.
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, "PTG" };
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, "PTG" };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, "PTG" };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, "p.m." };
            yield return new object[] { new CultureInfo("no").DateTimeFormat, "p.m." };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, "p.m." };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, "p.m." };
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, "p.m." };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, "p.m." };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "da tarde" };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, "p.m." };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, "pop." };
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, "PM" }; // по подне
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "PM" }; // po podne
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, "em" };
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, "em" };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, "பிற்பகல்" };
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, "பிற்பகல்" };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, "பிற்பகல்" };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, "பிற்பகல்" };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, "PM" };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, "หลังเที่ยง" };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, "ÖS" };
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, "ÖS" };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, "пп" };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "CH" };
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, "下午" };
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, "下午" };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, "下午" };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, "下午" };
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "下午" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(PMDesignator_Get_TestData_HybridGlobalization))]
        public void PMDesignator_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string value)
        {
            Assert.Equal(value, format.PMDesignator);
        }

        [Theory]
        [InlineData("")]
        [InlineData("PP")]
        [InlineData("P.M")]
        public void PMDesignator_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.PMDesignator = value;
            Assert.Equal(value, format.PMDesignator);
        }

        [Fact]
        public void PMDesignator_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.PMDesignator = null);
        }

        [Fact]
        public void PMDesignator_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.PMDesignator = "PP");
        }
    }
}
