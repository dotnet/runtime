// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { setI32 } from "../memory";
import { stringToUTF16, stringToUTF16Ptr, utf16ToString } from "../strings";
import { Int32Ptr, VoidPtr } from "../types/emscripten";
import { VoidPtrNull } from "../types/internal";
import { OUTER_SEPARATOR, normalizeLocale } from "./helpers";

export function mono_wasm_get_locale_info (culture: number, cultureLength: number, locale: number, localeLength: number, dst: number, dstMaxLength: number, dstLength: Int32Ptr): VoidPtr {
    try {
        const localeNameOriginal = utf16ToString(<any>locale, <any>(locale + 2 * localeLength));
        const localeName = normalizeLocale(localeNameOriginal);
        if (!localeName && localeNameOriginal) {
            // handle non-standard or malformed locales by forwarding the locale code
            stringToUTF16(dst, dst + 2 * localeNameOriginal.length, localeNameOriginal);
            setI32(dstLength, localeNameOriginal.length);
            return VoidPtrNull;
        }
        const cultureNameOriginal = utf16ToString(<any>culture, <any>(culture + 2 * cultureLength));
        const cultureName = normalizeLocale(cultureNameOriginal);

        if (!localeName || !cultureName)
            throw new Error(`Locale or culture name is null or empty. localeName=${localeName}, cultureName=${cultureName}`);

        const localeParts = localeName.split("-");
        // cultureName can be in a form of:
        // 1) "language", e.g. "zh"
        // 2) "language-region", e.g. "zn-CN"
        // 3) "language-script-region", e.g. "zh-Hans-CN"
        // 4) "language-script", e.g. "zh-Hans" (served in the catch block below)
        let languageName, regionName;
        try {
            const region = localeParts.length > 1 ? localeParts.pop() : undefined;
            // this line might fail if form 4 from the comment above is used:
            regionName = region ? new Intl.DisplayNames([cultureName], { type: "region" }).of(region) : undefined;
            const language = localeParts.join("-");
            languageName = new Intl.DisplayNames([cultureName], { type: "language" }).of(language);
        } catch (error) {
            if (error instanceof RangeError) {
                // if it failed from this reason then cultureName is in a form "language-script", without region
                try {
                    languageName = new Intl.DisplayNames([cultureName], { type: "language" }).of(localeName);
                } catch (error) {
                    if (error instanceof RangeError && localeNameOriginal) {
                        // handle non-standard or malformed locales by forwarding the locale code, e.g. "xx-u-xx"
                        stringToUTF16(dst, dst + 2 * localeNameOriginal.length, localeNameOriginal);
                        setI32(dstLength, localeNameOriginal.length);
                        return VoidPtrNull;
                    }
                    throw error;
                }
            } else {
                throw error;
            }
        }
        const localeInfo = {
            LanguageName: languageName,
            RegionName: regionName,
        };
        const result = Object.values(localeInfo).join(OUTER_SEPARATOR);

        if (!result)
            throw new Error(`Locale info for locale=${localeName} is null or empty.`);

        if (result.length > dstMaxLength)
            throw new Error(`Locale info for locale=${localeName} exceeds length of ${dstMaxLength}.`);

        stringToUTF16(dst, dst + 2 * result.length, result);
        setI32(dstLength, result.length);
        return VoidPtrNull;
    } catch (ex: any) {
        setI32(dstLength, -1);
        return stringToUTF16Ptr(ex.toString());
    }
}

export function mono_wasm_get_first_day_of_week (culture: number, cultureLength: number, resultPtr: Int32Ptr): VoidPtr {
    try {
        const cultureName = utf16ToString(<any>culture, <any>(culture + 2 * cultureLength));
        const canonicalLocale = normalizeLocale(cultureName);
        const result = getFirstDayOfWeek(canonicalLocale);
        setI32(resultPtr, result);
        return VoidPtrNull;
    } catch (ex: any) {
        setI32(resultPtr, -1);
        return stringToUTF16Ptr(ex.toString());
    }
}

