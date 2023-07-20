import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { mono_wasm_new_external_root } from "../roots";
import { monoStringToString, stringToUTF16 } from "../strings";
import { Int32Ptr } from "../types/emscripten";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { OUTER_SEPARATOR } from "./helpers";

/* eslint-disable no-console */
/* eslint-disable no-inner-declarations */

export function mono_wasm_get_culture_info(culture: MonoStringRef, dst: number, dstLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number
{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const cultureInfo = {
            AmDesignator: "",
            PmDesignator: "",
            ShortTimePattern: "",
        };
        const canonicalLocale = normalizeLocale(locale);
        const designators = getAmPmDesignators(canonicalLocale);
        cultureInfo.AmDesignator = designators.am;
        cultureInfo.PmDesignator = designators.pm;
        const shortTimePattern = getShortTimePattern(canonicalLocale, designators);
        cultureInfo.ShortTimePattern = shortTimePattern;
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

function normalizeLocale(locale: string | undefined)
{
    try
    {
        return (Intl as any).getCanonicalLocales(locale?.replace("_", "-"));
    }
    catch(ex: any)
    {
        throw new Error(`Get culture info failed for culture = ${locale} with error: ${ex}`);
    }
}

function getAmPmDesignators(locale: any)
{
    const pmTime = new Date("August 19, 1975 12:15:30");
    const amTime = new Date("August 19, 1975 11:15:30");
    const pmDesignator = getDesignator(pmTime, locale);
    const amDesignator = getDesignator(amTime, locale);
    return {
        am: amDesignator,
        pm: pmDesignator
    };

    function getDesignator(time: Date, locale: string)
    {
        const withDesignator = time.toLocaleTimeString(locale, { hourCycle: "h12"});
        const withoutDesignator = time.toLocaleTimeString(locale, { hourCycle: "h24"});
        const designator = withDesignator.replace(withoutDesignator, "").trim();
        if (new RegExp("[0-9]$").test(designator)){
            const designatorLikeParts = withDesignator.split(" ").filter(part => new RegExp("^((?![0-9]).)*$").test(part));
            if (!designatorLikeParts || designatorLikeParts.length == 0)
                return "";
            return designatorLikeParts.join(" ");
        }
        return designator;
    }
}

function getShortTimePattern(locale: string | undefined, designators: any) : string
{
    const hourIn24Format = 18; // later hours in some locales have night designators (instead of AM)
    const hourIn12Format = 6;
    const localizedHour24 = (hourIn24Format).toLocaleString(locale);
    const localizedHour12 = (hourIn12Format).toLocaleString(locale);
    const pmTime = new Date(`August 19, 1975 ${hourIn24Format}:15:30`); // in the comments, en-US locale was used
    const shortTime = new Intl.DateTimeFormat(locale, { timeStyle: "medium" });
    const shortPmStyle = shortTime.format(pmTime); // 12:15:30 PM
    const minutes = pmTime.toLocaleTimeString(locale, { minute: "numeric" }); // 15
    let shortPattern = shortPmStyle.replace(designators.pm, "tt").replace(minutes, "mm"); // 12:mm:30 tt
    shortPattern = removeSeconds(shortPattern, pmTime); // 12:mm tt

    const isISOStyle = shortPattern.includes(localizedHour24); // 24h or 12h pattern?
    const localized0 = (0).toLocaleString(locale);
    const hour12WithPrefix = `${localized0}${localizedHour12}`; // 06
    const amTime = new Date(`August 19, 1975 ${hourIn12Format}:15:30`);
    const h12Style = shortTime.format(amTime);
    let hourPattern;
    if (isISOStyle) // 24h
    {
        const hasPrefix = h12Style.includes(hour12WithPrefix);
        hourPattern = hasPrefix ? "HH" : "H";
        shortPattern = shortPattern.replace(localizedHour24, hourPattern);
    }
    else // 12h
    {
        const hasPrefix = h12Style.includes(hour12WithPrefix);
        hourPattern = hasPrefix ? "hh" : "h";
        shortPattern = shortPattern.replace(hasPrefix ? hour12WithPrefix : localizedHour12, hourPattern);
    }

    return shortPattern;

    function removeSeconds(shortPattern: string, pmTime: Date)
    {
        // short dotnet pattern does not contain seconds while JS always contains them
        const seconds = pmTime.toLocaleTimeString(locale, { second: "numeric" });
        const secondsIdx = shortPattern.indexOf(seconds); // 12
        if (secondsIdx > 0)
        {
            const secondsWithSeparator = `${shortPattern[secondsIdx - 1]}${seconds}`;
            // en-US: 12:mm:30 tt -> 12:mm tt;
            // fr-CA: 12 h mm min 30 s -> 12 h mm min s
            const shortPatternNoSecondsDigits = shortPattern.replace(secondsWithSeparator, "");
            if (shortPatternNoSecondsDigits.length > secondsIdx && shortPatternNoSecondsDigits[shortPatternNoSecondsDigits.length - 1] != "t")
            {
                shortPattern = shortPattern.split(secondsWithSeparator)[0];
            }
            else
            {
                shortPattern = shortPatternNoSecondsDigits;
            }
        }
        return shortPattern;
    }
}