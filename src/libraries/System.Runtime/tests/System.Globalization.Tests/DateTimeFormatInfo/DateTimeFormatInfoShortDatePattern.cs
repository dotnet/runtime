// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortDatePattern
    {
        public static IEnumerable<object[]> ShortDatePattern_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { "ar-SA", "d\u200f/M\u200f/yyyy" }; // "d\u200f/M\u200f/yyyy g"
            yield return new object[] { "am-ET", "dd/MM/yyyy" };
            yield return new object[] { "bg-BG", "d.MM.yyyy г." }; // "d.MM.yyyy 'г'."
            yield return new object[] { "bn-BD", "d/M/yyyy" };
            yield return new object[] { "bn-IN", "d/M/yyyy" };
            yield return new object[] { "ca-AD", "d/M/yyyy" };
            yield return new object[] { "ca-ES", "d/M/yyyy" };
            yield return new object[] { "cs-CZ", "dd.MM.yyyy" };
            yield return new object[] { "da-DK", "dd.MM.yyyy" };
            yield return new object[] { "de-AT", "dd.MM.yyyy" };
            yield return new object[] { "de-BE", "dd.MM.yyyy" };
            yield return new object[] { "de-CH", "dd.MM.yyyy" };
            yield return new object[] { "de-DE", "dd.MM.yyyy" };
            yield return new object[] { "de-IT", "dd.MM.yyyy" };
            yield return new object[] { "de-LI", "dd.MM.yyyy" };
            yield return new object[] { "de-LU", "dd.MM.yyyy" };
            yield return new object[] { "el-CY", "d/M/yyyy" };
            yield return new object[] { "el-GR", "d/M/yyyy" };
            yield return new object[] { "en-AE", "dd/MM/yyyy" };
            yield return new object[] { "en-AG", "dd/MM/yyyy" };
            yield return new object[] { "en-AI", "dd/MM/yyyy" };
            yield return new object[] { "en-AS", "M/d/yyyy" };
            yield return new object[] { "en-AT", "dd/MM/yyyy" };
            yield return new object[] { "en-AU", "d/M/yyyy" };
            yield return new object[] { "en-BB", "dd/MM/yyyy" };
            yield return new object[] { "en-BE", "dd/MM/yyyy" };
            yield return new object[] { "en-BI", "M/d/yyyy" };
            yield return new object[] { "en-BM", "dd/MM/yyyy" };
            yield return new object[] { "en-BS", "dd/MM/yyyy" };
            yield return new object[] { "en-BW", "dd/MM/yyyy" };
            yield return new object[] { "en-BZ", "dd/MM/yyyy" };
            yield return new object[] { "en-CA", "yyyy-MM-dd" };
            yield return new object[] { "en-CC", "dd/MM/yyyy" };
            yield return new object[] { "en-CH", "dd.MM.yyyy" }; // "dd/MM/yyyy"
            yield return new object[] { "en-CK", "dd/MM/yyyy" };
            yield return new object[] { "en-CM", "dd/MM/yyyy" };
            yield return new object[] { "en-CX", "dd/MM/yyyy" };
            yield return new object[] { "en-CY", "dd/MM/yyyy" };
            yield return new object[] { "en-DE", "dd/MM/yyyy" };
            yield return new object[] { "en-DK", "dd/MM/yyyy" };
            yield return new object[] { "en-DM", "dd/MM/yyyy" };
            yield return new object[] { "en-ER", "dd/MM/yyyy" };
            yield return new object[] { "en-FI", "dd/MM/yyyy" };
            yield return new object[] { "en-FJ", "dd/MM/yyyy" };
            yield return new object[] { "en-FK", "dd/MM/yyyy" };
            yield return new object[] { "en-FM", "dd/MM/yyyy" };
            yield return new object[] { "en-GB", "dd/MM/yyyy" };
            yield return new object[] { "en-GD", "dd/MM/yyyy" };
            yield return new object[] { "en-GG", "dd/MM/yyyy" };
            yield return new object[] { "en-GH", "dd/MM/yyyy" };
            yield return new object[] { "en-GI", "dd/MM/yyyy" };
            yield return new object[] { "en-GM", "dd/MM/yyyy" };
            yield return new object[] { "en-GU", "M/d/yyyy" };
            yield return new object[] { "en-GY", "dd/MM/yyyy" };
            yield return new object[] { "en-HK", "d/M/yyyy" };
            yield return new object[] { "en-IE", "dd/MM/yyyy" };
            yield return new object[] { "en-IL", "dd/MM/yyyy" };
            yield return new object[] { "en-IM", "dd/MM/yyyy" };
            yield return new object[] { "en-IN", "dd/MM/yyyy" };
            yield return new object[] { "en-IO", "dd/MM/yyyy" };
            yield return new object[] { "en-JE", "dd/MM/yyyy" };
            yield return new object[] { "en-JM", "dd/MM/yyyy" };
            yield return new object[] { "en-KE", "dd/MM/yyyy" };
            yield return new object[] { "en-KI", "dd/MM/yyyy" };
            yield return new object[] { "en-KN", "dd/MM/yyyy" };
            yield return new object[] { "en-KY", "dd/MM/yyyy" };
            yield return new object[] { "en-LC", "dd/MM/yyyy" };
            yield return new object[] { "en-LR", "dd/MM/yyyy" };
            yield return new object[] { "en-LS", "dd/MM/yyyy" };
            yield return new object[] { "en-MG", "dd/MM/yyyy" };
            yield return new object[] { "en-MH", "M/d/yyyy" };
            yield return new object[] { "en-MO", "dd/MM/yyyy" };
            yield return new object[] { "en-MP", "M/d/yyyy" };
            yield return new object[] { "en-MS", "dd/MM/yyyy" };
            yield return new object[] { "en-MT", "dd/MM/yyyy" };
            yield return new object[] { "en-MU", "dd/MM/yyyy" };
            yield return new object[] { "en-MW", "dd/MM/yyyy" };
            yield return new object[] { "en-MY", "dd/MM/yyyy" };
            yield return new object[] { "en-NA", "dd/MM/yyyy" };
            yield return new object[] { "en-NF", "dd/MM/yyyy" };
            yield return new object[] { "en-NG", "dd/MM/yyyy" };
            yield return new object[] { "en-NL", "dd/MM/yyyy" };
            yield return new object[] { "en-NR", "dd/MM/yyyy" };
            yield return new object[] { "en-NU", "dd/MM/yyyy" };
            yield return new object[] { "en-NZ", "d/MM/yyyy" };
            yield return new object[] { "en-PG", "dd/MM/yyyy" };
            yield return new object[] { "en-PH", "M/d/yyyy" }; // "dd/MM/yyyy"
            yield return new object[] { "en-PK", "dd/MM/yyyy" };
            yield return new object[] { "en-PN", "dd/MM/yyyy" };
            yield return new object[] { "en-PR", "M/d/yyyy" };
            yield return new object[] { "en-PW", "dd/MM/yyyy" };
            yield return new object[] { "en-RW", "dd/MM/yyyy" };
            yield return new object[] { "en-SB", "dd/MM/yyyy" };
            yield return new object[] { "en-SC", "dd/MM/yyyy" };
            yield return new object[] { "en-SD", "dd/MM/yyyy" };
            yield return new object[] { "en-SE", "yyyy-MM-dd" };
            yield return new object[] { "en-SG", "d/M/yyyy" };
            yield return new object[] { "en-SH", "dd/MM/yyyy" };
            yield return new object[] { "en-SI", "dd/MM/yyyy" };
            yield return new object[] { "en-SL", "dd/MM/yyyy" };
            yield return new object[] { "en-SS", "dd/MM/yyyy" };
            yield return new object[] { "en-SX", "dd/MM/yyyy" };
            yield return new object[] { "en-SZ", "dd/MM/yyyy" };
            yield return new object[] { "en-TC", "dd/MM/yyyy" };
            yield return new object[] { "en-TK", "dd/MM/yyyy" };
            yield return new object[] { "en-TO", "dd/MM/yyyy" };
            yield return new object[] { "en-TT", "dd/MM/yyyy" };
            yield return new object[] { "en-TV", "dd/MM/yyyy" };
            yield return new object[] { "en-TZ", "dd/MM/yyyy" };
            yield return new object[] { "en-UG", "dd/MM/yyyy" };
            yield return new object[] { "en-UM", "M/d/yyyy" };
            yield return new object[] { "en-US", "M/d/yyyy" };
            yield return new object[] { "en-VC", "dd/MM/yyyy" };
            yield return new object[] { "en-VG", "dd/MM/yyyy" };
            yield return new object[] { "en-VI", "M/d/yyyy" };
            yield return new object[] { "en-VU", "dd/MM/yyyy" };
            yield return new object[] { "en-WS", "dd/MM/yyyy" };
            yield return new object[] { "en-ZA", "yyyy/MM/dd" };
            yield return new object[] { "en-ZM", "dd/MM/yyyy" };
            yield return new object[] { "en-ZW", "d/M/yyyy" };
            yield return new object[] { "en-US", "M/d/yyyy" };
            yield return new object[] { "es-419", "d/M/yyyy" };
            yield return new object[] { "es-ES", "d/M/yyyy" };
            yield return new object[] { "es-MX", "dd/MM/yyyy" };
            yield return new object[] { "et-EE", "dd.MM.yyyy" };
            yield return new object[] { "fa-IR", "yyyy/M/d" }; // "yyyy/M/d"
            yield return new object[] { "fi-FI", "d.M.yyyy" };
            yield return new object[] { "fil-PH", "M/d/yyyy" };
            yield return new object[] { "fr-BE", "d/MM/yyyy" };
            yield return new object[] { "fr-CA", "yyyy-MM-dd" };
            yield return new object[] { "fr-CH", "dd.MM.yyyy" };
            yield return new object[] { "fr-FR", "dd/MM/yyyy" };
            yield return new object[] { "gu-IN", "d/M/yyyy" };
            yield return new object[] { "he-IL", "d.M.yyyy" };
            yield return new object[] { "hi-IN", "d/M/yyyy" };
            yield return new object[] { "hr-BA", "d. M. yyyy." };
            yield return new object[] { "hr-HR", "dd. MM. yyyy." };
            yield return new object[] { "hu-HU", "yyyy. MM. dd." };
            yield return new object[] { "id-ID", "dd/MM/yyyy" };
            yield return new object[] { "it-CH", "dd.MM.yyyy" };
            yield return new object[] { "it-IT", "dd/MM/yyyy" };
            yield return new object[] { "ja-JP", "yyyy/MM/dd" };
            yield return new object[] { "kn-IN", "d/M/yyyy" };
            yield return new object[] { "ko-KR", "yyyy. M. d." };
            yield return new object[] { "lt-LT", "yyyy-MM-dd" };
            yield return new object[] { "lv-LV", "dd.MM.yyyy" };
            yield return new object[] { "ml-IN", "d/M/yyyy" };
            yield return new object[] { "mr-IN", "d/M/yyyy" };
            yield return new object[] { "ms-BN", "d/MM/yyyy" };
            yield return new object[] { "ms-MY", "d/MM/yyyy" };
            yield return new object[] { "ms-SG", "d/MM/yyyy" };
            yield return new object[] { "nb-NO", "dd.MM.yyyy" };
            yield return new object[] { "no", "dd.MM.yyyy" };
            yield return new object[] { "no-NO", "dd.MM.yyyy" };
            yield return new object[] { "nl-AW", "dd-MM-yyyy" };
            yield return new object[] { "nl-BE", "d/MM/yyyy" };
            yield return new object[] { "nl-NL", "dd-MM-yyyy" };
            yield return new object[] { "pl-PL", "d.MM.yyyy" }; // "dd.MM.yyyy"
            yield return new object[] { "pt-BR", "dd/MM/yyyy" };
            yield return new object[] { "pt-PT", "dd/MM/yyyy" };
            yield return new object[] { "ro-RO", "dd.MM.yyyy" };
            yield return new object[] { "ru-RU", "dd.MM.yyyy" };
            yield return new object[] { "sk-SK", "d. M. yyyy" };
            yield return new object[] { "sl-SI", "d. MM. yyyy" };
            yield return new object[] { "sr-Cyrl-RS", "d.M.yyyy." };
            yield return new object[] { "sr-Latn-RS", "d.M.yyyy." };
            yield return new object[] { "sv-AX", "yyyy-MM-dd" };
            yield return new object[] { "sv-SE", "yyyy-MM-dd" };
            yield return new object[] { "sw-CD", "dd/MM/yyyy" };
            yield return new object[] { "sw-KE", "dd/MM/yyyy" };
            yield return new object[] { "sw-TZ", "dd/MM/yyyy" };
            yield return new object[] { "sw-UG", "dd/MM/yyyy" };
            yield return new object[] { "ta-IN", "d/M/yyyy" };
            yield return new object[] { "ta-LK", "d/M/yyyy" };
            yield return new object[] { "ta-MY", "d/M/yyyy" };
            yield return new object[] { "ta-SG", "d/M/yyyy" };
            yield return new object[] { "te-IN", "dd-MM-yyyy" };
            yield return new object[] { "th-TH", "d/M/yyyy" };
            yield return new object[] { "tr-CY", "d.MM.yyyy" };
            yield return new object[] { "tr-TR", "d.MM.yyyy" };
            yield return new object[] { "uk-UA", "dd.MM.yyyy" };
            yield return new object[] { "vi-VN", "dd/MM/yyyy" };
            yield return new object[] { "zh-CN", "yyyy/M/d" };
            yield return new object[] { "zh-Hans-HK", "d/M/yyyy" };
            yield return new object[] { "zh-SG", "dd/MM/yyyy" };
            yield return new object[] { "zh-HK", "d/M/yyyy" };
            yield return new object[] { "zh-TW", "yyyy/M/d" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(ShortDatePattern_Get_TestData_HybridGlobalization))]
        public void ShortDatePattern_Get_ReturnsExpected_HybridGlobalization(string cultureName, string expected)
        {
            var format = new CultureInfo(cultureName).DateTimeFormat;
            Assert.True(expected == format.ShortDatePattern, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {format.ShortDatePattern}");
        }

        [Fact]
        public void ShortDatePattern_InvariantInfo()
        {
            Assert.Equal("MM/dd/yyyy", DateTimeFormatInfo.InvariantInfo.ShortDatePattern);
        }

        public static IEnumerable<object[]> ShortDatePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "MM/dd/yyyy" };
            yield return new object[] { "MM-DD-yyyy" };
            yield return new object[] { "d" };
        }

        [Theory]
        [MemberData(nameof(ShortDatePattern_Set_TestData))]
        public void ShortDatePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.ShortDatePattern = value;
            Assert.Equal(value, format.ShortDatePattern);
        }

        [Fact]
        public void ShortDatePattern_Set_InvalidatesDerivedPatterns()
        {
            const string Pattern = "#$";
            var format = new DateTimeFormatInfo();
            var d = DateTimeOffset.Now;
            d.ToString("G", format); // GeneralLongTimePattern
            d.ToString("g", format); // GeneralShortTimePattern
            d.ToString(format); // DateTimeOffsetPattern
            format.ShortDatePattern = Pattern;
            Assert.Contains(Pattern, d.ToString("G", format));
            Assert.Contains(Pattern, d.ToString("g", format));
            Assert.Contains(Pattern, d.ToString(format));
        }

        [Fact]
        public void ShortDatePattern_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.ShortDatePattern = null);
        }

        [Fact]
        public void ShortDatePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortDatePattern = "MM/dd/yyyy");
        }
    }
}
