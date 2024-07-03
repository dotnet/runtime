// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoLongDatePattern
    {
        [Fact]
        public void LongDatePattern_InvariantInfo_ReturnsExpected()
        {
            Assert.Equal("dddd, dd MMMM yyyy", DateTimeFormatInfo.InvariantInfo.LongDatePattern);
        }

        public static IEnumerable<object[]> LongDatePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "dddd" };
            yield return new object[] { "D" };
            yield return new object[] { "HH:mm:ss dddd, dd MMMM yyyy" };
            yield return new object[] { "dddd, dd MMMM yyyy" };
        }

        public static IEnumerable<object[]> LongDatePattern_Get_TestData_ICU()
        {
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "dddd, MMMM d, yyyy", "en-US" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat,  "dddd d MMMM yyyy", "fr-FR" };
        }

        public static IEnumerable<object[]> LongDatePattern_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] {"ar-SA", "dddd، d MMMM yyyy" }; // dddd، d MMMM yyyy g
            yield return new object[] {"am-ET", "yyyy MMMM d, dddd" };
            yield return new object[] {"bg-BG", "dddd, d MMMM yyyy 'г'." };
            yield return new object[] {"bn-BD", "dddd, d MMMM, yyyy" };
            yield return new object[] {"bn-IN", "dddd, d MMMM, yyyy" };
            string catalanianPattern = PlatformDetection.IsFirefox || PlatformDetection.IsNodeJS ? "dddd, d 'de' MMMM 'de' yyyy" : "dddd, d 'de' MMMM 'del' yyyy"; // "dddd, d MMMM 'de' yyyy"
            yield return new object[] {"ca-AD", catalanianPattern };
            yield return new object[] {"ca-ES", catalanianPattern };
            yield return new object[] {"cs-CZ", "dddd d. MMMM yyyy" };
            yield return new object[] {"da-DK", "dddd 'den' d. MMMM yyyy" };
            yield return new object[] {"de-AT", "dddd, d. MMMM yyyy" };
            yield return new object[] {"de-BE", "dddd, d. MMMM yyyy" };
            yield return new object[] {"de-CH", "dddd, d. MMMM yyyy" };
            yield return new object[] {"de-DE", "dddd, d. MMMM yyyy" };
            yield return new object[] {"de-IT", "dddd, d. MMMM yyyy" };
            yield return new object[] {"de-LI", "dddd, d. MMMM yyyy" };
            yield return new object[] {"de-LU", "dddd, d. MMMM yyyy" };
            yield return new object[] {"el-CY", "dddd d MMMM yyyy" }; // "dddd, d MMMM yyyy"
            yield return new object[] {"el-GR", "dddd d MMMM yyyy" }; // "dddd, d MMMM yyyy"
            yield return new object[] {"en-AE", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-AG", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-AI", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-AS", "dddd, MMMM d, yyyy" };
            yield return new object[] {"en-AT", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-AU", PlatformDetection.IsFirefox || PlatformDetection.IsNodeJS ? "dddd, d MMMM yyyy" : "dddd d MMMM yyyy" };
            yield return new object[] {"en-BB", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-BE", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-BI", "dddd, MMMM d, yyyy" };
            yield return new object[] {"en-BM", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-BS", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-BW", "dddd, d MMMM yyyy" }; // "dddd, dd MMMM yyyy"
            yield return new object[] {"en-BZ", "dddd, d MMMM yyyy" }; // "dddd, dd MMMM yyyy"
            yield return new object[] {"en-CA", "dddd, MMMM d, yyyy" };
            yield return new object[] {"en-CC", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-CH", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-CK", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-CM", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-CX", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-CY", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-DE", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-DK", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-DM", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-ER", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-FI", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-FJ", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-FK", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-FM", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-GB", PlatformDetection.IsFirefox || PlatformDetection.IsNodeJS ? "dddd, d MMMM yyyy" :"dddd d MMMM yyyy" };
            yield return new object[] {"en-GD", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-GG", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-GH", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-GI", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-GM", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-GU", "dddd, MMMM d, yyyy" };
            yield return new object[] {"en-GY", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-HK", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-IE", "dddd d MMMM yyyy" };
            yield return new object[] {"en-IL", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-IM", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-IN", PlatformDetection.IsFirefox || PlatformDetection.IsNodeJS ? "dddd, d MMMM, yyyy" : "dddd d MMMM, yyyy" }; // dddd, d MMMM, yyyy
            yield return new object[] {"en-IO", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-JE", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-JM", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-KE", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-KI", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-KN", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-KY", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-LC", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-LR", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-LS", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-MG", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-MH", "dddd, MMMM d, yyyy" };
            yield return new object[] {"en-MO", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-MP", "dddd, MMMM d, yyyy" };
            yield return new object[] {"en-MS", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-MT", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-MU", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-MW", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-MY", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-NA", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-NF", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-NG", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-NL", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-NR", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-NU", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-NZ", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-PG", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-PH", "dddd, MMMM d, yyyy" }; // "dddd, d MMMM yyyy"
            yield return new object[] {"en-PK", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-PN", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-PR", "dddd, MMMM d, yyyy" };
            yield return new object[] {"en-PW", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-RW", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SB", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SC", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SD", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SE", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SG", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SH", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SI", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SL", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SS", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SX", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-SZ", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-TC", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-TK", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-TO", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-TT", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-TV", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-TZ", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-UG", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-UM", "dddd, MMMM d, yyyy" };
            yield return new object[] {"en-US", "dddd, MMMM d, yyyy" };
            yield return new object[] {"en-VC", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-VG", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-VI", "dddd, MMMM d, yyyy" };
            yield return new object[] {"en-VU", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-WS", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-ZA", "dddd, d MMMM yyyy" }; // "dddd, dd MMMM yyyy"
            yield return new object[] {"en-ZM", "dddd, d MMMM yyyy" };
            yield return new object[] {"en-ZW", "dddd, d MMMM yyyy" }; // "dddd, dd MMMM yyyy"
            yield return new object[] {"en-US", "dddd, MMMM d, yyyy" };
            string spanishPattern = "dddd, d 'de' MMMM 'de' yyyy";
            yield return new object[] {"es-419", spanishPattern };
            yield return new object[] {"es-ES", spanishPattern };
            yield return new object[] {"es-MX", spanishPattern };
            yield return new object[] {"et-EE", "dddd, d. MMMM yyyy" };
            yield return new object[] {"fa-IR", "yyyy MMMM d, dddd" };
            yield return new object[] {"fi-FI", "dddd d. MMMM yyyy" };
            yield return new object[] {"fil-PH", "dddd, MMMM d, yyyy" };
            yield return new object[] {"fr-BE", "dddd d MMMM yyyy" };
            yield return new object[] {"fr-CA", "dddd d MMMM yyyy" };
            yield return new object[] {"fr-CH", "dddd, d MMMM yyyy" };
            yield return new object[] {"fr-FR", "dddd d MMMM yyyy" };
            yield return new object[] {"gu-IN", "dddd, d MMMM, yyyy" };
            yield return new object[] {"he-IL", "dddd, d בMMMM yyyy" };
            yield return new object[] {"hi-IN", "dddd, d MMMM yyyy" };
            yield return new object[] {"hr-BA", "dddd, d. MMMM yyyy." };
            yield return new object[] {"hr-HR", "dddd, d. MMMM yyyy." };
            yield return new object[] {"hu-HU", "yyyy. MMMM d., dddd" };
            yield return new object[] {"id-ID", "dddd, d MMMM yyyy" }; // "dddd, dd MMMM yyyy"
            yield return new object[] {"it-CH", "dddd, d MMMM yyyy" };
            yield return new object[] {"it-IT", "dddd d MMMM yyyy" };
            yield return new object[] {"ja-JP", "yyyy年M月d日dddd" };
            yield return new object[] {"kn-IN", "dddd, MMMM d, yyyy" };
            yield return new object[] {"ko-KR", "yyyy년 M월 d일 dddd" };
            yield return new object[] {"lt-LT", "yyyy 'm'. MMMM d 'd'., dddd" };
            yield return new object[] {"lv-LV", "dddd, yyyy. 'gada' d. MMMM" };
            yield return new object[] {"ml-IN", "yyyy, MMMM d, dddd" };
            yield return new object[] {"mr-IN", "dddd, d MMMM, yyyy" };
            yield return new object[] {"ms-BN", "dddd, d MMMM yyyy" }; // "dd MMMM yyyy"
            yield return new object[] {"ms-MY", "dddd, d MMMM yyyy" };
            yield return new object[] {"ms-SG", "dddd, d MMMM yyyy" };
            yield return new object[] {"nb-NO", "dddd d. MMMM yyyy" };
            yield return new object[] {"no-NO", "dddd d. MMMM yyyy" };
            yield return new object[] {"nl-AW", "dddd d MMMM yyyy" };
            yield return new object[] {"nl-BE", "dddd d MMMM yyyy" };
            yield return new object[] {"nl-NL", "dddd d MMMM yyyy" };
            yield return new object[] {"pl-PL", "dddd, d MMMM yyyy" };
            yield return new object[] {"pt-BR", "dddd, d 'de' MMMM 'de' yyyy" };
            yield return new object[] {"pt-PT", "dddd, d 'de' MMMM 'de' yyyy" };
            yield return new object[] {"ro-RO", "dddd, d MMMM yyyy" };
            yield return new object[] {"ru-RU", "dddd, d MMMM yyyy 'г'." };
            yield return new object[] {"sk-SK", "dddd d. MMMM yyyy" };
            yield return new object[] {"sl-SI", "dddd, d. MMMM yyyy" }; // "dddd, dd. MMMM yyyy"
            yield return new object[] {"sr-Cyrl-RS", "dddd, d. MMMM yyyy." }; // "dddd, dd. MMMM yyyy"
            yield return new object[] {"sr-Latn-RS", "dddd, d. MMMM yyyy." }; // "dddd, dd. MMMM yyyy"
            yield return new object[] {"sv-AX", "dddd d MMMM yyyy" };
            yield return new object[] {"sv-SE", "dddd d MMMM yyyy" };
            yield return new object[] {"sw-CD", "dddd, d MMMM yyyy" };
            yield return new object[] {"sw-KE", "dddd, d MMMM yyyy" };
            yield return new object[] {"sw-TZ", "dddd, d MMMM yyyy" };
            yield return new object[] {"sw-UG", "dddd, d MMMM yyyy" };
            yield return new object[] {"ta-IN", "dddd, d MMMM, yyyy" };
            yield return new object[] {"ta-LK", "dddd, d MMMM, yyyy" };
            yield return new object[] {"ta-MY", "dddd, d MMMM, yyyy" };
            yield return new object[] {"ta-SG", "dddd, d MMMM, yyyy" };
            yield return new object[] {"te-IN", "d, MMMM yyyy, dddd" };
            yield return new object[] {"th-TH", "ddddที่ d MMMM g yyyy" };
            yield return new object[] {"tr-CY", "d MMMM yyyy dddd" };
            yield return new object[] {"tr-TR", "d MMMM yyyy dddd" };
            yield return new object[] {"uk-UA", "dddd, d MMMM yyyy 'р'." };
            yield return new object[] {"vi-VN", "dddd, d MMMM, yyyy" };
            yield return new object[] {"zh-CN", "yyyy年M月d日dddd" };
            yield return new object[] {"zh-Hans-HK", "yyyy年M月d日dddd" };
            yield return new object[] {"zh-SG", "yyyy年M月d日dddd" };
            yield return new object[] {"zh-HK", "yyyy年M月d日dddd" };
            yield return new object[] {"zh-TW", "yyyy年M月d日 dddd" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [MemberData(nameof(LongDatePattern_Get_TestData_ICU))]
        public void LongDatePattern_Get_ReturnsExpected_ICU(DateTimeFormatInfo format, string expected, string cultureName)
        {
            var result = format.LongDatePattern;
            Assert.True(expected == result, $"Failed for {cultureName}, Expected: \"{expected}\", Actual: \"{result}\"");
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(LongDatePattern_Get_TestData_HybridGlobalization))]
        public void LongDatePattern_Get_ReturnsExpected_HybridGlobalization(string cultureName, string expected)
        {
            var format = new CultureInfo(cultureName).DateTimeFormat;
            Assert.True(expected == format.LongDatePattern, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {format.LongDatePattern}");
        }

        [Theory]
        [MemberData(nameof(LongDatePattern_Set_TestData))]
        public void LongDatePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.LongDatePattern = value;
            Assert.Equal(value, format.LongDatePattern);
        }

        [Fact]
        public void LongDatePattern_Set_InvalidatesDerivedPattern()
        {
            const string Pattern = "#$";
            var format = new DateTimeFormatInfo();
            var d = DateTimeOffset.Now;
            d.ToString("F", format); // FullDateTimePattern
            format.LongDatePattern = Pattern;
            Assert.Contains(Pattern, d.ToString("F", format));
        }

        [Fact]
        public void LongDatePattern_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.LongDatePattern = null);
        }

        [Fact]
        public void LongDatePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.LongDatePattern = "dddd, dd MMMM yyyy");
        }
    }
}
