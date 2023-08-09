// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoCalendarWeekRule
    {
        public static IEnumerable<object[]> CalendarWeekRule_Get_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, CalendarWeekRule.FirstDay };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, CalendarWeekRule.FirstDay };

            if (PlatformDetection.IsNotBrowser)
            {
                yield return new object[] { new CultureInfo("br-FR").DateTimeFormat, DateTimeFormatInfoData.BrFRCalendarWeekRule() };
            }
            else
            {
                // "br-FR" is not presented in Browser's ICU. Let's test ru-RU instead.
                yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
            }

            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-US").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("en-US").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("es-419").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, PlatformDetection.IsNodeJS ? CalendarWeekRule.FirstDay : CalendarWeekRule.FirstFourDayWeek }; // v8/Browser responds like dotnet
                yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, PlatformDetection.IsNodeJS ? CalendarWeekRule.FirstFourDayWeek : CalendarWeekRule.FirstDay }; // v8/Browser responds like dotnet
                yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, CalendarWeekRule.FirstDay };
                yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, CalendarWeekRule.FirstDay };
            }
        }

        [Theory]
        [MemberData(nameof(CalendarWeekRule_Get_TestData))]
        public void CalendarWeekRuleTest(DateTimeFormatInfo format, CalendarWeekRule expected)
        {
            Assert.Equal(expected, format.CalendarWeekRule);
        }

        [Theory]
        [InlineData(CalendarWeekRule.FirstDay)]
        [InlineData(CalendarWeekRule.FirstFourDayWeek)]
        [InlineData(CalendarWeekRule.FirstFullWeek)]
        public void CalendarWeekRule_Set_GetReturnsExpected(CalendarWeekRule value)
        {
            var format = new DateTimeFormatInfo();
            format.CalendarWeekRule = value;
            Assert.Equal(value, format.CalendarWeekRule);
        }

        [Theory]
        [InlineData(CalendarWeekRule.FirstDay - 1)]
        [InlineData(CalendarWeekRule.FirstFourDayWeek + 1)]
        public void CalendarWeekRule_SetInvalidValue_ThrowsArgumentOutOfRangeException(CalendarWeekRule value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => format.CalendarWeekRule = value);
        }

        [Fact]
        public void CalendarWeekRule_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.CalendarWeekRule = CalendarWeekRule.FirstDay);
        }
    }
}
