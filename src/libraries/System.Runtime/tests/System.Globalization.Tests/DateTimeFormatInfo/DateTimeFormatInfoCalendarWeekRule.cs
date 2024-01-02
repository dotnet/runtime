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
            yield return new object[] { "invariant", CalendarWeekRule.FirstDay };
            yield return new object[] { "en-US", CalendarWeekRule.FirstDay };

            if (PlatformDetection.IsNotBrowser)
            {
                yield return new object[] { "br-FR", DateTimeFormatInfoData.BrFRCalendarWeekRule() };
            }
            else
            {
                // "br-FR" is not presented in Browser's ICU. Let's test ru-RU instead.
                yield return new object[] { "ru-RU", CalendarWeekRule.FirstFourDayWeek };
            }

            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { "ar-SA", CalendarWeekRule.FirstDay };
                yield return new object[] { "am-ET", CalendarWeekRule.FirstDay };
                yield return new object[] { "bg-BG", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "bn-BD", CalendarWeekRule.FirstDay };
                yield return new object[] { "bn-IN", CalendarWeekRule.FirstDay };
                yield return new object[] { "ca-AD", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "ca-ES", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "cs-CZ", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "da-DK", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "de-AT", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "de-BE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "de-CH", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "de-DE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "de-IT", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "de-LI", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "de-LU", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "el-CY", CalendarWeekRule.FirstDay };
                yield return new object[] { "el-GR", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-AE", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-AG", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-AI", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-AS", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-AT", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-AU", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-BB", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-BE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-BI", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-BM", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-BS", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-BW", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-BZ", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-CA", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-CC", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-CH", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-CK", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-CM", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-CX", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-CY", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-DE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-DK", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-DM", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-ER", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-FI", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-FJ", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-FK", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-FM", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-GB", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-GD", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-GG", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-GH", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-GI", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-GM", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-GU", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-GY", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-HK", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-IE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-IL", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-IM", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-IN", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-IO", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-JE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-JM", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-KE", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-KI", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-KN", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-KY", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-LC", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-LR", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-LS", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-MG", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-MH", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-MO", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-MP", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-MS", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-MT", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-MU", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-MW", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-MY", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-NA", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-NF", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-NG", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-NL", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-NR", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-NU", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-NZ", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-PG", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-PH", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-PK", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-PN", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-PR", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-PW", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-RW", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-SB", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-SC", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-SD", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-SE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "en-SG", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-SH", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-SI", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-SL", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-SS", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-SX", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-SZ", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-TC", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-TK", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-TO", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-TT", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-TV", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-TZ", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-UG", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-UM", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-US", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-VC", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-VG", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-VI", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-VU", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-WS", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-ZA", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-ZM", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-ZW", CalendarWeekRule.FirstDay };
                yield return new object[] { "en-US", CalendarWeekRule.FirstDay };
                yield return new object[] { "es-419", CalendarWeekRule.FirstDay };
                yield return new object[] { "es-ES", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "es-MX", CalendarWeekRule.FirstDay };
                yield return new object[] { "et-EE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "fa-IR", CalendarWeekRule.FirstDay };
                yield return new object[] { "fi-FI", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "fil-PH", CalendarWeekRule.FirstDay };
                yield return new object[] { "fr-BE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "fr-CA", CalendarWeekRule.FirstDay };
                yield return new object[] { "fr-CH", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "fr-FR", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "gu-IN", CalendarWeekRule.FirstDay };
                yield return new object[] { "he-IL", CalendarWeekRule.FirstDay };
                yield return new object[] { "hi-IN", CalendarWeekRule.FirstDay };
                yield return new object[] { "hr-BA", CalendarWeekRule.FirstDay };
                yield return new object[] { "hr-HR", CalendarWeekRule.FirstDay };
                yield return new object[] { "hu-HU", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "id-ID", CalendarWeekRule.FirstDay };
                yield return new object[] { "it-CH", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "it-IT", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "ja-JP", CalendarWeekRule.FirstDay };
                yield return new object[] { "kn-IN", CalendarWeekRule.FirstDay };
                yield return new object[] { "ko-KR", CalendarWeekRule.FirstDay };
                yield return new object[] { "lt-LT", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "lv-LV", CalendarWeekRule.FirstDay };
                yield return new object[] { "ml-IN", CalendarWeekRule.FirstDay };
                yield return new object[] { "mr-IN", CalendarWeekRule.FirstDay };
                yield return new object[] { "ms-BN", CalendarWeekRule.FirstDay };
                yield return new object[] { "ms-MY", CalendarWeekRule.FirstDay };
                yield return new object[] { "ms-SG", CalendarWeekRule.FirstDay };
                yield return new object[] { "nb-NO", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "no-NO", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "nl-AW", CalendarWeekRule.FirstDay };
                yield return new object[] { "nl-BE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "nl-NL", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "pl-PL", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "pt-BR", CalendarWeekRule.FirstDay };
                yield return new object[] { "pt-PT", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "ro-RO", CalendarWeekRule.FirstDay };
                yield return new object[] { "ru-RU", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "sk-SK", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "sl-SI", CalendarWeekRule.FirstDay };
                yield return new object[] { "sr-Cyrl-RS", CalendarWeekRule.FirstDay };
                yield return new object[] { "sr-Latn-RS", CalendarWeekRule.FirstDay };
                yield return new object[] { "sv-AX", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "sv-SE", CalendarWeekRule.FirstFourDayWeek };
                yield return new object[] { "sw-CD", CalendarWeekRule.FirstDay };
                yield return new object[] { "sw-KE", CalendarWeekRule.FirstDay };
                yield return new object[] { "sw-TZ", CalendarWeekRule.FirstDay };
                yield return new object[] { "sw-UG", CalendarWeekRule.FirstDay };
                yield return new object[] { "ta-IN", CalendarWeekRule.FirstDay };
                yield return new object[] { "ta-LK", CalendarWeekRule.FirstDay };
                yield return new object[] { "ta-MY", CalendarWeekRule.FirstDay };
                yield return new object[] { "ta-SG", CalendarWeekRule.FirstDay };
                yield return new object[] { "te-IN", CalendarWeekRule.FirstDay };
                yield return new object[] { "th-TH", CalendarWeekRule.FirstDay };
                yield return new object[] { "tr-CY", CalendarWeekRule.FirstDay };
                yield return new object[] { "tr-TR", CalendarWeekRule.FirstDay };
                yield return new object[] { "uk-UA", CalendarWeekRule.FirstDay };
                yield return new object[] { "vi-VN", CalendarWeekRule.FirstDay };
                yield return new object[] { "zh-CN", CalendarWeekRule.FirstDay };
                yield return new object[] { "zh-Hans-HK", CalendarWeekRule.FirstDay };
                yield return new object[] { "zh-SG", CalendarWeekRule.FirstDay };
                yield return new object[] { "zh-HK", CalendarWeekRule.FirstDay };
                yield return new object[] { "zh-TW", CalendarWeekRule.FirstDay };
            }
        }

        [Theory]
        [MemberData(nameof(CalendarWeekRule_Get_TestData))]
        public void CalendarWeekRuleTest(string cultureName, CalendarWeekRule expected)
        {
            var format = cultureName == "invariant" ? DateTimeFormatInfo.InvariantInfo : new CultureInfo(cultureName).DateTimeFormat;
            Assert.True(expected == format.CalendarWeekRule, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {format.CalendarWeekRule}");
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
