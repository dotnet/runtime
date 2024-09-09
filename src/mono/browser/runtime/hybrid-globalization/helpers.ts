// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const SURROGATE_HIGHER_START = "\uD800";
const SURROGATE_HIGHER_END = "\uDBFF";
const SURROGATE_LOWER_START = "\uDC00";
const SURROGATE_LOWER_END = "\uDFFF";

export const OUTER_SEPARATOR = "##";
export const INNER_SEPARATOR = "||";

export function normalizeLocale (locale: string | null) {
    if (!locale)
        return undefined;
    try {
        locale = locale.toLocaleLowerCase();
        if (locale.includes("zh")) {
            // browser does not recognize "zh-chs" and "zh-cht" as equivalents of "zh-HANS" "zh-HANT", we are helping, otherwise
            // it would throw on getCanonicalLocales with "RangeError: Incorrect locale information provided"
            locale = locale.replace("chs", "HANS").replace("cht", "HANT");
        }
        const canonicalLocales = (Intl as any).getCanonicalLocales(locale.replace("_", "-"));
        return canonicalLocales.length > 0 ? canonicalLocales[0] : undefined;
    } catch {
        return undefined;
    }
}

export function isSurrogate (str: string, startIdx: number): boolean {
    return SURROGATE_HIGHER_START <= str[startIdx] &&
        str[startIdx] <= SURROGATE_HIGHER_END &&
        startIdx + 1 < str.length &&
        SURROGATE_LOWER_START <= str[startIdx + 1] &&
        str[startIdx + 1] <= SURROGATE_LOWER_END;
}
