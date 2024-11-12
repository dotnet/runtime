// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { VoidPtrNull } from "../types/internal";
import { runtimeHelpers } from "./module-exports";
import { Int32Ptr, VoidPtr } from "../types/emscripten";
import { OUTER_SEPARATOR, normalizeLocale } from "./helpers";

const NO_PREFIX_24H = "H";
const PREFIX_24H = "HH";
const NO_PREFIX_12H = "h";
const PREFIX_12H = "hh";
const SECONDS_CODE = "ss";
const MINUTES_CODE = "mm";
const DESIGNATOR_CODE = "tt";
// Note: wrapSubstrings
// The character "h" can be ambiguous as it might represent an hour code hour code and a fixed (quoted) part of the format.
// Special Case for "fr-CA": Always recognize "HH" as a keyword and do not quote it, to avoid formatting issues.
const keyWords = [SECONDS_CODE, MINUTES_CODE, DESIGNATOR_CODE, PREFIX_24H];

export function mono_wasm_get_culture_info (culture: number, cultureLength: number, dst: number, dstMaxLength: number, dstLength: Int32Ptr): VoidPtr {
    try {
        const cultureName = runtimeHelpers.utf16ToString(<any>culture, <any>(culture + 2 * cultureLength));
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
        if (result.length > dstMaxLength) {
            throw new Error(`Culture info exceeds length of ${dstMaxLength}.`);
        }
        runtimeHelpers.stringToUTF16(dst, dst + 2 * result.length, result);
        runtimeHelpers.setI32(dstLength, result.length);
        return VoidPtrNull;
    } catch (ex: any) {
        runtimeHelpers.setI32(dstLength, -1);
        return runtimeHelpers.stringToUTF16Ptr(ex.toString());
    }
}

function getAmPmDesignators (locale: any) {
    const pmTime = new Date("August 19, 1975 12:15:33"); // do not change, some PM hours result in hour digits change, e.g. 13 -> 01 or 1
    const amTime = new Date("August 19, 1975 11:15:33"); // do not change, some AM hours result in hour digits change, e.g. 9 -> 09
    const pmDesignator = getDesignator(pmTime, locale);
    const amDesignator = getDesignator(amTime, locale);
    return {
        am: amDesignator,
        pm: pmDesignator
    };
}

function getDesignator (time: Date, locale: string) {
    let withDesignator = time.toLocaleTimeString(locale, { hourCycle: "h12" });
    const localizedZero = (0).toLocaleString(locale);
    if (withDesignator.includes(localizedZero)) {
        // in v8>=11.8 "12" changes to "0" for ja-JP
        const localizedTwelve = (12).toLocaleString(locale);
        withDesignator = withDesignator.replace(localizedZero, localizedTwelve);
    }
    const withoutDesignator = time.toLocaleTimeString(locale, { hourCycle: "h24" });
    const designator = withDesignator.replace(withoutDesignator, "").trim();
    if (new RegExp("[0-9]$").test(designator)) {
        const designatorParts = withDesignator.split(" ").filter(part => new RegExp("^((?![0-9]).)*$").test(part));
        if (!designatorParts || designatorParts.length == 0)
            return "";
        return designatorParts.join(" ");
    }
    return designator;
}

function getLongTimePattern (locale: string | undefined, designators: any): string {
    const hourIn24Format = 18; // later hours than 18 have night designators in some locales (instead of AM designator)
    const hourIn12Format = 6;
    const localizedHour24 = (hourIn24Format).toLocaleString(locale); // not all locales use arabic numbers
    const localizedHour12 = (hourIn12Format).toLocaleString(locale);
    const pmTime = new Date(`August 19, 1975 ${hourIn24Format}:15:30`); // in the comments, en-US locale is used:
    const shortTime = new Intl.DateTimeFormat(locale, { timeStyle: "medium" });
    const shortPmStyle = shortTime.format(pmTime); // 12:15:30 PM
    const minutes = pmTime.toLocaleTimeString(locale, { minute: "numeric" }); // 15
    const seconds = pmTime.toLocaleTimeString(locale, { second: "numeric" }); // 30
    let pattern = shortPmStyle.replace(designators.pm, DESIGNATOR_CODE).replace(minutes, MINUTES_CODE).replace(seconds, SECONDS_CODE); // 12:mm:ss tt

    const isISOStyle = pattern.includes(localizedHour24); // 24h or 12h pattern?
    const localized0 = (0).toLocaleString(locale);
    const hour12WithPrefix = `${localized0}${localizedHour12}`; // 06
    const amTime = new Date(`August 19, 1975 ${hourIn12Format}:15:30`);
    const h12Style = shortTime.format(amTime);
    let hourPattern;
    if (isISOStyle) { // 24h
        const hasPrefix = h12Style.includes(hour12WithPrefix);
        hourPattern = hasPrefix ? PREFIX_24H : NO_PREFIX_24H;
        pattern = pattern.replace(localizedHour24, hourPattern);
    } else { // 12h
        const hasPrefix = h12Style.includes(hour12WithPrefix);
        hourPattern = hasPrefix ? PREFIX_12H : NO_PREFIX_12H;
        pattern = pattern.replace(hasPrefix ? hour12WithPrefix : localizedHour12, hourPattern);
    }
    return wrapSubstrings(pattern);
}

function getShortTimePattern (pattern: string): string {
    // remove seconds:
    // short dotnet pattern does not contain seconds while JS's pattern always contains them
    const secondsIdx = pattern.indexOf(SECONDS_CODE);
    if (secondsIdx > 0) {
        const secondsWithSeparator = `${pattern[secondsIdx - 1]}${SECONDS_CODE}`;
        // en-US: 12:mm:ss tt -> 12:mm tt;
        // fr-CA: 12 h mm min ss s -> 12 h mm min s
        const shortPatternNoSecondsDigits = pattern.replace(secondsWithSeparator, "");
        if (shortPatternNoSecondsDigits.length > secondsIdx && shortPatternNoSecondsDigits[shortPatternNoSecondsDigits.length - 1] != "t") {
            pattern = pattern.split(secondsWithSeparator)[0];
        } else {
            pattern = shortPatternNoSecondsDigits;
        }
    }
    return pattern;
}

// wraps all substrings in the format in quotes, except for key words
// transform e.g. "HH h mm min ss s" into "HH 'h' mm 'min' ss 's'"
function wrapSubstrings (str: string) {
    const words = str.split(/\s+/);

    for (let i = 0; i < words.length; i++) {
        if (!words[i].includes(":") && !words[i].includes(".") && !keyWords.includes(words[i])) {
            words[i] = `'${words[i]}'`;
        }
    }

    return words.join(" ");
}
