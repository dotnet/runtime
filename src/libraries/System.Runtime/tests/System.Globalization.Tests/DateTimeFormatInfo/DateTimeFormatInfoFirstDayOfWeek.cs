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
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, DayOfWeek.Sunday, "invariant" };
            yield return new object[] { new CultureInfo("en-US", false).DateTimeFormat, DayOfWeek.Sunday, "en-US" };
            yield return new object[] { new CultureInfo("fr-FR", false).DateTimeFormat, DayOfWeek.Monday, "fr-FR" };
        }

        public static IEnumerable<object[]> FirstDayOfWeek_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { "ar-SA", DayOfWeek.Sunday };
            yield return new object[] { "am-ET", DayOfWeek.Sunday };
            yield return new object[] { "bg-BG", DayOfWeek.Monday };
            yield return new object[] { "bn-BD", DayOfWeek.Sunday };
            yield return new object[] { "bn-IN", DayOfWeek.Sunday };
            yield return new object[] { "ca-AD", DayOfWeek.Monday };
            yield return new object[] { "ca-ES", DayOfWeek.Monday };
            yield return new object[] { "cs-CZ", DayOfWeek.Monday };
            yield return new object[] { "da-DK", DayOfWeek.Monday };
            yield return new object[] { "de-AT", DayOfWeek.Monday };
            yield return new object[] { "de-BE", DayOfWeek.Monday };
            yield return new object[] { "de-CH", DayOfWeek.Monday };
            yield return new object[] { "de-DE", DayOfWeek.Monday };
            yield return new object[] { "de-IT", DayOfWeek.Monday };
            yield return new object[] { "de-LI", DayOfWeek.Monday };
            yield return new object[] { "de-LU", DayOfWeek.Monday };
            yield return new object[] { "el-CY", DayOfWeek.Monday };
            yield return new object[] { "el-GR", DayOfWeek.Monday };
            yield return new object[] { "en-AE", DayOfWeek.Saturday };
            yield return new object[] { "en-AG", DayOfWeek.Sunday };
            yield return new object[] { "en-AI", DayOfWeek.Monday };
            yield return new object[] { "en-AS", DayOfWeek.Sunday };
            yield return new object[] { "en-AT", DayOfWeek.Monday };
            yield return new object[] { "en-AU", DayOfWeek.Monday }; // originally in ICU: Sunday, even though ISO 8601 states: Monday
            yield return new object[] { "en-BB", DayOfWeek.Monday };
            yield return new object[] { "en-BE", DayOfWeek.Monday };
            yield return new object[] { "en-BI", DayOfWeek.Monday };
            yield return new object[] { "en-BM", DayOfWeek.Monday };
            yield return new object[] { "en-BS", DayOfWeek.Sunday };
            yield return new object[] { "en-BW", DayOfWeek.Sunday };
            yield return new object[] { "en-BZ", DayOfWeek.Sunday };
            yield return new object[] { "en-CA", DayOfWeek.Sunday };
            yield return new object[] { "en-CC", DayOfWeek.Monday };
            yield return new object[] { "en-CH", DayOfWeek.Monday };
            yield return new object[] { "en-CK", DayOfWeek.Monday };
            yield return new object[] { "en-CM", DayOfWeek.Monday };
            yield return new object[] { "en-CX", DayOfWeek.Monday };
            yield return new object[] { "en-CY", DayOfWeek.Monday };
            yield return new object[] { "en-DE", DayOfWeek.Monday };
            yield return new object[] { "en-DK", DayOfWeek.Monday };
            yield return new object[] { "en-DM", DayOfWeek.Sunday };
            yield return new object[] { "en-ER", DayOfWeek.Monday };
            yield return new object[] { "en-FI", DayOfWeek.Monday };
            yield return new object[] { "en-FJ", DayOfWeek.Monday };
            yield return new object[] { "en-FK", DayOfWeek.Monday };
            yield return new object[] { "en-FM", DayOfWeek.Monday };
            yield return new object[] { "en-GB", DayOfWeek.Monday };
            yield return new object[] { "en-GD", DayOfWeek.Monday };
            yield return new object[] { "en-GG", DayOfWeek.Monday };
            yield return new object[] { "en-GH", DayOfWeek.Monday };
            yield return new object[] { "en-GI", DayOfWeek.Monday };
            yield return new object[] { "en-GM", DayOfWeek.Monday };
            yield return new object[] { "en-GU", DayOfWeek.Sunday };
            yield return new object[] { "en-GY", DayOfWeek.Monday };
            yield return new object[] { "en-HK", DayOfWeek.Sunday };
            yield return new object[] { "en-IE", DayOfWeek.Monday };
            yield return new object[] { "en-IL", DayOfWeek.Sunday };
            yield return new object[] { "en-IM", DayOfWeek.Monday };
            yield return new object[] { "en-IN", DayOfWeek.Sunday };
            yield return new object[] { "en-IO", DayOfWeek.Monday };
            yield return new object[] { "en-JE", DayOfWeek.Monday };
            yield return new object[] { "en-JM", DayOfWeek.Sunday };
            yield return new object[] { "en-KE", DayOfWeek.Sunday };
            yield return new object[] { "en-KI", DayOfWeek.Monday };
            yield return new object[] { "en-KN", DayOfWeek.Monday };
            yield return new object[] { "en-KY", DayOfWeek.Monday };
            yield return new object[] { "en-LC", DayOfWeek.Monday };
            yield return new object[] { "en-LR", DayOfWeek.Monday };
            yield return new object[] { "en-LS", DayOfWeek.Monday };
            yield return new object[] { "en-MG", DayOfWeek.Monday };
            yield return new object[] { "en-MH", DayOfWeek.Sunday };
            yield return new object[] { "en-MO", DayOfWeek.Sunday };
            yield return new object[] { "en-MP", DayOfWeek.Monday };
            yield return new object[] { "en-MS", DayOfWeek.Monday };
            yield return new object[] { "en-MT", DayOfWeek.Sunday };
            yield return new object[] { "en-MU", DayOfWeek.Monday };
            yield return new object[] { "en-MW", DayOfWeek.Monday };
            yield return new object[] { "en-MY", DayOfWeek.Monday };
            yield return new object[] { "en-NA", DayOfWeek.Monday };
            yield return new object[] { "en-NF", DayOfWeek.Monday };
            yield return new object[] { "en-NG", DayOfWeek.Monday };
            yield return new object[] { "en-NL", DayOfWeek.Monday };
            yield return new object[] { "en-NR", DayOfWeek.Monday };
            yield return new object[] { "en-NU", DayOfWeek.Monday };
            yield return new object[] { "en-NZ", DayOfWeek.Monday };
            yield return new object[] { "en-PG", DayOfWeek.Monday };
            yield return new object[] { "en-PH", DayOfWeek.Sunday };
            yield return new object[] { "en-PK", DayOfWeek.Sunday };
            yield return new object[] { "en-PN", DayOfWeek.Monday };
            yield return new object[] { "en-PR", DayOfWeek.Sunday };
            yield return new object[] { "en-PW", DayOfWeek.Monday };
            yield return new object[] { "en-RW", DayOfWeek.Monday };
            yield return new object[] { "en-SB", DayOfWeek.Monday };
            yield return new object[] { "en-SC", DayOfWeek.Monday };
            yield return new object[] { "en-SD", DayOfWeek.Saturday };
            yield return new object[] { "en-SE", DayOfWeek.Monday };
            yield return new object[] { "en-SG", DayOfWeek.Sunday };
            yield return new object[] { "en-SH", DayOfWeek.Monday };
            yield return new object[] { "en-SI", DayOfWeek.Monday };
            yield return new object[] { "en-SL", DayOfWeek.Monday };
            yield return new object[] { "en-SS", DayOfWeek.Monday };
            yield return new object[] { "en-SX", DayOfWeek.Monday };
            yield return new object[] { "en-SZ", DayOfWeek.Monday };
            yield return new object[] { "en-TC", DayOfWeek.Monday };
            yield return new object[] { "en-TK", DayOfWeek.Monday };
            yield return new object[] { "en-TO", DayOfWeek.Monday };
            yield return new object[] { "en-TT", DayOfWeek.Sunday };
            yield return new object[] { "en-TV", DayOfWeek.Monday };
            yield return new object[] { "en-TZ", DayOfWeek.Monday };
            yield return new object[] { "en-UG", DayOfWeek.Monday };
            yield return new object[] { "en-UM", DayOfWeek.Sunday };
            yield return new object[] { "en-VC", DayOfWeek.Monday };
            yield return new object[] { "en-VG", DayOfWeek.Monday };
            yield return new object[] { "en-VI", DayOfWeek.Sunday };
            yield return new object[] { "en-VU", DayOfWeek.Monday };
            yield return new object[] { "en-WS", DayOfWeek.Sunday };
            yield return new object[] { "en-ZA", DayOfWeek.Sunday };
            yield return new object[] { "en-ZM", DayOfWeek.Monday };
            yield return new object[] { "en-ZW", DayOfWeek.Sunday };
            yield return new object[] { "en-US", DayOfWeek.Sunday };
            yield return new object[] { "es-419", DayOfWeek.Monday };
            yield return new object[] { "es-ES", DayOfWeek.Monday };
            yield return new object[] { "es-MX", DayOfWeek.Sunday };
            yield return new object[] { "et-EE", DayOfWeek.Monday };
            yield return new object[] { "fa-IR", DayOfWeek.Saturday };
            yield return new object[] { "fi-FI", DayOfWeek.Monday };
            yield return new object[] { "fil-PH", DayOfWeek.Sunday };
            yield return new object[] { "fr-BE", DayOfWeek.Monday };
            yield return new object[] { "fr-CA", DayOfWeek.Sunday };
            yield return new object[] { "fr-CH", DayOfWeek.Monday };
            yield return new object[] { "fr-FR", DayOfWeek.Monday };
            yield return new object[] { "gu-IN", DayOfWeek.Sunday };
            yield return new object[] { "he-IL", DayOfWeek.Sunday };
            yield return new object[] { "hi-IN", DayOfWeek.Sunday };
            yield return new object[] { "hr-BA", DayOfWeek.Monday };
            yield return new object[] { "hr-HR", DayOfWeek.Monday };
            yield return new object[] { "hu-HU", DayOfWeek.Monday };
            yield return new object[] { "id-ID", DayOfWeek.Sunday };
            yield return new object[] { "it-CH", DayOfWeek.Monday };
            yield return new object[] { "it-IT", DayOfWeek.Monday };
            yield return new object[] { "ja-JP", DayOfWeek.Sunday };
            yield return new object[] { "kn-IN", DayOfWeek.Sunday };
            yield return new object[] { "ko-KR", DayOfWeek.Sunday };
            yield return new object[] { "lt-LT", DayOfWeek.Monday };
            yield return new object[] { "lv-LV", DayOfWeek.Monday };
            yield return new object[] { "ml-IN", DayOfWeek.Sunday };
            yield return new object[] { "mr-IN", DayOfWeek.Sunday };
            yield return new object[] { "ms-BN", DayOfWeek.Monday };
            yield return new object[] { "ms-MY", DayOfWeek.Monday };
            yield return new object[] { "ms-SG", DayOfWeek.Sunday };
            yield return new object[] { "nb-NO", DayOfWeek.Monday };
            yield return new object[] { "no", DayOfWeek.Monday };
            yield return new object[] { "no-NO", DayOfWeek.Monday };
            yield return new object[] { "nl-AW", DayOfWeek.Monday };
            yield return new object[] { "nl-BE", DayOfWeek.Monday };
            yield return new object[] { "nl-NL", DayOfWeek.Monday };
            yield return new object[] { "pl-PL", DayOfWeek.Monday };
            yield return new object[] { "pt-BR", DayOfWeek.Sunday };
            yield return new object[] { "pt-PT", DayOfWeek.Sunday };
            yield return new object[] { "ro-RO", DayOfWeek.Monday };
            yield return new object[] { "ru-RU", DayOfWeek.Monday };
            yield return new object[] { "sk-SK", DayOfWeek.Monday };
            yield return new object[] { "sl-SI", DayOfWeek.Monday };
            yield return new object[] { "sr-Cyrl-RS", DayOfWeek.Monday };
            yield return new object[] { "sr-Latn-RS", DayOfWeek.Monday };
            yield return new object[] { "sv-AX", DayOfWeek.Monday };
            yield return new object[] { "sv-SE", DayOfWeek.Monday };
            yield return new object[] { "sw-CD", DayOfWeek.Monday };
            yield return new object[] { "sw-KE", DayOfWeek.Sunday };
            yield return new object[] { "sw-TZ", DayOfWeek.Monday };
            yield return new object[] { "sw-UG", DayOfWeek.Monday };
            yield return new object[] { "ta-IN", DayOfWeek.Sunday };
            yield return new object[] { "ta-LK", DayOfWeek.Monday };
            yield return new object[] { "ta-MY", DayOfWeek.Monday };
            yield return new object[] { "ta-SG", DayOfWeek.Sunday };
            yield return new object[] { "te-IN", DayOfWeek.Sunday };
            yield return new object[] { "th-TH", DayOfWeek.Sunday };
            yield return new object[] { "tr-CY", DayOfWeek.Monday };
            yield return new object[] { "tr-TR", DayOfWeek.Monday };
            yield return new object[] { "uk-UA", DayOfWeek.Monday };
            yield return new object[] { "vi-VN", DayOfWeek.Monday };
            yield return new object[] { "zh-CN", DayOfWeek.Monday };
            yield return new object[] { "zh-Hans-HK", DayOfWeek.Sunday };
            yield return new object[] { "zh-SG", DayOfWeek.Sunday };
            yield return new object[] { "zh-HK", DayOfWeek.Sunday };
            yield return new object[] { "zh-TW", DayOfWeek.Sunday };
        }

        [Theory]
        [MemberData(nameof(FirstDayOfWeek_Get_TestData))]
        public void FirstDayOfWeek(DateTimeFormatInfo format, DayOfWeek expected, string cultureName)
        {
            Assert.True(expected == format.FirstDayOfWeek, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {format.FirstDayOfWeek}");
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(FirstDayOfWeek_Get_TestData_HybridGlobalization))]
        public void FirstDayOfWeekHybridGlobalization(string culture, DayOfWeek expected)
        {
            DateTimeFormatInfo format = new CultureInfo(culture).DateTimeFormat;
            FirstDayOfWeek(format, expected, culture);
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
