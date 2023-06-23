// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        private string JsGetLanguageDisplayName(string cultureName) =>
            JsGetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName, CultureInfo.CurrentUICulture.Name);
            // from some reason always CultureInfo.CurrentUICulture.Name == cultureName

        private string JsGetLocaleInfo(LocaleStringData type, string? uiCultureName = null)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(_sWindowsName != null, "[CultureData.JsGetLocaleInfo] Expected _sWindowsName to be populated already");
            return JsGetLocaleInfo(_sWindowsName, type, uiCultureName);
        }

        private HashSet<string> NEGATIVE_IS_MINUS_SIGN = new HashSet<string> { "et", "fa", "fi", "lt", "nb", "no", "sl", "sv" };

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private unsafe string JsGetLocaleInfo(string localeName, LocaleStringData type, string? uiCultureName = null)
        {
            string[] localeParts = localeName.Split('-');
            if (localeParts.Length == 0)
                throw new Exception("Incorrect localeName.");
            string localeFamily = localeParts[0];
            switch (type)
            {
                case LocaleStringData.LocalizedDisplayName:
                    if (uiCultureName == null)
                        return string.Empty;
                    return GetDisplayName(localeName, uiCultureName!);
                case LocaleStringData.EnglishDisplayName:
                    return GetDisplayName(localeName, "en");
                case LocaleStringData.NativeDisplayName:
                    return GetDisplayName(localeName, localeName);
                case LocaleStringData.LocalizedLanguageName:
                    if (uiCultureName == null)
                        return string.Empty;
                    return GetLanguageName(localeFamily, uiCultureName!);
                case LocaleStringData.EnglishLanguageName:
                    return GetLanguageName(localeFamily, "en");
                case LocaleStringData.NativeLanguageName:
                    return GetLanguageName(localeFamily, localeName);
                case LocaleStringData.LocalizedCountryName:
                    if (uiCultureName == null)
                        return string.Empty;
                    return GetCountryName(localeParts, uiCultureName!);
                case LocaleStringData.EnglishCountryName:
                    return GetCountryName(localeParts, "en");
                case LocaleStringData.NativeCountryName:
                    return GetCountryName(localeParts, localeName);
                //case LocaleStringData.AbbreviatedWindowsLanguageName:
                    // NLS usage only
                case LocaleStringData.ListSeparator:
                    // this enum value used only for NLS + ICU's GetLocaleDataNumericPart has this data even when locales_tree excluded
                    return IcuGetLocaleInfo(type, uiCultureName);
                case LocaleStringData.DecimalSeparator:
                    char* bufferDS = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
                    int resultLenDS = Interop.JsGlobalization.JsGetDecimalSeparator(localeName, bufferDS, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY, out int exceptionDS, out object exResultDS);
                    if (exceptionDS != 0)
                        throw new Exception((string)exResultDS);
                    return new string(bufferDS, 0, resultLenDS);
                case LocaleStringData.ThousandSeparator:
                    char* bufferTS = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
                    int resultLenTS = Interop.JsGlobalization.JsGetThousandSeparator(localeName, bufferTS, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY, out int exceptionTS, out object exResultTS);
                    if (exceptionTS != 0)
                        throw new Exception((string)exResultTS);
                    return new string(bufferTS, 0, resultLenTS);
                case LocaleStringData.Digits:
                    // only a few WASM's locales need JS's assistance "ar-SA", "bn-BD", "fa-IR", "mr-IN"
                    if (localeFamily != "ar" && localeFamily != "bn" && localeFamily != "fa" && localeFamily != "mr")
                        return "0\uFFFF1\uFFFF2\uFFFF3\uFFFF4\uFFFF5\uFFFF6\uFFFF7\uFFFF8\uFFFF9\uFFFF";
                    char* bufferD = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
                    int resultLenD = Interop.JsGlobalization.JsGetDigits(localeName, bufferD, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY, out int exceptionD, out object exResultD);
                    if (exceptionD != 0)
                        throw new Exception((string)exResultD);
                    return new string(bufferD, 0, resultLenD);
                case LocaleStringData.MonetarySymbol:
                    // ICU does not have this data for sure, we can get it using Iso4217MonetarySymbol, it's RegionInfo.ISOCurrencySymbol
                    if (localeName == "es-419")
                        return "\u00a4"; // invariant symbol
                    string isoSymbolMS = GetISOSymbolFromStaticData(localeName, localeFamily);
                    if (string.IsNullOrEmpty(isoSymbolMS))
                        return string.Empty;
                    char* bufferMS = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
                    int resultLenMS = Interop.JsGlobalization.JsGetMonetarySymbol(localeName, isoSymbolMS, bufferMS, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY, out int exceptionMS, out object exResultMS);
                    if (exceptionMS != 0)
                        throw new Exception((string)exResultMS);
                    return new string(bufferMS, 0, resultLenMS);
                case LocaleStringData.CurrencyEnglishName:
                    if (localeName == "es-419")
                        return string.Empty;
                    return GetCurrencyName(localeName, localeFamily, "en");
                case LocaleStringData.CurrencyNativeName:
                    if (localeName == "es-419")
                        return string.Empty;
                    return GetCurrencyName(localeName, localeFamily, localeName);
                case LocaleStringData.Iso4217MonetarySymbol:
                    // ICU does not have this data for sure, it's CultureData.CurrencySymbol == RegionInfo.CurrencySymbol
                    return GetISOSymbolFromStaticData(localeName, localeFamily);
                case LocaleStringData.MonetaryDecimalSeparator:
                    if (localeName == "es-419")
                        return ".";
                    string isoSymbolMDS = GetISOSymbolFromStaticData(localeName, localeFamily);
                    if (string.IsNullOrEmpty(isoSymbolMDS))
                        return string.Empty;
                    char* bufferMDS = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
                    int resultLenMDS = Interop.JsGlobalization.JsGetMonetaryDecimalSeparator(localeName, isoSymbolMDS, bufferMDS, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY, out int exceptionMDS, out object exResultMDS);
                    if (exceptionMDS != 0)
                        throw new Exception((string)exResultMDS);
                    return new string(bufferMDS, 0, resultLenMDS);
                case LocaleStringData.MonetaryThousandSeparator:
                    if (localeName == "es-419")
                        return " ";
                    string isoSymbolMTS = GetISOSymbolFromStaticData(localeName, localeFamily);
                    if (string.IsNullOrEmpty(isoSymbolMTS))
                        return string.Empty;
                    char* bufferMTS = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
                    int resultLenMTS = Interop.JsGlobalization.JsGetMonetaryThousandSeparator(localeName, isoSymbolMTS, bufferMTS, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY, out int exceptionMTS, out object exResultMTS);
                    if (exceptionMTS != 0)
                        throw new Exception((string)exResultMTS);
                    return new string(bufferMTS, 0, resultLenMTS);
                case LocaleStringData.AMDesignator:
                    // ICU still has this data - to be implemented with calendars removal
                    // calendar/Gregorian/AmPmMarkers
                    return IcuGetLocaleInfo(type, uiCultureName);
                case LocaleStringData.PMDesignator:
                    // ICU still has this data
                    return IcuGetLocaleInfo(type, uiCultureName);
                case LocaleStringData.PositiveSign:
                    if (localeFamily == "ar")
                        return "\u061c\u002b";
                    if (localeName == "fa-IR" || localeFamily == "he") // "fa" -> "\u002b"
                        return "\u200e\u002b";
                    // all the rest - plus sign
                    return "\u002b";
                case LocaleStringData.NegativeSign: // WebAPI gives incorrect answers, e.g. console.log("-1.2".toLocaleString('ar')) -> hypen-minus
                    if (localeFamily == "ar")
                        return "\u061c\u002d";
                    if (localeName == "fa-IR") // "fa" -> "\u2212"
                        return "\u200e\u2212";
                    if (localeName == "he-IL") // "he" -> "\u002d"
                        return "\u200e\u002d";
                    if (NEGATIVE_IS_MINUS_SIGN.Contains(localeFamily))
                        return "\u2212";
                    // all the rest - hyphen-minus
                    return "\u002d";
                case LocaleStringData.Iso639LanguageTwoLetterName: // same as Iso639LanguageName
                    // ICU still has this data
                    return IcuGetLocaleInfo(type, uiCultureName);
                case LocaleStringData.Iso639LanguageThreeLetterName:
                    // ICU still has this data
                    return IcuGetLocaleInfo(type, uiCultureName);
                case LocaleStringData.Iso3166CountryName:
                    // ICU still has this data
                    return IcuGetLocaleInfo(type, uiCultureName);
                case LocaleStringData.Iso3166CountryName2:
                    // ICU still has this data
                    return IcuGetLocaleInfo(type, uiCultureName);
                case LocaleStringData.NaNSymbol:
                    switch (localeFamily)
                    {
                        case "ar":
                            return "\u0644\u064a\u0633\u0020\u0631\u0642\u0645";
                        case "fa":
                            return "\u0646\u0627\u0639\u062f\u062f";
                        case "fi":
                            return "ep\u00e4luku";
                        case "lv":
                            return "NS";
                        case "ru":
                            return "\u043d\u0435\u0020\u0447\u0438\u0441\u043b\u043e";
                        case "zh":
                            return "\u975e\u6578\u503c";
                    }
                    return "NaN";
                case LocaleStringData.PositiveInfinitySymbol:
                    // WebAPI works fine here but we don't need it, as all locales should return the same
                    return IcuGetLocaleInfo(type, uiCultureName);
                    // return "\u221e";
                case LocaleStringData.NegativeInfinitySymbol:
                    return JsGetLocaleInfo(localeName, LocaleStringData.NegativeSign) +
                        JsGetLocaleInfo(localeName, LocaleStringData.PositiveInfinitySymbol);
                case LocaleStringData.ParentName:
                    // ICU still has this data even after the whole locale_tree exclusion
                    return IcuGetLocaleInfo(type, uiCultureName);
                // case LocaleStringData.ConsoleFallbackName:
                    // used by NLS only
                case LocaleStringData.PercentSymbol:
                    if (localeFamily == "ar")
                        return "\u066a\u061c";
                    if (localeFamily == "fa")
                        return "\u066a";
                    return "\u0025";
                case LocaleStringData.PerMilleSymbol:
                    if (localeFamily == "ar" || localeFamily == "fa")
                        return "\u0609";
                    return "\u2030";
            }
            return string.Empty;
        }

        private unsafe string GetDisplayName(string localeName, string nameDisplayLocale)
        {
            char* buffer = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
            int resultLen = Interop.JsGlobalization.JsGetDisplayName(nameDisplayLocale, localeName, buffer, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY, out int exception, out object exResult);
            if (exception != 0)
                throw new Exception((string)exResult);
            return new string (buffer, 0, resultLen);
        }

        private unsafe string GetLanguageName(string localeFamily, string languageDisplayLocale)
        {
            // does not work for a.e. "ar-SA" because region is SA
            char* buffer = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
            int resultLen = Interop.JsGlobalization.JsGetLanguageName(languageDisplayLocale, localeFamily, buffer, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY, out int exception, out object exResult);
            if (exception != 0)
                throw new Exception((string)exResult);
            return new string(buffer, 0, resultLen);
        }

        private unsafe string GetCountryName(string[] localeParts, string countryDisplayLocale)
        {
            if (localeParts.Length < 2)
                throw new Exception("Error when extracting Region code from Locale.");
            // zh-Hans-HK -> HK but zh-HK -> HK, es-419 -> 419
            string localeRegion = localeParts.Length == 2 ? localeParts[1] : localeParts[2];
            char* buffer = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
            int resultLen = Interop.JsGlobalization.JsGetCountryName(countryDisplayLocale, localeRegion, buffer, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY, out int exception, out object exResult);
            if (exception != 0)
                throw new Exception((string)exResult);
            return new string(buffer, 0, resultLen);
        }

        private unsafe string GetCurrencyName(string localeName, string localeFamily, string currencyDisplayLocale)
        {
            string isoSymbol = GetISOSymbolFromStaticData(localeName, localeFamily);
            if (string.IsNullOrEmpty(isoSymbol))
                return string.Empty;
            char* buffer = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
            int resultLen = Interop.JsGlobalization.JsGeCurrencyName(currencyDisplayLocale, isoSymbol, buffer, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY, out int exception, out object exResult);
            if (exception != 0)
                throw new Exception((string)exResult);
            return new string(buffer, 0, resultLen);
        }

        private string GetISOSymbolFromStaticData(string localeName, string localeFamilyName)
        {
            if (!ISO4217_CURRENCIES.TryGetValue(localeName, out string? value))
            {
                if (!ISO4217_CURRENCIES.TryGetValue(localeFamilyName, out string? valueFamily))
                {
                    Debug.Fail("[CultureData.JsGetLocaleInfo(LocaleStringData)] Failed");
                    return string.Empty;
                }
                return valueFamily;
            }
            return value;
        }

        private Dictionary<string, string> ISO4217_CURRENCIES = new Dictionary<string, string>()
        {
            { "ar", "ARS" },
            { "ar-SA", "SAR" },
            { "am", "AMD" },
            { "am-ET", "ETB" },
            { "bg", "BGN" },
            { "bn", "BND" },
            { "bn-BD", "BDT" },
            { "bn-IN", "INR" },
            { "ca", "CAD" },
            { "ca-AD", "EUR" },
            { "ca-ES", "EUR" },
            { "cs", "CSD" },
            { "cs-CZ", "CZK" },
            { "da-DK", "DKK" },
            { "de", "EUR" },
            { "de-CH", "CHF" },
            { "de-LI", "CHF" },
            { "el-CY", "EUR" },
            { "el-GR", "EUR" },
            { "en-AE", "AED" },
            { "en-AG", "XCD" },
            { "en-AI", "XCD" },
            { "en-AS", "USD" },
            { "en-AT", "EUR" },
            { "en-AU", "AUD" },
            { "en-BB", "BBD" },
            { "en-BE", "EUR" },
            { "en-BI", "BIF" },
            { "en-BM", "BMD" },
            { "en-BS", "BSD" },
            { "en-BW", "BWP" },
            { "en-BZ", "BZD" },
            { "en-CA", "CAD" },
            { "en-CC", "AUD" },
            { "en-CH", "CHF" },
            { "en-CK", "NZD" },
            { "en-CM", "XAF" },
            { "en-CX", "AUD" },
            { "en-CY", "EUR" },
            { "en-DE", "EUR" },
            { "en-DK", "DKK" },
            { "en-DM", "XCD" },
            { "en-ER", "ERN" },
            { "en-FI", "EUR" },
            { "en-FJ", "FJD" },
            { "en-FK", "FKP" },
            { "en-FM", "USD" },
            { "en-GB", "GBP" },
            { "en-GD", "XCD" },
            { "en-GG", "GBP" },
            { "en-GH", "GHS" },
            { "en-GI", "GIP" },
            { "en-GM", "GMD" },
            { "en-GU", "USD" },
            { "en-GY", "GYD" },
            { "en-HK", "HKD" },
            { "en-IE", "EUR" },
            { "en-IL", "ILS" },
            { "en-IM", "GBP" },
            { "en-IN", "INR" },
            { "en-IO", "USD" },
            { "en-JE", "GBP" },
            { "en-JM", "JMD" },
            { "en-KE", "KES" },
            { "en-KI", "AUD" },
            { "en-KN", "XCD" },
            { "en-KY", "KYD" },
            { "en-LC", "XCD" },
            { "en-LR", "LRD" },
            { "en-LS", "ZAR" },
            { "en-MG", "MGA" },
            { "en-MH", "USD" },
            { "en-MO", "MOP" },
            { "en-MP", "USD" },
            { "en-MS", "XCD" },
            { "en-MT", "EUR" },
            { "en-MU", "MUR" },
            { "en-MW", "MWK" },
            { "en-MY", "MYR" },
            { "en-NA", "NAD" },
            { "en-NF", "AUD" },
            { "en-NG", "NGN" },
            { "en-NL", "EUR" },
            { "en-NR", "AUD" },
            { "en-NU", "NZD" },
            { "en-NZ", "NZD" },
            { "en-PG", "PGK" },
            { "en-PH", "PHP" },
            { "en-PK", "PKR" },
            { "en-PN", "NZD" },
            { "en-PR", "USD" },
            { "en-PW", "USD" },
            { "en-RW", "RWF" },
            { "en-SB", "SBD" },
            { "en-SC", "SCR" },
            { "en-SD", "SDG" },
            { "en-SE", "SEK" },
            { "en-SG", "SGD" },
            { "en-SH", "SHP" },
            { "en-SI", "EUR" },
            { "en-SL", "SLL" },
            { "en-SS", "SSP" },
            { "en-SX", "ANG" },
            { "en-SZ", "SZL" },
            { "en-TC", "USD" },
            { "en-TK", "NZD" },
            { "en-TO", "TOP" },
            { "en-TT", "TTD" },
            { "en-TV", "AUD" },
            { "en-TZ", "TZS" },
            { "en-UG", "UGX" },
            { "en-UM", "USD" },
            { "en-US", "USD" },
            { "en-VC", "XCD" },
            { "en-VG", "USD" },
            { "en-VI", "USD" },
            { "en-VU", "VUV" },
            { "en-WS", "WST" },
            { "en-ZA", "ZAR" },
            { "en-ZM", "ZMW" },
            { "en-ZW", "USD" },
            { "es", "EUR" },
            { "es-419", "¤¤" },
            { "es-MX", "MXN" },
            { "et", "ETB" },
            { "et-EE", "EUR" },
            { "fa-IR", "IRR" },
            { "fi", "EUR" },
            { "fil-PH", "PHP" },
            { "fr", "EUR" },
            { "fr-CA", "CAD" },
            { "fr-CH", "CHF" },
            { "gu", "USD" },
            { "gu-IN", "INR" },
            { "he-IL", "ILS" },
            { "hi-IN", "INR" },
            { "hr", "HRK" },
            { "hr-BA", "BAM" },
            { "hu", "HUF" },
            { "id", "IDR" },
            { "it", "EUR" },
            { "it-CH", "CHF" },
            { "ja-JP", "JPY" },
            { "kn", "XCD" },
            { "kn-IN", "INR" },
            { "ko-KR", "KRW" },
            { "lt", "EUR" },
            { "lv", "EUR" },
            { "ml", "XOF" },
            { "ml-IN", "INR" },
            { "mr", "MRU" },
            { "mr-IN", "INR" },
            { "ms", "XCD" },
            { "ms-BN", "BND" },
            { "ms-MY", "MYR" },
            { "ms-SG", "SGD" },
            { "nb-NO", "NOK" },
            { "no", "NOK" },
            { "nl", "EUR" },
            { "nl-AW", "AWG" },
            { "pl", "PLN" },
            { "pt", "EUR" },
            { "pt-BR", "BRL" },
            { "ro", "RON" },
            { "ru", "RUB" },
            { "sk", "EUR" },
            { "sl", "SLL" },
            { "sl-SI", "EUR" },
            { "sr", "SRD" },
            { "sr-Cyrl-RS", "RSD" },
            { "sr-Latn-RS", "RSD" },
            { "sv", "USD" },
            { "sv-AX", "EUR" },
            { "sv-SE", "SEK" },
            { "sw-CD", "CDF" },
            { "sw-KE", "KES" },
            { "sw-TZ", "TZS" },
            { "sw-UG", "UGX" },
            { "ta-IN", "INR" },
            { "ta-LK", "LKR" },
            { "ta-MY", "MYR" },
            { "ta-SG", "SGD" },
            { "te-IN", "INR" },
            { "th", "THB" },
            { "tr", "TRY" },
            { "tr-CY", "EUR" },
            { "uk-UA", "UAH" },
            { "vi", "USD" },
            { "vi-VN", "VND" },
            { "zh-CN", "CNY" },
            { "zh-Hans-HK", "HKD" },
            { "zh-SG", "SGD" },
            { "zh-HK", "HKD" },
            { "zh-TW", "TWD" },
        };
    }
}
