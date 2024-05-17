// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { VoidPtrNull } from "./types/internal";
import { Int32Ptr, VoidPtr } from "./types/emscripten";
import { OUTER_SEPARATOR, normalizeLocale } from "./hybrid-globalization/helpers";
import { stringToUTF16, stringToUTF16Ptr, utf16ToString } from "./strings";
import { setI32 } from "./memory";

// functions common for Hybrid Globalization -> true | false :

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
