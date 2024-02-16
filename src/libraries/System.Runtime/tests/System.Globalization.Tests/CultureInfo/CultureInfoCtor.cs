// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoConstructor
    {
        public static IEnumerable<object[]> Ctor_String_TestData()
        {
            yield return new object[] { "", new [] { "" } };
            yield return new object[] { "af", new [] { "af" }};
            yield return new object[] { "af-ZA", new [] { "af-ZA" }};
            yield return new object[] { "am", new [] { "am" } };
            yield return new object[] { "am-ET", new [] { "am-ET" } };
            yield return new object[] { "ar", new [] { "ar" } };
            yield return new object[] { "ar-AE", new [] { "ar-AE" }};
            yield return new object[] { "ar-BH", new [] { "ar-BH" }};
            yield return new object[] { "ar-DZ", new [] { "ar-DZ" }};
            yield return new object[] { "ar-EG", new [] { "ar-EG" }};
            yield return new object[] { "ar-IQ", new [] { "ar-IQ" }};
            yield return new object[] { "ar-JO", new [] { "ar-JO" }};
            yield return new object[] { "ar-KW", new [] { "ar-KW" }};
            yield return new object[] { "ar-LB", new [] { "ar-LB" }};
            yield return new object[] { "ar-LY", new [] { "ar-LY" }};
            yield return new object[] { "ar-MA", new [] { "ar-MA" }};
            yield return new object[] { "ar-OM", new [] { "ar-OM" }};
            yield return new object[] { "ar-QA", new [] { "ar-QA" }};
            yield return new object[] { "ar-SA", new [] { "ar-SA" } };
            yield return new object[] { "ar-SY", new [] { "ar-SY" }};
            yield return new object[] { "ar-TN", new [] { "ar-TN" }};
            yield return new object[] { "ar-YE", new [] { "ar-YE" }};
            yield return new object[] { "arn", new [] { "arn" }};
            yield return new object[] { "arn-CL", new [] { "arn-CL" }};
            yield return new object[] { "as", new [] { "as" }};
            yield return new object[] { "as-IN", new [] { "as-IN" }};
            yield return new object[] { "az", new [] { "az" }};
            yield return new object[] { "az-Cyrl", new [] { "az-Cyrl" }};
            yield return new object[] { "az-Cyrl-AZ", new [] { "az-Cyrl-AZ" }};
            yield return new object[] { "ba", new [] { "ba" }};
            yield return new object[] { "ba-RU", new [] { "ba-RU" }};
            yield return new object[] { "be", new [] { "be" }};
            yield return new object[] { "be-BY", new [] { "be-BY" }};
            yield return new object[] { "bg", new [] { "bg" } };
            yield return new object[] { "bg-BG", new [] { "bg-BG" } };
            yield return new object[] { "bn", new [] { "bn" } };
            yield return new object[] { "bn-BD", new [] { "bn-BD" } };
            yield return new object[] { "bn-IN", new [] { "bn-IN" } };
            yield return new object[] { "bo", new [] { "bo" }};
            yield return new object[] { "bo-CN", new [] { "bo-CN" }};
            yield return new object[] { "br", new [] { "br" }};
            yield return new object[] { "br-FR", new [] { "br-FR" }};
            yield return new object[] { "bs", new [] { "bs" }};
            yield return new object[] { "bs-Cyrl", new [] { "bs-Cyrl" }};
            yield return new object[] { "bs-Cyrl-BA", new [] { "bs-Cyrl-BA" }};
            yield return new object[] { "ca", new [] { "ca" } };
            yield return new object[] { "ca-ES", new [] { "ca-ES" } };
            yield return new object[] { "co", new [] { "co" }};
            yield return new object[] { "co-FR", new [] { "co-FR" }};
            yield return new object[] { "cs", new [] { "cs" } };
            yield return new object[] { "cs-CZ", new [] { "cs-CZ" } };
            yield return new object[] { "cy", new [] { "cy" }};
            yield return new object[] { "cy-GB", new [] { "cy-GB" }};
            yield return new object[] { "da", new [] { "da" } };
            yield return new object[] { "da-DK", new [] { "da-DK" } };
            yield return new object[] { "de", new [] { "de" } };
            yield return new object[] { "de-AT", new [] { "de-AT" } };
            yield return new object[] { "de-CH", new [] { "de-CH" } };
            yield return new object[] { "de-DE", new [] { "de-DE" } };
            yield return new object[] { "de-DE_phoneb", new [] { "de-DE", "de-DE_phoneb" }};
            yield return new object[] { "de-LI", new [] { "de-LI" } };
            yield return new object[] { "de-LU", new [] { "de-LU" } };
            yield return new object[] { "dsb", new [] { "dsb" }};
            yield return new object[] { "dsb-DE", new [] { "dsb-DE" }};
            yield return new object[] { "dv", new [] { "dv" }};
            yield return new object[] { "dv-MV", new [] { "dv-MV" }};
            yield return new object[] { "el", new [] { "el" } };
            yield return new object[] { "el-GR", new [] { "el-GR" } };
            yield return new object[] { "en", new [] { "en" } };
            yield return new object[] { "en-029", new [] { "en-029" }};
            yield return new object[] { "en-AU", new [] { "en-AU" } };
            yield return new object[] { "en-BZ", new [] { "en-BZ" } };
            yield return new object[] { "en-CA", new [] { "en-CA" } };
            yield return new object[] { "en-GB", new [] { "en-GB" } };
            yield return new object[] { "en-IE", new [] { "en-IE" } };
            yield return new object[] { "en-IN", new [] { "en-IN" } };
            yield return new object[] { "en-JM", new [] { "en-JM" } };
            yield return new object[] { "en-MY", new [] { "en-MY" } };
            yield return new object[] { "en-NZ", new [] { "en-NZ" } };
            yield return new object[] { "en-PH", new [] { "en-PH" } };
            yield return new object[] { "en-SG", new [] { "en-SG" } };
            yield return new object[] { "en-TT", new [] { "en-TT" } };
            yield return new object[] { "en-US", new [] { "en-US" } };
            yield return new object[] { "en-ZA", new [] { "en-ZA" } };
            yield return new object[] { "en-ZW", new [] { "en-ZW" } };
            yield return new object[] { "es", new [] { "es" } };
            yield return new object[] { "es-AR", new [] { "es-AR" }};
            yield return new object[] { "es-BO", new [] { "es-BO" }};
            yield return new object[] { "es-CL", new [] { "es-CL" }};
            yield return new object[] { "es-CO", new [] { "es-CO" }};
            yield return new object[] { "es-CR", new [] { "es-CR" }};
            yield return new object[] { "es-DO", new [] { "es-DO" }};
            yield return new object[] { "es-EC", new [] { "es-EC" }};
            yield return new object[] { "es-ES", new [] { "es-ES" } };
            yield return new object[] { "es-ES_tradnl", new [] { "es-ES", "es-ES_tradnl" }};
            yield return new object[] { "es-GT", new [] { "es-GT" }};
            yield return new object[] { "es-HN", new [] { "es-HN" }};
            yield return new object[] { "es-MX", new [] { "es-MX" } };
            yield return new object[] { "es-NI", new [] { "es-NI" }};
            yield return new object[] { "es-PA", new [] { "es-PA" }};
            yield return new object[] { "es-PE", new [] { "es-PE" }};
            yield return new object[] { "es-PR", new [] { "es-PR" }};
            yield return new object[] { "es-PY", new [] { "es-PY" }};
            yield return new object[] { "es-SV", new [] { "es-SV" }};
            yield return new object[] { "es-US", new [] { "es-US" }};
            yield return new object[] { "es-UY", new [] { "es-UY" }};
            yield return new object[] { "es-VE", new [] { "es-VE" }};
            yield return new object[] { "et", new [] { "et" } };
            yield return new object[] { "et-EE", new [] { "et-EE" } };
            yield return new object[] { "eu", new [] { "eu" }};
            yield return new object[] { "eu-ES", new [] { "eu-ES" }};
            yield return new object[] { "fa", new [] { "fa" } };
            yield return new object[] { "fa-IR", new [] { "fa-IR" } };
            yield return new object[] { "fi", new [] { "fi" } };
            yield return new object[] { "fi-FI", new [] { "fi-FI" } };
            yield return new object[] { "fil", new [] { "fil" } };
            yield return new object[] { "fil-PH", new [] { "fil-PH" } };
            yield return new object[] { "fo", new [] { "fo" }};
            yield return new object[] { "fo-FO", new [] { "fo-FO" }};
            yield return new object[] { "fr", new [] { "fr" } };
            yield return new object[] { "fr-BE", new [] { "fr-BE" } };
            yield return new object[] { "fr-CA", new [] { "fr-CA" } };
            yield return new object[] { "fr-CH", new [] { "fr-CH" } };
            yield return new object[] { "fr-FR", new [] { "fr-FR" } };
            yield return new object[] { "fr-LU", new [] { "fr-LU" }};
            yield return new object[] { "fr-MC", new [] { "fr-MC" }};
            yield return new object[] { "fy", new [] { "fy" }};
            yield return new object[] { "fy-NL", new [] { "fy-NL" }};
            yield return new object[] { "ga", new [] { "ga" }};
            yield return new object[] { "ga-IE", new [] { "ga-IE" }};
            yield return new object[] { "gd", new [] { "gd" }};
            yield return new object[] { "gd-GB", new [] { "gd-GB" }};
            yield return new object[] { "gl", new [] { "gl" }};
            yield return new object[] { "gl-ES", new [] { "gl-ES" }};
            yield return new object[] { "gsw", new [] { "gsw" }};
            yield return new object[] { "gsw-FR", new [] { "gsw-FR" }};
            yield return new object[] { "gu", new [] { "gu" } };
            yield return new object[] { "gu-IN", new [] { "gu-IN" } };
            yield return new object[] { "ha", new [] { "ha" }};
            yield return new object[] { "he", new [] { "he" } };
            yield return new object[] { "he-IL", new [] { "he-IL" } };
            yield return new object[] { "hi", new [] { "hi" } };
            yield return new object[] { "hi-IN", new [] { "hi-IN" } };
            yield return new object[] { "hr", new [] { "hr" } };
            yield return new object[] { "hr-BA", new [] { "hr-BA" } };
            yield return new object[] { "hr-HR", new [] { "hr-HR" } };
            yield return new object[] { "hsb", new [] { "hsb" }};
            yield return new object[] { "hsb-DE", new [] { "hsb-DE" }};
            yield return new object[] { "hu", new [] { "hu" } };
            yield return new object[] { "hu-HU", new [] { "hu-HU" } };
            yield return new object[] { "hu-HU_technl", new [] { "hu-HU", "hu-HU_technl" }};
            yield return new object[] { "hy", new [] { "hy" }};
            yield return new object[] { "hy-AM", new [] { "hy-AM" }};
            yield return new object[] { "id", new [] { "id" } };
            yield return new object[] { "id-ID", new [] { "id-ID" } };
            yield return new object[] { "ig", new [] { "ig" }};
            yield return new object[] { "ig-NG", new [] { "ig-NG" }};
            yield return new object[] { "ii", new [] { "ii" }};
            yield return new object[] { "ii-CN", new [] { "ii-CN" }};
            yield return new object[] { "is", new [] { "is" }};
            yield return new object[] { "is-IS", new [] { "is-IS" }};
            yield return new object[] { "it", new [] { "it" } };
            yield return new object[] { "it-CH", new [] { "it-CH" } };
            yield return new object[] { "it-IT", new [] { "it-IT" } };
            yield return new object[] { "iu", new [] { "iu" }};
            yield return new object[] { "iu-Cans", new [] { "iu-Cans" }};
            yield return new object[] { "iu-Cans-CA", new [] { "iu-Cans-CA" }};
            yield return new object[] { "iu-Latn", new [] { "iu-Latn" }};
            yield return new object[] { "iu-Latn-CA", new [] { "iu-Latn-CA" }};
            yield return new object[] { "ja", new [] { "ja" } };
            yield return new object[] { "ja-JP", new [] { "ja-JP" } };
            yield return new object[] { "ja-JP_radstr", new [] { "ja-JP", "ja-JP_radstr" }};
            yield return new object[] { "ka", new [] { "ka" }};
            yield return new object[] { "ka-GE", new [] { "ka-GE" }};
            yield return new object[] { "ka-GE_modern", new [] { "ka-GE", "ka-GE_modern" }};
            yield return new object[] { "kk", new [] { "kk" }};
            yield return new object[] { "kk-KZ", new [] { "kk-KZ" }};
            yield return new object[] { "kl", new [] { "kl" }};
            yield return new object[] { "kl-GL", new [] { "kl-GL" }};
            yield return new object[] { "km", new [] { "km" }};
            yield return new object[] { "km-KH", new [] { "km-KH" }};
            yield return new object[] { "kn", new [] { "kn" } };
            yield return new object[] { "kn-IN", new [] { "kn-IN" } };
            yield return new object[] { "ko", new [] { "ko" } };
            yield return new object[] { "ko-KR", new [] { "ko-KR" } };
            yield return new object[] { "kok", new [] { "kok" }};
            yield return new object[] { "kok-IN", new [] { "kok-IN" }};
            yield return new object[] { "ky", new [] { "ky" }};
            yield return new object[] { "ky-KG", new [] { "ky-KG" }};
            yield return new object[] { "lb", new [] { "lb" }};
            yield return new object[] { "lb-LU", new [] { "lb-LU" }};
            yield return new object[] { "lo", new [] { "lo" }};
            yield return new object[] { "lo-LA", new [] { "lo-LA" }};
            yield return new object[] { "lt", new [] { "lt" } };
            yield return new object[] { "lt-LT", new [] { "lt-LT" } };
            yield return new object[] { "lv", new [] { "lv" } };
            yield return new object[] { "lv-LV", new [] { "lv-LV" } };
            yield return new object[] { "mi", new [] { "mi" }};
            yield return new object[] { "mi-NZ", new [] { "mi-NZ" }};
            yield return new object[] { "mk", new [] { "mk" }};
            yield return new object[] { "mk-MK", new [] { "mk-MK" }};
            yield return new object[] { "ml", new [] { "ml" } };
            yield return new object[] { "ml-IN", new [] { "ml-IN" } };
            yield return new object[] { "mn", new [] { "mn" }};
            yield return new object[] { "mn-MN", new [] { "mn-MN" }};
            yield return new object[] { "mn-Mong", new [] { "mn-Mong" }};
            yield return new object[] { "mn-Mong-CN", new [] { "mn-Mong-CN" }};
            yield return new object[] { "moh", new [] { "moh" }};
            yield return new object[] { "moh-CA", new [] { "moh-CA" }};
            yield return new object[] { "mr", new [] { "mr" } };
            yield return new object[] { "mr-IN", new [] { "mr-IN" } };
            yield return new object[] { "ms", new [] { "ms" } };
            yield return new object[] { "ms-BN", new [] { "ms-BN" } };
            yield return new object[] { "ms-MY", new [] { "ms-MY" } };
            yield return new object[] { "mt", new [] { "mt" }};
            yield return new object[] { "mt-MT", new [] { "mt-MT" }};
            yield return new object[] { "nb", new [] { "nb" } };
            yield return new object[] { "nb-NO", new [] { "nb-NO" } };
            yield return new object[] { "ne", new [] { "ne" }};
            yield return new object[] { "ne-NP", new [] { "ne-NP" }};
            yield return new object[] { "nl", new [] { "nl" } };
            yield return new object[] { "nl-BE", new [] { "nl-BE" } };
            yield return new object[] { "nl-NL", new [] { "nl-NL" } };
            yield return new object[] { "nn", new [] { "nn" }};
            yield return new object[] { "nn-NO", new [] { "nn-NO" }};
            yield return new object[] { "nso", new [] { "nso" }};
            yield return new object[] { "nso-ZA", new [] { "nso-ZA" }};
            yield return new object[] { "oc", new [] { "oc" }};
            yield return new object[] { "oc-FR", new [] { "oc-FR" }};
            yield return new object[] { "or", new [] { "or" }};
            yield return new object[] { "or-IN", new [] { "or-IN" }};
            yield return new object[] { "pa", new [] { "pa" }};
            yield return new object[] { "pa-IN", new [] { "pa-IN" }};
            yield return new object[] { "pl", new [] { "pl" } };
            yield return new object[] { "pl-PL", new [] { "pl-PL" } };
            yield return new object[] { "prs", new [] { "prs", "fa" } };
            yield return new object[] { "prs-AF", new [] { "prs-AF", "fa-AF" }};
            yield return new object[] { "ps", new [] { "ps" }};
            yield return new object[] { "ps-AF", new [] { "ps-AF" }};
            yield return new object[] { "pt", new [] { "pt" } };
            yield return new object[] { "pt-BR", new [] { "pt-BR" } };
            yield return new object[] { "pt-PT", new [] { "pt-PT" } };
            yield return new object[] { "quz", new [] { "quz" }};
            yield return new object[] { "quz-BO", new [] { "quz-BO" }};
            yield return new object[] { "quz-EC", new [] { "quz-EC" }};
            yield return new object[] { "quz-PE", new [] { "quz-PE" }};
            yield return new object[] { "rm", new [] { "rm" }};
            yield return new object[] { "rm-CH", new [] { "rm-CH" }};
            yield return new object[] { "ro", new [] { "ro" } };
            yield return new object[] { "ro-RO", new [] { "ro-RO" } };
            yield return new object[] { "ru", new [] { "ru" } };
            yield return new object[] { "ru-RU", new [] { "ru-RU" } };
            yield return new object[] { "rw", new [] { "rw" }};
            yield return new object[] { "rw-RW", new [] { "rw-RW" }};
            yield return new object[] { "sa", new [] { "sa" }};
            yield return new object[] { "sa-IN", new [] { "sa-IN" }};
            yield return new object[] { "sah", new [] { "sah" }};
            yield return new object[] { "sah-RU", new [] { "sah-RU" }};
            yield return new object[] { "se", new [] { "se" }};
            yield return new object[] { "se-FI", new [] { "se-FI" }};
            yield return new object[] { "se-NO", new [] { "se-NO" }};
            yield return new object[] { "se-SE", new [] { "se-SE" }};
            yield return new object[] { "si", new [] { "si" }};
            yield return new object[] { "si-LK", new [] { "si-LK" }};
            yield return new object[] { "sk", new [] { "sk" } };
            yield return new object[] { "sk-SK", new [] { "sk-SK" } };
            yield return new object[] { "sl", new [] { "sl" } };
            yield return new object[] { "sl-SI", new [] { "sl-SI" } };
            yield return new object[] { "sma", new [] { "sma" }};
            yield return new object[] { "sma-NO", new [] { "sma-NO" }};
            yield return new object[] { "sma-SE", new [] { "sma-SE" }};
            yield return new object[] { "smj", new [] { "smj" }};
            yield return new object[] { "smj-NO", new [] { "smj-NO" }};
            yield return new object[] { "smj-SE", new [] { "smj-SE" }};
            yield return new object[] { "smn", new [] { "smn" }};
            yield return new object[] { "smn-FI", new [] { "smn-FI" }};
            yield return new object[] { "sms", new [] { "sms" }};
            yield return new object[] { "sms-FI", new [] { "sms-FI" }};
            yield return new object[] { "sq", new [] { "sq" }};
            yield return new object[] { "sq-AL", new [] { "sq-AL" }};
            yield return new object[] { "sr", new [] { "sr" } };
            yield return new object[] { "sr-Latn", new [] { "sr-Latn" } };
            yield return new object[] { "sr-Latn-BA", new [] { "sr-Latn-BA" }};
            yield return new object[] { "sr-Latn-ME", new [] { "sr-Latn-ME" }};
            yield return new object[] { "sr-Latn-RS", new [] { "sr-Latn-RS" } };
            yield return new object[] { "sv", new [] { "sv" } };
            yield return new object[] { "sv-FI", new [] { "sv-FI" }};
            yield return new object[] { "sv-SE", new [] { "sv-SE" } };
            yield return new object[] { "sw", new [] { "sw" } };
            yield return new object[] { "sw-KE", new [] { "sw-KE" } };
            yield return new object[] { "syr", new [] { "syr" }};
            yield return new object[] { "syr-SY", new [] { "syr-SY" }};
            yield return new object[] { "ta", new [] { "ta" } };
            yield return new object[] { "ta-IN", new [] { "ta-IN" } };
            yield return new object[] { "te", new [] { "te" } };
            yield return new object[] { "te-IN", new [] { "te-IN" } };
            yield return new object[] { "tg", new [] { "tg" }};
            yield return new object[] { "th", new [] { "th" } };
            yield return new object[] { "th-TH", new [] { "th-TH" } };
            yield return new object[] { "tk", new [] { "tk" }};
            yield return new object[] { "tk-TM", new [] { "tk-TM" }};
            yield return new object[] { "tn", new [] { "tn" }};
            yield return new object[] { "tn-ZA", new [] { "tn-ZA" }};
            yield return new object[] { "tr", new [] { "tr" } };
            yield return new object[] { "tr-TR", new [] { "tr-TR" } };
            yield return new object[] { "tt", new [] { "tt" }};
            yield return new object[] { "tt-RU", new [] { "tt-RU" }};
            yield return new object[] { "tzm", new [] { "tzm" }};
            yield return new object[] { "ug", new [] { "ug" }};
            yield return new object[] { "ug-CN", new [] { "ug-CN" }};
            yield return new object[] { "uk", new [] { "uk" } };
            yield return new object[] { "uk-UA", new [] { "uk-UA" } };
            yield return new object[] { "ur", new [] { "ur" }};
            yield return new object[] { "ur-PK", new [] { "ur-PK" }};
            yield return new object[] { "uz", new [] { "uz" }};
            yield return new object[] { "uz-Cyrl", new [] { "uz-Cyrl" }};
            yield return new object[] { "uz-Cyrl-UZ", new [] { "uz-Cyrl-UZ" }};
            yield return new object[] { "vi", new [] { "vi" } };
            yield return new object[] { "vi-VN", new [] { "vi-VN" } };
            yield return new object[] { "wo", new [] { "wo" }};
            yield return new object[] { "wo-SN", new [] { "wo-SN" }};
            yield return new object[] { "xh", new [] { "xh" }};
            yield return new object[] { "xh-ZA", new [] { "xh-ZA" }};
            yield return new object[] { "yo", new [] { "yo" }};
            yield return new object[] { "yo-NG", new [] { "yo-NG" }};
            yield return new object[] { "zh", new [] { "zh" } };
            yield return new object[] { "zh-CHS", new [] { "zh-CHS", "zh-Hans" }};
            yield return new object[] { "zh-CHT", new [] { "zh-CHT", "zh-Hant" }};
            yield return new object[] { "zh-CN", new [] { "zh-CN" } };
            yield return new object[] { "zh-CN_stroke", new [] { "zh-CN", "zh-CN_stroke" }};
            yield return new object[] { "zh-Hans", new [] { "zh-Hans" } };
            yield return new object[] { "zh-Hant", new [] { "zh-Hant" } };
            yield return new object[] { "zh-HK", new [] { "zh-HK" } };
            yield return new object[] { "zh-HK_radstr", new [] { "zh-HK", "zh-HK_radstr" }};
            yield return new object[] { "zh-MO", new [] { "zh-MO" }};
            yield return new object[] { "zh-MO_radstr", new [] { "zh-MO", "zh-MO_radstr" }};
            yield return new object[] { "zh-MO_stroke", new [] { "zh-MO", "zh-MO_stroke" }};
            yield return new object[] { "zh-SG", new [] { "zh-SG" } };
            yield return new object[] { "zh-SG_stroke", new [] { "zh-SG", "zh-SG_stroke" }};
            yield return new object[] { "zh-TW", new [] { "zh-TW" } };
            yield return new object[] { "zh-TW_pronun", new [] { "zh-TW", "zh-TW_pronun" }};
            yield return new object[] { "zh-TW_radstr", new [] { "zh-TW", "zh-TW_radstr" }};
            yield return new object[] { "zu", new [] { "zu" }};
            yield return new object[] { "zu-ZA", new [] { "zu-ZA" }};
            yield return new object[] { CultureInfo.CurrentCulture.Name, new [] { CultureInfo.CurrentCulture.Name } };

            if (PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
            {
                yield return new object[] { "az-Latn", new [] { "az-Latn" }};
                yield return new object[] { "az-Latn-AZ", new [] { "az-Latn-AZ" }};
                yield return new object[] { "bs-Latn", new [] { "bs-Latn" }};
                yield return new object[] { "bs-Latn-BA", new [] { "bs-Latn-BA" }};
                yield return new object[] { "ha-Latn", new [] { "ha-Latn" }};
                yield return new object[] { "ha-Latn-NG", new [] { "ha-Latn-NG" }};
                yield return new object[] { "mn-Cyrl", new [] { "mn-Cyrl" }};
                yield return new object[] { "no", new [] { "no" } };
                yield return new object[] { "sr-Cyrl", new [] { "sr-Cyrl" } };
                yield return new object[] { "sr-Cyrl-BA", new [] { "sr-Cyrl-BA" }};
                yield return new object[] { "sr-Cyrl-CS", new [] { "sr-Cyrl-CS" }};
                yield return new object[] { "sr-Cyrl-ME", new [] { "sr-Cyrl-ME" }};
                yield return new object[] { "sr-Cyrl-RS", new [] { "sr-Cyrl-RS" } };
                yield return new object[] { "sr-Latn-CS", new [] { "sr-Latn-CS" }};
                yield return new object[] { "tg-Cyrl", new [] { "tg-Cyrl" }};
                yield return new object[] { "tg-Cyrl-TJ", new [] { "tg-Cyrl-TJ" }};
                yield return new object[] { "tzm-Latn", new [] { "tzm-Latn" }};
                yield return new object[] { "tzm-Latn-DZ", new [] { "tzm-Latn-DZ" }};
                yield return new object[] { "uz-Latn", new [] { "uz-Latn" }};
                yield return new object[] { "uz-Latn-UZ", new [] { "uz-Latn-UZ" }};
            }

            if ((!PlatformDetection.IsWindows || PlatformDetection.WindowsVersion >= 10) && (PlatformDetection.IsNotBrowser))
            {
                yield return new object[] { "en-US-CUSTOM", new [] { "en-US-CUSTOM", "en-US-custom" } };
                yield return new object[] { "xx-XX", new [] { "xx-XX" } };
            }
        }

        [Theory]
        [MemberData(nameof(Ctor_String_TestData))]
        public void Ctor_String(string name, string[] expectedNames)
        {
            CultureInfo culture = new CultureInfo(name);
            string cultureName = culture.Name;
            Assert.Contains(cultureName, expectedNames, StringComparer.OrdinalIgnoreCase);

            culture = new CultureInfo(cultureName);
            Assert.Equal(cultureName, culture.ToString(), ignoreCase: true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnApplePlatform))]
        public void Ctor_String_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => new CultureInfo(null)); // Name is null
            Assert.Throws<CultureNotFoundException>(() => new CultureInfo("en-US@x=1")); // Name doesn't support ICU keywords
            Assert.Throws<CultureNotFoundException>(() => new CultureInfo("NotAValidCulture")); // Name is invalid

            if (PlatformDetection.IsWindows && PlatformDetection.WindowsVersion < 10)
            {
                Assert.Throws<CultureNotFoundException>(() => new CultureInfo("no-such-culture"));
                Assert.Throws<CultureNotFoundException>(() => new CultureInfo("en-US-CUSTOM"));
                Assert.Throws<CultureNotFoundException>(() => new CultureInfo("xx-XX"));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version1903OrGreater))]
        [InlineData(0x2000)]
        [InlineData(0x2400)]
        [InlineData(0x2800)]
        [InlineData(0x2C00)]
        [InlineData(0x3000)]
        [InlineData(0x3400)]
        [InlineData(0x3800)]
        [InlineData(0x3C00)]
        [InlineData(0x4000)]
        [InlineData(0x4400)]
        [InlineData(0x4800)]
        [InlineData(0x4C00)]
        public void TestCreationWithTemporaryLCID(int lcid)
        {
            // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/926e694f-1797-4418-a922-343d1c5e91a6
            // If a temporary LCID is assigned it will be dynamically assigned at runtime to be
            // 0x2000, 0x2400, 0x2800, 0x2C00, 0x3000, 0x3400, 0x3800, 0x3C00, 0x4000, 0x4400, 0x4800, or 0x4C00,
            // for the valid language-script-region tags.

            Assert.NotEqual(lcid, new CultureInfo(lcid).LCID);
        }

        [InlineData("zh-TW-u-co-zhuyin", "zh-TW", "zh-TW_zhuyin")]
        [InlineData("de-DE-u-co-phonebk", "de-DE", "de-DE_phoneboo")]
        [InlineData("de-DE-u-co-phonebk-u-xx", "de-DE-u-xx", "de-DE-u-xx_phoneboo")]
        [InlineData("de-DE-u-xx-u-co-phonebk", "de-DE-u-xx-u-co-phonebk", "de-DE-u-xx-u-co-phonebk")]
        [InlineData("de-DE-t-xx-u-co-phonebk", "de-DE-t-xx-u-co-phonebk", "de-DE-t-xx-u-co-phonebk_phoneboo")]
        [InlineData("de-DE-u-co-phonebk-t-xx", "de-DE-t-xx", "de-DE-t-xx_phoneboo")]
        [InlineData("de-DE-u-co-phonebk-t-xx-u-yy", "de-DE-t-xx-u-yy", "de-DE-t-xx-u-yy_phoneboo")]
        [InlineData("de-DE", "de-DE", "de-DE")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization), nameof(PlatformDetection.IsNotHybridGlobalizationOnApplePlatform), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        public void TestCreationWithMangledSortName(string cultureName, string expectedCultureName, string expectedSortName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);

            Assert.Equal(expectedCultureName, ci.Name);
            Assert.Equal(expectedSortName, ci.CompareInfo.Name);
        }

        [InlineData("xx-u-XX", "xx-u-xx")]
        [InlineData("xx-u-XX-u-yy", "xx-u-xx-u-yy")]
        [InlineData("xx-t-ja-JP", "xx-t-ja-jp")]
        [InlineData("qps-plocm", "qps-PLOCM")] // ICU normalize this name to "qps--plocm" which we normalize it back to "qps-plocm"
        [InlineData("zh_CN", "zh_cn")]
        [InlineData("km_KH", "km_kh")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization), nameof(PlatformDetection.IsNotHybridGlobalizationOnApplePlatform), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser), nameof(PlatformDetection.IsNotWindowsServerCore))]
        public void TestCreationWithICUNormalizedNames(string cultureName, string expectedCultureName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            Assert.Equal(expectedCultureName, ci.Name);
        }

        private static bool SupportRemoteExecutionWithIcu => RemoteExecutor.IsSupported && PlatformDetection.IsIcuGlobalization;

        [InlineData("xx-u-XX")]
        [InlineData("xx-u-XX-u-yy")]
        [InlineData("xx-t-ja-JP")]
        [InlineData("qps-plocm")]
        [InlineData("zh-TW-u-co-zhuyin")]
        [InlineData("de-DE-u-co-phonebk")]
        [InlineData("de-DE-u-co-phonebk-u-xx")]
        [InlineData("de-DE-u-xx-u-co-phonebk")]
        [InlineData("de-DE-t-xx-u-co-phonebk")]
        [InlineData("de-DE-u-co-phonebk-t-xx")]
        [InlineData("de-DE-u-co-phonebk-t-xx-u-yy")]
        [InlineData("de-DE")]
        [ConditionalTheory(nameof(SupportRemoteExecutionWithIcu))]
        public void TestWithResourceLookup(string cultureName)
        {
            RemoteExecutor.Invoke(name => {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
                int Zero = 0;

                // This should go through the resource manager to get the localized exception message using the current UI culture
                Assert.Throws<DivideByZeroException>(() => 1 / Zero);
            }, cultureName).Dispose();
        }
    }
}
