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
            yield return new object[] { "ar-SA", "ص" };
            yield return new object[] { "am-ET", "ጥዋት" };
            yield return new object[] { "bg-BG", "пр.об." };
            yield return new object[] { "bn-BD", "AM" };
            yield return new object[] { "bn-IN", "AM" };
            yield return new object[] { "ca-AD", "a.\u00A0m." };
            yield return new object[] { "ca-ES", "a.\u00A0m." };
            yield return new object[] { "cs-CZ", "dop." };
            yield return new object[] { "da-DK", "AM" };
            yield return new object[] { "de-AT", "AM" };
            yield return new object[] { "de-BE", "AM" };
            yield return new object[] { "de-CH", "AM" };
            yield return new object[] { "de-DE", "AM" };
            yield return new object[] { "de-IT", "AM" };
            yield return new object[] { "de-LI", "AM" };
            yield return new object[] { "de-LU", "AM" };
            yield return new object[] { "el-CY", "π.μ." };
            yield return new object[] { "el-GR", "π.μ." };
            yield return new object[] { "en-AE", "AM" };
            yield return new object[] { "en-AG", "am" };
            yield return new object[] { "en-AI", "am" };
            yield return new object[] { "en-AS", "AM" };
            yield return new object[] { "en-AT", "am" };
            yield return new object[] { "en-AU", "am" };
            yield return new object[] { "en-BB", "am" };
            yield return new object[] { "en-BE", "am" };
            yield return new object[] { "en-BI", "AM" };
            yield return new object[] { "en-BM", "am" };
            yield return new object[] { "en-BS", "am" };
            yield return new object[] { "en-BW", "am" };
            yield return new object[] { "en-BZ", "am" };
            yield return new object[] { "en-CA", "a.m." };
            yield return new object[] { "en-CC", "am" };
            yield return new object[] { "en-CH", "am" };
            yield return new object[] { "en-CK", "am" };
            yield return new object[] { "en-CM", "am" };
            yield return new object[] { "en-CX", "am" };
            yield return new object[] { "en-CY", "am" };
            yield return new object[] { "en-DE", "am" };
            yield return new object[] { "en-DK", "am" };
            yield return new object[] { "en-DM", "am" };
            yield return new object[] { "en-ER", "am" };
            yield return new object[] { "en-FI", "am" };
            yield return new object[] { "en-FJ", "am" };
            yield return new object[] { "en-FK", "am" };
            yield return new object[] { "en-FM", "am" };
            yield return new object[] { "en-GB", "am" };
            yield return new object[] { "en-GD", "am" };
            yield return new object[] { "en-GG", "am" };
            yield return new object[] { "en-GH", "am" };
            yield return new object[] { "en-GI", "am" };
            yield return new object[] { "en-GM", "am" };
            yield return new object[] { "en-GU", "AM" };
            yield return new object[] { "en-GY", "am" };
            yield return new object[] { "en-HK", "am" };
            yield return new object[] { "en-IE", "a.m." };
            yield return new object[] { "en-IL", "am" };
            yield return new object[] { "en-IM", "am" };
            yield return new object[] { "en-IN", "am" };
            yield return new object[] { "en-IO", "am" };
            yield return new object[] { "en-JE", "am" };
            yield return new object[] { "en-JM", "am" };
            yield return new object[] { "en-KE", "am" };
            yield return new object[] { "en-KI", "am" };
            yield return new object[] { "en-KN", "am" };
            yield return new object[] { "en-KY", "am" };
            yield return new object[] { "en-LC", "am" };
            yield return new object[] { "en-LR", "am" };
            yield return new object[] { "en-LS", "am" };
            yield return new object[] { "en-MG", "am" };
            yield return new object[] { "en-MH", "AM" };
            yield return new object[] { "en-MO", "am" };
            yield return new object[] { "en-MP", "AM" };
            yield return new object[] { "en-MS", "am" };
            yield return new object[] { "en-MT", "am" };
            yield return new object[] { "en-MU", "am" };
            yield return new object[] { "en-MW", "am" };
            yield return new object[] { "en-MY", "am" };
            yield return new object[] { "en-NA", "am" };
            yield return new object[] { "en-NF", "am" };
            yield return new object[] { "en-NG", "am" };
            yield return new object[] { "en-NL", "am" };
            yield return new object[] { "en-NR", "am" };
            yield return new object[] { "en-NU", "am" };
            yield return new object[] { "en-NZ", "am" };
            yield return new object[] { "en-PG", "am" };
            yield return new object[] { "en-PH", "AM" }; // am
            yield return new object[] { "en-PK", "am" };
            yield return new object[] { "en-PN", "am" };
            yield return new object[] { "en-PR", "AM" };
            yield return new object[] { "en-PW", "am" };
            yield return new object[] { "en-RW", "am" };
            yield return new object[] { "en-SB", "am" };
            yield return new object[] { "en-SC", "am" };
            yield return new object[] { "en-SD", "am" };
            yield return new object[] { "en-SE", "am" };
            yield return new object[] { "en-SG", "am" };
            yield return new object[] { "en-SH", "am" };
            yield return new object[] { "en-SI", "am" };
            yield return new object[] { "en-SL", "am" };
            yield return new object[] { "en-SS", "am" };
            yield return new object[] { "en-SX", "am" };
            yield return new object[] { "en-SZ", "am" };
            yield return new object[] { "en-TC", "am" };
            yield return new object[] { "en-TK", "am" };
            yield return new object[] { "en-TO", "am" };
            yield return new object[] { "en-TT", "am" };
            yield return new object[] { "en-TV", "am" };
            yield return new object[] { "en-TZ", "am" };
            yield return new object[] { "en-UG", "am" };
            yield return new object[] { "en-UM", "AM" };
            yield return new object[] { "en-US", "AM" };
            yield return new object[] { "en-VC", "am" };
            yield return new object[] { "en-VG", "am" };
            yield return new object[] { "en-VI", "AM" };
            yield return new object[] { "en-VU", "am" };
            yield return new object[] { "en-WS", "am" };
            yield return new object[] { "en-ZA", "am" };
            yield return new object[] { "en-ZM", "am" };
            yield return new object[] { "en-ZW", "am" };
            string latinAmericaSpanishAMDesignator = PlatformDetection.IsFirefox || PlatformDetection.IsNodeJS ? "a.\u00A0m." : "a.m.";
            yield return new object[] { "es-419", latinAmericaSpanishAMDesignator };
            yield return new object[] { "es-ES", "a.\u00A0m." };
            yield return new object[] { "es-MX", latinAmericaSpanishAMDesignator };
            yield return new object[] { "et-EE", "AM" };
            yield return new object[] { "fa-IR", "قبل‌ازظهر" };
            yield return new object[] { "fi-FI", "ap." };
            yield return new object[] { "fil-PH", "AM" };
            yield return new object[] { "fr-BE", "AM" };
            yield return new object[] { "fr-CA", "a.m." };
            yield return new object[] { "fr-CH", "AM" };
            yield return new object[] { "fr-FR", "AM" };
            yield return new object[] { "gu-IN", "AM" };
            yield return new object[] { "he-IL", "לפנה״צ" };
            yield return new object[] { "hi-IN", "am" };
            yield return new object[] { "hr-BA", "AM" };
            yield return new object[] { "hr-HR", "AM" };
            yield return new object[] { "hu-HU", "de." };
            yield return new object[] { "id-ID", "AM" };
            yield return new object[] { "it-CH", "AM" };
            yield return new object[] { "it-IT", "AM" };
            yield return new object[] { "ja-JP", "午前" };
            yield return new object[] { "kn-IN", "ಪೂರ್ವಾಹ್ನ" };
            yield return new object[] { "ko-KR", "오전" };
            yield return new object[] { "lt-LT", "priešpiet" };
            yield return new object[] { "lv-LV", "priekšpusdienā" };
            yield return new object[] { "ml-IN", "AM" };
            yield return new object[] { "mr-IN", "AM" }; // म.पू.
            yield return new object[] { "ms-BN", "PG" };
            yield return new object[] { "ms-MY", "PG" };
            yield return new object[] { "ms-SG", "PG" };
            yield return new object[] { "nb-NO", "a.m." };
            yield return new object[] { "no", "a.m." };
            yield return new object[] { "no-NO", "a.m." };
            yield return new object[] { "nl-AW", "a.m." };
            yield return new object[] { "nl-BE", "a.m." };
            yield return new object[] { "nl-NL", "a.m." };
            yield return new object[] { "pl-PL", "AM" };
            yield return new object[] { "pt-BR", "AM" };
            yield return new object[] { "pt-PT", "da manhã" };
            yield return new object[] { "ro-RO", "a.m." };
            yield return new object[] { "ru-RU", "AM" };
            yield return new object[] { "sk-SK", "AM" };
            yield return new object[] { "sl-SI", "dop." };
            yield return new object[] { "sr-Cyrl-RS", "AM" }; // пре подне
            yield return new object[] { "sr-Latn-RS", "AM" }; // pre podne
            yield return new object[] { "sv-AX", "fm" };
            yield return new object[] { "sv-SE", "fm" };
            yield return new object[] { "sw-CD", "AM" };
            yield return new object[] { "sw-KE", "AM" };
            yield return new object[] { "sw-TZ", "AM" };
            yield return new object[] { "sw-UG", "AM" };
            string tamilAMDesignator = PlatformDetection.IsFirefox || PlatformDetection.IsNodeJS ? "முற்பகல்" : "AM"; // முற்பகல்
            yield return new object[] { "ta-IN", tamilAMDesignator };
            yield return new object[] { "ta-LK", tamilAMDesignator };
            yield return new object[] { "ta-MY", tamilAMDesignator };
            yield return new object[] { "ta-SG", tamilAMDesignator };
            yield return new object[] { "te-IN", "AM" };
            yield return new object[] { "th-TH", "ก่อนเที่ยง" };
            yield return new object[] { "tr-CY", "ÖÖ" };
            yield return new object[] { "tr-TR", "ÖÖ" };
            yield return new object[] { "uk-UA", "дп" };
            yield return new object[] { "vi-VN", "SA" };
            yield return new object[] { "zh-CN", "上午" };
            yield return new object[] { "zh-Hans-HK", "上午" };
            yield return new object[] { "zh-SG", "上午" };
            yield return new object[] { "zh-HK", "上午" };
            yield return new object[] { "zh-TW", "上午" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(AMDesignator_Get_TestData_HybridGlobalization))]
        public void AMDesignator_Get_ReturnsExpected_HybridGlobalization(string cultureName, string expected)
        {
            var format = new CultureInfo(cultureName).DateTimeFormat;
            Assert.True(expected == format.AMDesignator, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {format.AMDesignator}");
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
