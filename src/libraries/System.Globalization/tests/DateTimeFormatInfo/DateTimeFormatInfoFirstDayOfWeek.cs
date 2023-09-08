// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoFirstDayOfWeek
    {
        public static IEnumerable<object[]> FirstDayOfWeek_Get_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, DayOfWeek.Sunday };
            if (!PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { new CultureInfo("en-US", false).DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("fr-FR", false).DateTimeFormat, DayOfWeek.Monday };
            }
            else
            {
                // see the comments on the right to check the non-Hybrid result, if it differs
                yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, DayOfWeek.Saturday };
                yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, DayOfWeek.Monday }; // originally in ICU: Sunday, even though ISO 8601 states: Monday
                yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, DayOfWeek.Saturday };
                yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("en-US").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("es-419").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, DayOfWeek.Saturday };
                yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("no").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, DayOfWeek.Monday };
                yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, DayOfWeek.Monday  };
                yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, DayOfWeek.Sunday };
                yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, DayOfWeek.Sunday };
            }
        }

        [Theory]
        [MemberData(nameof(FirstDayOfWeek_Get_TestData))]
        public void FirstDayOfWeek(DateTimeFormatInfo format, DayOfWeek expected)
        {
            Assert.Equal(expected, format.FirstDayOfWeek);
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday)]
        [InlineData(DayOfWeek.Monday)]
        [InlineData(DayOfWeek.Tuesday)]
        [InlineData(DayOfWeek.Wednesday)]
        [InlineData(DayOfWeek.Thursday)]
        [InlineData(DayOfWeek.Friday)]
        [InlineData(DayOfWeek.Saturday)]
        public void FirstDayOfWeek_Set_GetReturnsExpected(DayOfWeek value)
        {
            var format = new DateTimeFormatInfo();
            format.FirstDayOfWeek = value;
            Assert.Equal(value, format.FirstDayOfWeek);
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday - 1)]
        [InlineData(DayOfWeek.Saturday + 1)]
        public void FirstDayOfWeek_SetInvalid_ThrowsArgumentOutOfRangeException(DayOfWeek value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => format.FirstDayOfWeek = value);
        }

        [Fact]
        public void FirstDayOfWeek_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.FirstDayOfWeek = DayOfWeek.Wednesday);
        }
    }
}