export function mono_wasm_get_first_week_of_year (culture: number, cultureLength: number, resultPtr: Int32Ptr): VoidPtr {
    try {
        const cultureName = utf16ToString(<any>culture, <any>(culture + 2 * cultureLength));
        const canonicalLocale = normalizeLocale(cultureName);
        const result = getFirstWeekOfYear(canonicalLocale);
        setI32(resultPtr, result);
        return VoidPtrNull;
    } catch (ex: any) {
        setI32(resultPtr, -1);
        return stringToUTF16Ptr(ex.toString());
    }
}

function getFirstDayOfWeek (locale: string) {
    const weekInfo = getWeekInfo(locale);
    if (weekInfo) {
        // JS's Sunday == 7 while dotnet's Sunday == 0
        return weekInfo.firstDay == 7 ? 0 : weekInfo.firstDay;
    }
    // Firefox does not support it rn but we can make a temporary workaround for it,
    // that should be removed when it starts being supported:
    const saturdayLocales = ["en-AE", "en-SD", "fa-IR"];
    if (saturdayLocales.includes(locale)) {
        return 6;
    }
    const sundayLanguages = ["th", "pt", "mr", "ml", "ko", "kn", "ja", "id", "hi", "he", "gu", "fil", "bn", "am", "ar", "te"];
    const sundayLocales = ["ta-SG", "ta-IN", "sw-KE", "ms-SG", "fr-CA", "es-MX", "en-US", "en-ZW", "en-ZA", "en-WS", "en-VI", "en-UM", "en-TT", "en-SG", "en-PR", "en-PK", "en-PH", "en-MT", "en-MO", "en-MH", "en-KE", "en-JM", "en-IN", "en-IL", "en-HK", "en-GU", "en-DM", "en-CA", "en-BZ", "en-BW", "en-BS", "en-AS", "en-AG", "zh-Hans-HK", "zh-SG", "zh-HK", "zh-TW"]; // "en-AU" is Monday in chrome, so firefox should be in line
    const localeLang = locale.split("-")[0];
    if (sundayLanguages.includes(localeLang) || sundayLocales.includes(locale)) {
        return 0;
    }
    return 1;
}

function getFirstWeekOfYear (locale: string) {
    const weekInfo = getWeekInfo(locale);
    if (weekInfo) {
        // enum CalendarWeekRule
        // FirstDay = 0,           // when minimalDays < 4
        // FirstFullWeek = 1,      // when miminalDays == 7
        // FirstFourDayWeek = 2    // when miminalDays >= 4
        return weekInfo.minimalDays == 7 ? 1 :
            weekInfo.minimalDays < 4 ? 0 : 2;
    }
    // Firefox does not support it rn but we can make a temporary workaround for it,
    // that should be removed when it starts being supported:
    const firstFourDayWeekLocales = ["pt-PT", "fr-CH", "fr-FR", "fr-BE", "es-ES", "en-SE", "en-NL", "en-JE", "en-IM", "en-IE", "en-GI", "en-GG", "en-GB", "en-FJ", "en-FI", "en-DK", "en-DE", "en-CH", "en-BE", "en-AT", "el-GR", "nl-BE", "nl-NL"];
    const firstFourDayWeekLanguages = ["sv", "sk", "ru", "pl", "no", "nb", "lt", "it", "hu", "fi", "et", "de", "da", "cs", "ca", "bg"];
    const localeLang = locale.split("-")[0];
    if (firstFourDayWeekLocales.includes(locale) || firstFourDayWeekLanguages.includes(localeLang)) {
        return 2;
    }
    return 0;
}

function getWeekInfo (locale: string) {
    try {
        // most tools have it implemented as property
        return (new Intl.Locale(locale) as any).weekInfo;
    } catch {
        try {
            // but a few use methods, which is the preferred way
            return (new Intl.Locale(locale) as any).getWeekInfo();
        } catch {
            return undefined;
        }
    }
}
