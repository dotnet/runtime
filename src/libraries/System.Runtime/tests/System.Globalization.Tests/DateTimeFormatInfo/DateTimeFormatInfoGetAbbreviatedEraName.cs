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
            yield return new object[] { "en-US", 0, DateTimeFormatInfoData.EnUSAbbreviatedEraName() };
            yield return new object[] { "en-US", 1, DateTimeFormatInfoData.EnUSAbbreviatedEraName() };
            yield return new object[] { "invariant", 0, "AD" };
            yield return new object[] { "invariant", 1, "AD" };
            yield return new object[] { "ja-JP", 1, DateTimeFormatInfoData.JaJPAbbreviatedEraName() };

            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                // see the comments on the right to check the non-Hybrid result, if it differs
                yield return new object[] { "ar-SA", 1, "هـ" };
                yield return new object[] { "am-ET", 1, "ዓ/ም" };
                yield return new object[] { "bg-BG", 1, "сл.Хр." };
                yield return new object[] { "bn-BD", 1, "খৃষ্টাব্দ" };
                yield return new object[] { "bn-IN", 1, "খ্রিঃ" }; // "খৃষ্টাব্দ"
                yield return new object[] { "ca-AD", 1, "dC" };
                yield return new object[] { "ca-ES", 1, "dC" };
                yield return new object[] { "cs-CZ", 1, "n.l." };
                yield return new object[] { "da-DK", 1, "eKr" };
                yield return new object[] { "de-AT", 1, "n. Chr." };
                yield return new object[] { "de-BE", 1, "n. Chr." };
                yield return new object[] { "de-CH", 1, "n. Chr." };
                yield return new object[] { "de-DE", 1, "n. Chr." };
                yield return new object[] { "de-IT", 1, "n. Chr." };
                yield return new object[] { "de-LI", 1, "n. Chr." };
                yield return new object[] { "de-LU", 1, "n. Chr." };
                yield return new object[] { "el-CY", 1, "μ.Χ." };
                yield return new object[] { "el-GR", 1, "μ.Χ." };
                yield return new object[] { "en-AE", 1, "A" }; // AD
                yield return new object[] { "en-AG", 1, "A" }; // AD
                yield return new object[] { "en-AI", 1, "A" }; // AD
                yield return new object[] { "en-AS", 1, "A" };
                yield return new object[] { "en-AT", 1, "A" }; // AD
                yield return new object[] { "en-AU", 1, "A" }; // AD
                yield return new object[] { "en-BB", 1, "A" }; // AD
                yield return new object[] { "en-BE", 1, "A" }; // AD
                yield return new object[] { "en-BI", 1, "A" }; // AD
                yield return new object[] { "en-BM", 1, "A" }; // AD
                yield return new object[] { "en-BS", 1, "A" }; // AD
                yield return new object[] { "en-BW", 1, "A" }; // AD
                yield return new object[] { "en-BZ", 1, "A" }; // AD
                yield return new object[] { "en-CA", 1, "A" }; // AD
                yield return new object[] { "en-CC", 1, "A" }; // AD
                yield return new object[] { "en-CH", 1, "A" }; // AD
                yield return new object[] { "en-CK", 1, "A" }; // AD
                yield return new object[] { "en-CM", 1, "A" }; // AD
                yield return new object[] { "en-CX", 1, "A" }; // AD
                yield return new object[] { "en-CY", 1, "A" }; // AD
                yield return new object[] { "en-DE", 1, "A" }; // AD
                yield return new object[] { "en-DK", 1, "A" }; // AD
                yield return new object[] { "en-DM", 1, "A" }; // AD
                yield return new object[] { "en-ER", 1, "A" }; // AD
                yield return new object[] { "en-FI", 1, "A" }; // AD
                yield return new object[] { "en-FJ", 1, "A" }; // AD
                yield return new object[] { "en-FK", 1, "A" }; // AD
                yield return new object[] { "en-FM", 1, "A" }; // AD
                yield return new object[] { "en-GB", 1, "A" }; // AD
                yield return new object[] { "en-GD", 1, "A" }; // AD
                yield return new object[] { "en-GG", 1, "A" }; // AD
                yield return new object[] { "en-GH", 1, "A" }; // AD
                yield return new object[] { "en-GI", 1, "A" }; // AD
                yield return new object[] { "en-GM", 1, "A" }; // AD
                yield return new object[] { "en-GU", 1, "A" };
                yield return new object[] { "en-GY", 1, "A" }; // AD
                yield return new object[] { "en-HK", 1, "A" }; // AD
                yield return new object[] { "en-IE", 1, "A" }; // AD
                yield return new object[] { "en-IL", 1, "A" }; // AD
                yield return new object[] { "en-IM", 1, "A" }; // AD
                yield return new object[] { "en-IN", 1, "A" }; // AD
                yield return new object[] { "en-IO", 1, "A" }; // AD
                yield return new object[] { "en-JE", 1, "A" }; // AD
                yield return new object[] { "en-JM", 1, "A" }; // AD
                yield return new object[] { "en-KE", 1, "A" }; // AD
                yield return new object[] { "en-KI", 1, "A" }; // AD
                yield return new object[] { "en-KN", 1, "A" }; // AD
                yield return new object[] { "en-KY", 1, "A" }; // AD
                yield return new object[] { "en-LC", 1, "A" }; // AD
                yield return new object[] { "en-LR", 1, "A" }; // AD
                yield return new object[] { "en-LS", 1, "A" }; // AD
                yield return new object[] { "en-MG", 1, "A" }; // AD
                yield return new object[] { "en-MH", 1, "A" };
                yield return new object[] { "en-MO", 1, "A" }; // AD
                yield return new object[] { "en-MP", 1, "A" };
                yield return new object[] { "en-MS", 1, "A" }; // AD
                yield return new object[] { "en-MT", 1, "A" }; // AD
                yield return new object[] { "en-MU", 1, "A" }; // AD
                yield return new object[] { "en-MW", 1, "A" }; // AD
                yield return new object[] { "en-MY", 1, "A" }; // AD
                yield return new object[] { "en-NA", 1, "A" }; // AD
                yield return new object[] { "en-NF", 1, "A" }; // AD
                yield return new object[] { "en-NG", 1, "A" }; // AD
                yield return new object[] { "en-NL", 1, "A" }; // AD
                yield return new object[] { "en-NR", 1, "A" }; // AD
                yield return new object[] { "en-NU", 1, "A" }; // AD
                yield return new object[] { "en-NZ", 1, "A" }; // AD
                yield return new object[] { "en-PG", 1, "A" }; // AD
                yield return new object[] { "en-PH", 1, "A" }; // AD
                yield return new object[] { "en-PK", 1, "A" }; // AD
                yield return new object[] { "en-PN", 1, "A" }; // AD
                yield return new object[] { "en-PR", 1, "A" };
                yield return new object[] { "en-PW", 1, "A" }; // AD
                yield return new object[] { "en-RW", 1, "A" }; // AD
                yield return new object[] { "en-SB", 1, "A" }; // AD
                yield return new object[] { "en-SC", 1, "A" }; // AD
                yield return new object[] { "en-SD", 1, "A" }; // AD
                yield return new object[] { "en-SE", 1, "A" }; // AD
                yield return new object[] { "en-SG", 1, "A" }; // AD
                yield return new object[] { "en-SH", 1, "A" }; // AD
                yield return new object[] { "en-SI", 1, "A" }; // AD
                yield return new object[] { "en-SL", 1, "A" }; // AD
                yield return new object[] { "en-SS", 1, "A" }; // AD
                yield return new object[] { "en-SX", 1, "A" }; // AD
                yield return new object[] { "en-SZ", 1, "A" }; // AD
                yield return new object[] { "en-TC", 1, "A" }; // AD
                yield return new object[] { "en-TK", 1, "A" }; // AD
                yield return new object[] { "en-TO", 1, "A" }; // AD
                yield return new object[] { "en-TT", 1, "A" }; // AD
                yield return new object[] { "en-TV", 1, "A" }; // AD
                yield return new object[] { "en-TZ", 1, "A" }; // AD
                yield return new object[] { "en-UG", 1, "A" }; // AD
                yield return new object[] { "en-UM", 1, "A" };
                yield return new object[] { "en-US", 1, "A" };
                yield return new object[] { "en-VC", 1, "A" }; // AD
                yield return new object[] { "en-VG", 1, "A" }; // AD
                yield return new object[] { "en-VI", 1, "A" };
                yield return new object[] { "en-VU", 1, "A" }; // AD
                yield return new object[] { "en-WS", 1, "A" }; // AD
                yield return new object[] { "en-ZA", 1, "A" }; // AD
                yield return new object[] { "en-ZM", 1, "A" }; // AD
                yield return new object[] { "en-ZW", 1, "A" }; // AD
                yield return new object[] { "es-ES", 1, "d. C." };
                yield return new object[] { "es-419", 1, "d.C." }; // "d. C."
                yield return new object[] { "es-MX", 1, "d.C." }; // "d. C."
                yield return new object[] { "et-EE", 1, "pKr" };
                yield return new object[] { "fa-IR", 1, "ه.ش" }; // ه‍.ش.
                yield return new object[] { "fi-FI", 1, "jKr" };
                yield return new object[] { "fil-PH", 1, "AD" };
                yield return new object[] { "fr-BE", 1, "ap. J.-C." };
                yield return new object[] { "fr-CA", 1, "ap. J.-C." };
                yield return new object[] { "fr-CH", 1, "ap. J.-C." };
                yield return new object[] { "fr-FR", 1, "ap. J.-C." };
                yield return new object[] { "gu-IN", 1, "ઇસ" };
                yield return new object[] { "he-IL", 1, "אחריי" }; // לספירה
                yield return new object[] { "hi-IN", 1, "ईस्वी" };
                yield return new object[] { "hr-BA", 1, "AD" }; // po. Kr.
                yield return new object[] { "hr-HR", 1, "AD" };
                yield return new object[] { "hu-HU", 1, "isz." };
                yield return new object[] { "id-ID", 1, "M" };
                yield return new object[] { "it-CH", 1, "dC" }; // d.C.
                yield return new object[] { "it-IT", 1, "dC" };
                yield return new object[] { "ja-JP", 1, "AD" };
                yield return new object[] { "kn-IN", 1, "ಕ್ರಿ.ಶ" };
                yield return new object[] { "ko-KR", 1, "AD" };
                yield return new object[] { "lt-LT", 1, "po Kr." };
                yield return new object[] { "lv-LV", 1, "m.ē." };
                yield return new object[] { "ml-IN", 1, "എഡി" };
                yield return new object[] { "mr-IN", 1, "इ. स." };
                yield return new object[] { "ms-BN", 1, "TM" };
                yield return new object[] { "ms-MY", 1, "TM" };
                yield return new object[] { "ms-SG", 1, "TM" };
                yield return new object[] { "nb-NO", 1, "e.Kr." };
                yield return new object[] { "no", 1, "e.Kr." };
                yield return new object[] { "no-NO", 1, "e.Kr." };
                yield return new object[] { "nl-AW", 1, "n.C." };
                yield return new object[] { "nl-BE", 1, "n.C." }; // n.Chr.
                yield return new object[] { "nl-NL", 1, "n.C." };
                yield return new object[] { "pl-PL", 1, "n.e." };
                yield return new object[] { "pt-BR", 1, "d.C." };
                yield return new object[] { "pt-PT", 1, "d.C." };
                yield return new object[] { "ro-RO", 1, "d.Hr." };
                yield return new object[] { "ru-RU", 1, "н.э." };
                yield return new object[] { "sk-SK", 1, "po Kr." };
                yield return new object[] { "sl-SI", 1, "po Kr." };
                yield return new object[] { "sr-Cyrl-RS", 1, "н.е." };
                yield return new object[] { "sr-Latn-RS", 1, "n.e." };
                yield return new object[] { "sv-AX", 1, "e.Kr." };
                yield return new object[] { "sv-SE", 1, "e.Kr." };
                yield return new object[] { "sw-CD", 1, "BK" };
                yield return new object[] { "sw-KE", 1, "BK" };
                yield return new object[] { "sw-TZ", 1, "BK" };
                yield return new object[] { "sw-UG", 1, "BK" };
                yield return new object[] { "ta-IN", 1, "கி.பி." };
                yield return new object[] { "ta-LK", 1, "கி.பி." };
                yield return new object[] { "ta-MY", 1, "கி.பி." };
                yield return new object[] { "ta-SG", 1, "கி.பி." };
                yield return new object[] { "te-IN", 1, "క్రీశ" };
                yield return new object[] { "th-TH", 1, "พ.ศ." };
                yield return new object[] { "tr-CY", 1, "MS" };
                yield return new object[] { "tr-TR", 1, "MS" };
                yield return new object[] { "uk-UA", 1, "н.е." };
                yield return new object[] { "vi-VN", 1, "sau CN" };
                yield return new object[] { "zh-CN", 1, "公元" };
                yield return new object[] { "zh-Hans-HK", 1, "公元" };
                yield return new object[] { "zh-SG", 1, "公元" };
                yield return new object[] { "zh-HK", 1, "公元" };
                yield return new object[] { "zh-TW", 1, "西元" };
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnApplePlatform))]
        [MemberData(nameof(GetAbbreviatedEraName_TestData))]
        public void GetAbbreviatedEraName_Invoke_ReturnsExpected(string cultureName, int era, string expected)
        {
            var format = cultureName == "invariant" ? new DateTimeFormatInfo() : new CultureInfo(cultureName).DateTimeFormat;
            var eraName = format.GetAbbreviatedEraName(era);
            Assert.True(expected == eraName, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {eraName}");
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
