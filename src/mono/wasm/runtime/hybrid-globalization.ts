// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_external_root } from "./roots";
import { MonoString, MonoStringRef } from "./types/internal";
import { Int32Ptr } from "./types/emscripten";
import { conv_string_root, js_string_to_mono_string_root, string_decoder } from "./strings";
import { setU16_unchecked } from "./memory";

export function mono_wasm_change_case_invariant(exceptionMessage: Int32Ptr, src: number, srcLength: number, dst: number, dstLength: number, toUpper: number): void {
    try {
        const input = string_decoder.decode(<any>src, <any>(src + 2 * srcLength));
        let result = toUpper ? input.toUpperCase() : input.toLowerCase();
        // Unicode defines some codepoints which expand into multiple codepoints,
        // originally we do not support this expansion
        if (result.length > dstLength)
            result = input;

        for (let i = 0, j = dst; i < result.length; i++, j += 2)
            setU16_unchecked(j, result.charCodeAt(i));
    }
    catch (ex: any) {
        pass_exception_details(ex, exceptionMessage);
    }
}

export function mono_wasm_change_case(exceptionMessage: Int32Ptr, culture: MonoStringRef, src: number, srcLength: number, dst: number, destLength: number, toUpper: number): void {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture);
    try {
        const cultureName = conv_string_root(cultureRoot);
        if (!cultureName)
            throw new Error("Cannot change case, the culture name is null.");
        const input = string_decoder.decode(<any>src, <any>(src + 2 * srcLength));
        let result = toUpper ? input.toLocaleUpperCase(cultureName) : input.toLocaleLowerCase(cultureName);
        if (result.length > destLength)
            result = input;

        for (let i = 0, j = dst; i < result.length; i++, j += 2)
            setU16_unchecked(j, result.charCodeAt(i));
    }
    catch (ex: any) {
        pass_exception_details(ex, exceptionMessage);
    }
    finally {
        cultureRoot.release();
    }
}

export function mono_wasm_compare_string(exceptionMessage: Int32Ptr, culture: MonoStringRef, str1: number, str1Length: number, str2: number, str2Length: number, options: number): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture);
    try {
        const cultureName = conv_string_root(cultureRoot);
        const string1 = string_decoder.decode(<any>str1, <any>(str1 + 2 * str1Length));
        const string2 = string_decoder.decode(<any>str2, <any>(str2 + 2 * str2Length));
        const casePicker = (options & 0x1f);
        const locale = cultureName ? cultureName : undefined;
        return compare_strings(string1, string2, locale, casePicker);
    }
    catch (ex: any) {
        pass_exception_details(ex, exceptionMessage);
        return -2;
    }
    finally {
        cultureRoot.release();
    }
}

function pass_exception_details(ex: any, exceptionMessage: Int32Ptr) {
    const exceptionJsString = ex.message + "\n" + ex.stack;
    const exceptionRoot = mono_wasm_new_external_root<MonoString>(<any>exceptionMessage);
    js_string_to_mono_string_root(exceptionJsString, exceptionRoot);
    exceptionRoot.release();
}

export function mono_wasm_starts_with(exceptionMessage: Int32Ptr, culture: MonoStringRef, srcPtr: number, srcLength: number, prefixPtr: number, prefixLength: number, options: number): number{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture);
    try {
        const cultureName = conv_string_root(cultureRoot);
        const prefix = decode_to_clean_string(prefixPtr, prefixLength);
        // no need to look for an empty string
        if (prefix.length == 0)
            return 1; // true

        const source = decode_to_clean_string(srcPtr, srcLength);
        if (source.length < prefix.length)
            return 0; //false
        const sourceOfPrefixLength = source.slice(0, prefix.length);

        const casePicker = (options & 0x1f);
        const locale = cultureName ? cultureName : undefined;
        const result = compare_strings(sourceOfPrefixLength, prefix, locale, casePicker);
        return result === 0 ? 1 : 0; // equals ? true : false
    }
    catch (ex: any) {
        pass_exception_details(ex, exceptionMessage);
        return -1;
    }
    finally {
        cultureRoot.release();
    }
}

