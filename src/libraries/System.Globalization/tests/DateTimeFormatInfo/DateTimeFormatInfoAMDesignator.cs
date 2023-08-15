// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAMDesignator
    {
        [Fact]
        public void AMDesignator_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("AM", DateTimeFormatInfo.InvariantInfo.AMDesignator);
        }

        public static IEnumerable<object[]> AMDesignator_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "ص" };
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, "ጥዋት" };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "пр.об." };
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "a.\u00A0m." };
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, "a.\u00A0m." };
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, "dop." };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, "π.μ." };
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, "π.μ." };
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, "a.m." };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, "a.m." };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, "AM" }; // am
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "a.\u00A0m." };
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "a.\u00A0m." };
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "a.\u00A0m." };
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "قبل‌ازظهر" };
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, "ap." };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, "a.m." };
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, "לפנה״צ" };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, "am" };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "de." };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "午前" };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, "ಪೂರ್ವಾಹ್ನ" };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "오전" };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "priešpiet" };
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "priekšpusdienā" };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, "AM" }; // म.पू.
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, "PG" };
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, "PG" };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, "PG" };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, "a.m." };
            yield return new object[] { new CultureInfo("no").DateTimeFormat, "a.m." };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, "a.m." };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, "a.m." };
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, "a.m." };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, "a.m." };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "da manhã" };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, "a.m." };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, "dop." };
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, "AM" }; // пре подне
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "AM" }; // pre podne
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, "fm" };
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, "fm" };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, "முற்பகல்" };
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, "முற்பகல்" };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, "முற்பகல்" };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, "முற்பகல்" };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, "AM" };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, "ก่อนเที่ยง" };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, "ÖÖ" };
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, "ÖÖ" };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, "дп" };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "SA" };
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, "上午" };
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, "上午" };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, "上午" };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, "上午" };
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "上午" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(AMDesignator_Get_TestData_HybridGlobalization))]
        public void AMDesignator_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string value)
        {
            Assert.Equal(value, format.AMDesignator);
        }

        [Theory]
        [InlineData("")]
        [InlineData("AA")]
        [InlineData("A.M")]
        public void AMDesignator_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.AMDesignator = value;
            Assert.Equal(value, format.AMDesignator);
        }

        [Fact]
        public void AMDesignator_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AMDesignator = null);
        }

        [Fact]
        public void AMDesignator_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.AMDesignator = "AA");
        }
    }
}
