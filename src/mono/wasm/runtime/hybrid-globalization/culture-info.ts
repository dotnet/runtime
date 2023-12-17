// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { mono_wasm_new_external_root } from "../roots";
import { monoStringToString, stringToUTF16 } from "../strings";
import { Int32Ptr } from "../types/emscripten";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { OUTER_SEPARATOR, normalizeLocale, normalizeSpaces } from "./helpers";

export function mono_wasm_get_culture_info(culture: MonoStringRef, dst: number, dstLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number
{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const cultureInfo = {
            AmDesignator: "",
            PmDesignator: "",
            LongTimePattern: "",
            ShortTimePattern: ""
        };
        const canonicalLocale = normalizeLocale(cultureName);
        const designators = getAmPmDesignators(canonicalLocale);
        cultureInfo.AmDesignator = designators.am;
        cultureInfo.PmDesignator = designators.pm;
        cultureInfo.LongTimePattern = getLongTimePattern(canonicalLocale, designators);
        cultureInfo.ShortTimePattern = getShortTimePattern(cultureInfo.LongTimePattern);
        const result = Object.values(cultureInfo).join(OUTER_SEPARATOR);
        if (result.length > dstLength)
        {
            throw new Error(`Culture info exceeds length of ${dstLength}.`);
        }
        stringToUTF16(dst, dst + 2 * result.length, result);
        wrap_no_error_root(isException, exceptionRoot);
        return result.length;
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        exceptionRoot.release();
    }
}

function getAmPmDesignators(locale: any)
{
    const pmTime = new Date("August 19, 1975 12:15:33"); // do not change, some PM hours result in hour digits change, e.g. 13 -> 01 or 1
    const amTime = new Date("August 19, 1975 11:15:33"); // do not change, some AM hours result in hour digits change, e.g. 9 -> 09
    const pmDesignator = getDesignator(pmTime, locale);
    const amDesignator = getDesignator(amTime, locale);
    return {
        am: amDesignator,
        pm: pmDesignator
    };
}

function getDesignator(time: Date, locale: string)
{
    let withDesignator = time.toLocaleTimeString(locale, { hourCycle: "h12"});
    const localizedZero = (0).toLocaleString(locale);
    if (withDesignator.includes(localizedZero))
    {
        // in v8>=11.8 "12" changes to "0" for ja-JP
        const localizedTwelve = (12).toLocaleString(locale);
        withDesignator = withDesignator.replace(localizedZero, localizedTwelve);
    }
    const withoutDesignator = time.toLocaleTimeString(locale, { hourCycle: "h24"});
    const designator = withDesignator.replace(withoutDesignator, "").trim();
    if (new RegExp("[0-9]$").test(designator)){
        const designatorParts = withDesignator.split(" ").filter(part => new RegExp("^((?![0-9]).)*$").test(part));
        if (!designatorParts || designatorParts.length == 0)
            return "";
        return designatorParts.join(" ");
    }
    return designator;
}

function getLongTimePattern(locale: string | undefined, designators: any) : string
{
    const hourIn24Format = 18; // later hours than 18 have night designators in some locales (instead of AM designator)
    const hourIn12Format = 6;
    const localizedHour24 = (hourIn24Format).toLocaleString(locale); // not all locales use arabic numbers
    const localizedHour12 = (hourIn12Format).toLocaleString(locale);
    const pmTime = new Date(`August 19, 1975 ${hourIn24Format}:15:30`); // in the comments, en-US locale is used:
    const shortTime = new Intl.DateTimeFormat(locale, { timeStyle: "medium" });
    const shortPmStyle = shortTime.format(pmTime); // 12:15:30 PM
    const minutes = pmTime.toLocaleTimeString(locale, { minute: "numeric" }); // 15
    const seconds = pmTime.toLocaleTimeString(locale, { second: "numeric" }); // 30
    let pattern = shortPmStyle.replace(designators.pm, "tt").replace(minutes, "mm").replace(seconds, "ss"); // 12:mm:ss tt

    const isISOStyle = pattern.includes(localizedHour24); // 24h or 12h pattern?
    const localized0 = (0).toLocaleString(locale);
    const hour12WithPrefix = `${localized0}${localizedHour12}`; // 06
    const amTime = new Date(`August 19, 1975 ${hourIn12Format}:15:30`);
    const h12Style = shortTime.format(amTime);
    let hourPattern;
    if (isISOStyle) // 24h
    {
        const hasPrefix = h12Style.includes(hour12WithPrefix);
        hourPattern = hasPrefix ? "HH" : "H";
        pattern = pattern.replace(localizedHour24, hourPattern);
    }
    else // 12h
    {
        const hasPrefix = h12Style.includes(hour12WithPrefix);
        hourPattern = hasPrefix ? "hh" : "h";
        pattern = pattern.replace(hasPrefix ? hour12WithPrefix : localizedHour12, hourPattern);
    }
    return normalizeSpaces(pattern);
}

function getShortTimePattern(pattern: string) : string
{
    // remove seconds:
    // short dotnet pattern does not contain seconds while JS's pattern always contains them
    const secondsIdx = pattern.indexOf("ss");
    if (secondsIdx > 0)
    {
        const secondsWithSeparator = `${pattern[secondsIdx - 1]}ss`;
        // en-US: 12:mm:ss tt -> 12:mm tt;
        // fr-CA: 12 h mm min ss s -> 12 h mm min s
        const shortPatternNoSecondsDigits = pattern.replace(secondsWithSeparator, "");
        if (shortPatternNoSecondsDigits.length > secondsIdx && shortPatternNoSecondsDigits[shortPatternNoSecondsDigits.length - 1] != "t")
        {
            pattern = pattern.split(secondsWithSeparator)[0];
        }
        else
        {
            pattern = shortPatternNoSecondsDigits;
        }
    }
    return pattern;
}