export function mono_wasm_ends_with(exceptionMessage: Int32Ptr, culture: MonoStringRef, srcPtr: number, srcLength: number, suffixPtr: number, suffixLength: number, options: number): number{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture);
    try {
        const cultureName = conv_string_root(cultureRoot);
        const suffix = decode_to_clean_string(suffixPtr, suffixLength);
        if (suffix.length == 0)
            return 1; // true

        const source = decode_to_clean_string(srcPtr, srcLength);
        const diff = source.length - suffix.length;
        if (diff < 0)
            return 0; //false
        const sourceOfSuffixLength = source.slice(diff, source.length);

        const casePicker = (options & 0x1f);
        const locale = cultureName ? cultureName : undefined;
        const result = compare_strings(sourceOfSuffixLength, suffix, locale, casePicker);
        return result === 0 ? 1 : 0; // equals ? true : false
    }
    catch (ex: any) {
        pass_exception_details(ex, exceptionMessage);
        return -1;
    }
    finally {
        cultureRoot.release();
    }
}

function decode_to_clean_string(strPtr: number, strLen: number)
{
    const str = string_decoder.decode(<any>strPtr, <any>(strPtr + 2*strLen));
    return clean_string(str);
}

function clean_string(str: string)
{
    const nStr = str.normalize();
    return nStr.replace(/[\u200B-\u200D\uFEFF\0]/g, "");
}

export function mono_wasm_index_of(exceptionMessage: Int32Ptr, culture: MonoStringRef, needlePtr: number, needleLength: number, srcPtr: number, srcLength: number, options: number, fromBeginning: number): number{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture);
    try {
        const needle = string_decoder.decode(<any>needlePtr, <any>(needlePtr + 2*needleLength));
        // no need to look for an empty string
        if (clean_string(needle).length == 0)
            return fromBeginning ? 0 : srcLength;

        const source = string_decoder.decode(<any>srcPtr, <any>(srcPtr + 2*srcLength));
        // no need to look in an empty string
        if (clean_string(source).length == 0)
            return fromBeginning ? 0 : srcLength;
        const cultureName = conv_string_root(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const casePicker = (options & 0x1f);

        const segmenter = new Intl.Segmenter(locale, { granularity: "grapheme" });
        const needleSegments = Array.from(segmenter.segment(needle)).map(s => s.segment);
        let i = 0;
        let stop = false;
        let result = -1;
        let segmentWidth = 0;
        let index = 0;
        let nextIndex = 0;
        while (!stop)
        {
            // we need to restart the iterator in this outer loop because we have shifted it in the inner loop
            const iteratorSrc = segmenter.segment(source.slice(i, source.length))[Symbol.iterator]();
            let srcNext = iteratorSrc.next();

            if (srcNext.done)
                break;

            let matchFound = check_match_found(srcNext.value.segment, needleSegments[0], locale, casePicker);
            index = nextIndex;
            srcNext = iteratorSrc.next();
            if (srcNext.done)
            {
                result = matchFound ? index : result;
                break;
            }
            segmentWidth = srcNext.value.index;
            nextIndex = index + segmentWidth;
            if (matchFound)
            {
                for(let j=1; j<needleSegments.length; j++)
                {
                    if (srcNext.done)
                    {
                        stop = true;
                        break;
                    }
                    matchFound = check_match_found(srcNext.value.segment, needleSegments[j], locale, casePicker);
                    if (!matchFound)
                        break;

                    srcNext = iteratorSrc.next();
                }
                if (stop)
                    break;
            }

            if (matchFound)
            {
                result = index;
                if (fromBeginning)
                    break;
            }
            i = nextIndex;
        }
        return result;
    }
    catch (ex: any) {
        pass_exception_details(ex, exceptionMessage);
        return -1;
    }
    finally {
        cultureRoot.release();
    }

    function check_match_found(str1: string, str2: string, locale: string | undefined, casePicker: number) : boolean
    {
        return compare_strings(str1, str2, locale, casePicker) === 0;
    }
}

