// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_external_root } from "../roots";
import { monoStringToString, utf16ToString } from "../strings";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { Int32Ptr } from "../types/emscripten";
import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { GraphemeSegmenter } from "./grapheme-segmenter";

const COMPARISON_ERROR = -2;
const INDEXING_ERROR = -1;
let graphemeSegmenterCached: GraphemeSegmenter | null;

export function mono_wasm_compare_string(culture: MonoStringRef, str1: number, str1Length: number, str2: number, str2Length: number, options: number, is_exception: Int32Ptr, ex_address: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(ex_address);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const string1 = utf16ToString(<any>str1, <any>(str1 + 2 * str1Length));
        const string2 = utf16ToString(<any>str2, <any>(str2 + 2 * str2Length));
        const casePicker = (options & 0x1f);
        const locale = cultureName ? cultureName : undefined;
        wrap_no_error_root(is_exception, exceptionRoot);
        return compareStrings(string1, string2, locale, casePicker);
    }
    catch (ex: any) {
        wrap_error_root(is_exception, ex, exceptionRoot);
        return COMPARISON_ERROR;
    }
    finally {
        cultureRoot.release();
        exceptionRoot.release();
    }
}

export function mono_wasm_starts_with(culture: MonoStringRef, str1: number, str1Length: number, str2: number, str2Length: number, options: number, is_exception: Int32Ptr, ex_address: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(ex_address);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const prefix = decodeToCleanString(str2, str2Length);
        // no need to look for an empty string
        if (prefix.length == 0)
            return 1; // true

        const source = decodeToCleanString(str1, str1Length);
        if (source.length < prefix.length)
            return 0; //false
        const sourceOfPrefixLength = source.slice(0, prefix.length);

        const casePicker = (options & 0x1f);
        const locale = cultureName ? cultureName : undefined;
        const result = compareStrings(sourceOfPrefixLength, prefix, locale, casePicker);
        wrap_no_error_root(is_exception, exceptionRoot);
        return result === 0 ? 1 : 0; // equals ? true : false
    }
    catch (ex: any) {
        wrap_error_root(is_exception, ex, exceptionRoot);
        return INDEXING_ERROR;
    }
    finally {
        cultureRoot.release();
        exceptionRoot.release();
    }
}

export function mono_wasm_ends_with(culture: MonoStringRef, str1: number, str1Length: number, str2: number, str2Length: number, options: number, is_exception: Int32Ptr, ex_address: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(ex_address);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const suffix = decodeToCleanString(str2, str2Length);
        if (suffix.length == 0)
            return 1; // true

        const source = decodeToCleanString(str1, str1Length);
        const diff = source.length - suffix.length;
        if (diff < 0)
            return 0; //false
        const sourceOfSuffixLength = source.slice(diff, source.length);

        const casePicker = (options & 0x1f);
        const locale = cultureName ? cultureName : undefined;
        const result = compareStrings(sourceOfSuffixLength, suffix, locale, casePicker);
        wrap_no_error_root(is_exception, exceptionRoot);
        return result === 0 ? 1 : 0; // equals ? true : false
    }
    catch (ex: any) {
        wrap_error_root(is_exception, ex, exceptionRoot);
        return INDEXING_ERROR;
    }
    finally {
        cultureRoot.release();
        exceptionRoot.release();
    }
}

export function mono_wasm_index_of(culture: MonoStringRef, needlePtr: number, needleLength: number, srcPtr: number, srcLength: number, options: number, fromBeginning: number, is_exception: Int32Ptr, ex_address: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(ex_address);
    try {
        const needle = utf16ToString(<any>needlePtr, <any>(needlePtr + 2 * needleLength));
        // no need to look for an empty string
        if (cleanString(needle).length == 0) {
            wrap_no_error_root(is_exception, exceptionRoot);
            return fromBeginning ? 0 : srcLength;
        }

        const source = utf16ToString(<any>srcPtr, <any>(srcPtr + 2 * srcLength));
        // no need to look in an empty string
        if (cleanString(source).length == 0) {
            wrap_no_error_root(is_exception, exceptionRoot);
            return fromBeginning ? 0 : srcLength;
        }
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const casePicker = (options & 0x1f);
        let result = -1;

        const graphemeSegmenter = graphemeSegmenterCached || (graphemeSegmenterCached = new GraphemeSegmenter());
        const needleSegments = [];
        let needleIdx = 0;

        // Grapheme segmentation of needle string
        while (needleIdx < needle.length) {
            const needleGrapheme = graphemeSegmenter.nextGrapheme(needle, needleIdx);
            needleSegments.push(needleGrapheme);
            needleIdx += needleGrapheme.length;
        }

        let srcIdx = 0;
        while (srcIdx < source.length) {
            const srcGrapheme = graphemeSegmenter.nextGrapheme(source, srcIdx);
            srcIdx += srcGrapheme.length;

            if (!checkMatchFound(srcGrapheme, needleSegments[0], locale, casePicker)) {
                continue;
            }

            let j;
            let srcNextIdx = srcIdx;
            for (j = 1; j < needleSegments.length; j++) {
                const srcGrapheme = graphemeSegmenter.nextGrapheme(source, srcNextIdx);

                if (!checkMatchFound(srcGrapheme, needleSegments[j], locale, casePicker)) {
                    break;
                }
                srcNextIdx += srcGrapheme.length;
            }
            if (j === needleSegments.length) {
                result = srcIdx - srcGrapheme.length;
                if (fromBeginning)
                    break;
            }
        }
        wrap_no_error_root(is_exception, exceptionRoot);
        return result;
    }
    catch (ex: any) {
        wrap_error_root(is_exception, ex, exceptionRoot);
        return INDEXING_ERROR;
    }
    finally {
        cultureRoot.release();
        exceptionRoot.release();
    }

    function checkMatchFound(str1: string, str2: string, locale: string | undefined, casePicker: number): boolean {
        return compareStrings(str1, str2, locale, casePicker) === 0;
    }
}

function compareStrings(string1: string, string2: string, locale: string | undefined, casePicker: number): number {
    switch (casePicker) {
        case 0:
            // 0: None - default algorithm for the platform OR
            //    StringSort - for ICU it gives the same result as None, see: https://github.com/dotnet/dotnet-api-docs/issues
            //    does not work for "ja"
            if (locale && locale.split("-")[0] === "ja")
                return COMPARISON_ERROR;
            return string1.localeCompare(string2, locale); // a ≠ b, a ≠ á, a ≠ A
        case 8:
            // 8: IgnoreKanaType works only for "ja"
            if (locale && locale.split("-")[0] !== "ja")
                return COMPARISON_ERROR;
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

function decodeToCleanString(strPtr: number, strLen: number) {
    const str = utf16ToString(<any>strPtr, <any>(strPtr + 2 * strLen));
    return cleanString(str);
}

function cleanString(str: string) {
    const nStr = str.normalize();
    return nStr.replace(/[\u200B-\u200D\uFEFF\0]/g, "");
}
