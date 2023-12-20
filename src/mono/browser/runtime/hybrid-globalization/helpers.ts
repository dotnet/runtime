// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export const OUTER_SEPARATOR = "##";
export const INNER_SEPARATOR = "||";

export function normalizeLocale(locale: string | null)
{
    if (!locale)
        return undefined;
    try
    {
        locale = locale.toLocaleLowerCase();
        if (locale.includes("zh"))
        {
            // browser does not recognize "zh-chs" and "zh-cht" as equivalents of "zh-HANS" "zh-HANT", we are helping, otherwise
            // it would throw on getCanonicalLocales with "RangeError: Incorrect locale information provided"
            locale = locale.replace("chs", "HANS").replace("cht", "HANT");
        }
        const canonicalLocales = (Intl as any).getCanonicalLocales(locale.replace("_", "-"));
        return canonicalLocales.length > 0 ? canonicalLocales[0] : undefined;
    }
    catch(ex: any)
    {
        throw new Error(`Get culture info failed for culture = ${locale} with error: ${ex}`);
    }
}

export function normalizeSpaces(pattern: string)
{
    if (!pattern.includes("\u202F"))
        return pattern;

    // if U+202F present, replace them with spaces
    return pattern.replace("\u202F", "\u0020");
}