export function compare_strings(string1: string, string2: string, locale: string | undefined, casePicker: number): number {
    switch (casePicker) {
        case 0:
            // 0: None - default algorithm for the platform OR
            //    StringSort - since .Net 5 StringSort gives the same result as None, even for hyphen etc.
            //    does not work for "ja"
            if (locale && locale.startsWith("ja"))
                return -2;
            return string1.localeCompare(string2, locale); // a ≠ b, a ≠ á, a ≠ A
        case 8:
            // 8: IgnoreKanaType works only for "ja"
            if (locale && !locale.startsWith("ja"))
                return -2;
            return string1.localeCompare(string2, locale); // a ≠ b, a ≠ á, a ≠ A
        case 1:
            // 1: IgnoreCase
            string1 = string1.toLocaleLowerCase(locale);
            string2 = string2.toLocaleLowerCase(locale);
            return string1.localeCompare(string2, locale); // a ≠ b, a ≠ á, a ≠ A
        case 4:
        case 12:
            // 4: IgnoreSymbols
            // 12: IgnoreKanaType | IgnoreSymbols
            return string1.localeCompare(string2, locale, { ignorePunctuation: true }); // by default ignorePunctuation: false
        case 5:
            // 5: IgnoreSymbols | IgnoreCase
            string1 = string1.toLocaleLowerCase(locale);
            string2 = string2.toLocaleLowerCase(locale);
            return string1.localeCompare(string2, locale, { ignorePunctuation: true }); // a ≠ b, a ≠ á, a ≠ A
        case 9:
            // 9: IgnoreKanaType | IgnoreCase
            return string1.localeCompare(string2, locale, { sensitivity: "accent" }); // a ≠ b, a ≠ á, a = A
        case 10:
            // 10: IgnoreKanaType | IgnoreNonSpace
            return string1.localeCompare(string2, locale, { sensitivity: "case" }); // a ≠ b, a = á, a ≠ A
        case 11:
            // 11: IgnoreKanaType | IgnoreNonSpace | IgnoreCase
            return string1.localeCompare(string2, locale, { sensitivity: "base" }); // a ≠ b, a = á, a = A
        case 13:
            // 13: IgnoreKanaType | IgnoreCase | IgnoreSymbols
            return string1.localeCompare(string2, locale, { sensitivity: "accent", ignorePunctuation: true }); // a ≠ b, a ≠ á, a = A
        case 14:
            // 14: IgnoreKanaType | IgnoreSymbols | IgnoreNonSpace
            return string1.localeCompare(string2, locale, { sensitivity: "case", ignorePunctuation: true });// a ≠ b, a = á, a ≠ A
        case 15:
            // 15: IgnoreKanaType | IgnoreSymbols | IgnoreNonSpace | IgnoreCase
            return string1.localeCompare(string2, locale, { sensitivity: "base", ignorePunctuation: true }); // a ≠ b, a = á, a = A
        case 2:
        case 3:
        case 6:
        case 7:
        case 16:
        case 17:
        case 18:
        case 19:
        case 20:
        case 21:
        case 22:
        case 23:
        case 24:
        case 25:
        case 26:
        case 27:
        case 28:
        case 29:
        case 30:
        case 31:
        default:
            // 2: IgnoreNonSpace
            // 3: IgnoreNonSpace | IgnoreCase
            // 6: IgnoreSymbols | IgnoreNonSpace
            // 7: IgnoreSymbols | IgnoreNonSpace | IgnoreCase
            // 16: IgnoreWidth
            // 17: IgnoreWidth | IgnoreCase
            // 18: IgnoreWidth | IgnoreNonSpace
            // 19: IgnoreWidth | IgnoreNonSpace | IgnoreCase
            // 20: IgnoreWidth | IgnoreSymbols
            // 21: IgnoreWidth | IgnoreSymbols | IgnoreCase
            // 22: IgnoreWidth | IgnoreSymbols | IgnoreNonSpace
            // 23: IgnoreWidth | IgnoreSymbols | IgnoreNonSpace | IgnoreCase
            // 24: IgnoreKanaType | IgnoreWidth
            // 25: IgnoreKanaType | IgnoreWidth | IgnoreCase
            // 26: IgnoreKanaType | IgnoreWidth | IgnoreNonSpace
            // 27: IgnoreKanaType | IgnoreWidth | IgnoreNonSpace | IgnoreCase
            // 28: IgnoreKanaType | IgnoreWidth | IgnoreSymbols
            // 29: IgnoreKanaType | IgnoreWidth | IgnoreSymbols | IgnoreCase
            // 30: IgnoreKanaType | IgnoreWidth | IgnoreSymbols | IgnoreNonSpace
            // 31: IgnoreKanaType | IgnoreWidth | IgnoreSymbols | IgnoreNonSpace | IgnoreCase
            throw new Error(`Invalid comparison option. Option=${casePicker}`);
    }
}
