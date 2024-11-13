// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { VoidPtrNull } from "../types/internal";
import { runtimeHelpers } from "./module-exports";
import { Int32Ptr, VoidPtr } from "../types/emscripten";
import { GraphemeSegmenter } from "./grapheme-segmenter";

const COMPARISON_ERROR = -2;
const INDEXING_ERROR = -1;
let graphemeSegmenterCached: GraphemeSegmenter | null;

export function mono_wasm_compare_string (culture: number, cultureLength: number, str1: number, str1Length: number, str2: number, str2Length: number, options: number, resultPtr: Int32Ptr): VoidPtr {
    try {
        const cultureName = runtimeHelpers.utf16ToString(<any>culture, <any>(culture + 2 * cultureLength));
        const string1 = runtimeHelpers.utf16ToString(<any>str1, <any>(str1 + 2 * str1Length));
        const string2 = runtimeHelpers.utf16ToString(<any>str2, <any>(str2 + 2 * str2Length));
        const compareOptions = (options & 0x3f);
        const locale = cultureName ? cultureName : undefined;
        const result = compareStrings(string1, string2, locale, compareOptions);
        runtimeHelpers.setI32(resultPtr, result);
        return VoidPtrNull;
    } catch (ex: any) {
        runtimeHelpers.setI32(resultPtr, COMPARISON_ERROR);
        return runtimeHelpers.stringToUTF16Ptr(ex.toString());
    }
}

export function mono_wasm_starts_with (culture: number, cultureLength: number, str1: number, str1Length: number, str2: number, str2Length: number, options: number, resultPtr: Int32Ptr): VoidPtr {
    try {
        const cultureName = runtimeHelpers.utf16ToString(<any>culture, <any>(culture + 2 * cultureLength));
        const prefix = decodeToCleanString(str2, str2Length);
        // no need to look for an empty string
        if (prefix.length == 0) {
            runtimeHelpers.setI32(resultPtr, 1); // true
            return VoidPtrNull;
        }

        const source = decodeToCleanString(str1, str1Length);
        if (source.length < prefix.length) {
            runtimeHelpers.setI32(resultPtr, 0); // false
            return VoidPtrNull;
        }
        const sourceOfPrefixLength = source.slice(0, prefix.length);

        const casePicker = (options & 0x3f);
        const locale = cultureName ? cultureName : undefined;
        const cmpResult = compareStrings(sourceOfPrefixLength, prefix, locale, casePicker);
        const result = cmpResult === 0 ? 1 : 0; // equals ? true : false
        runtimeHelpers.setI32(resultPtr, result);
        return VoidPtrNull;
    } catch (ex: any) {
        runtimeHelpers.setI32(resultPtr, INDEXING_ERROR);
        return runtimeHelpers.stringToUTF16Ptr(ex.toString());
    }
}

export function mono_wasm_ends_with (culture: number, cultureLength: number, str1: number, str1Length: number, str2: number, str2Length: number, options: number, resultPtr: Int32Ptr): VoidPtr {
    try {
        const cultureName = runtimeHelpers.utf16ToString(<any>culture, <any>(culture + 2 * cultureLength));
        const suffix = decodeToCleanString(str2, str2Length);
        if (suffix.length == 0) {
            runtimeHelpers.setI32(resultPtr, 1); // true
            return VoidPtrNull;
        }

        const source = decodeToCleanString(str1, str1Length);
        const diff = source.length - suffix.length;
        if (diff < 0) {
            runtimeHelpers.setI32(resultPtr, 0); // false
            return VoidPtrNull;
        }
        const sourceOfSuffixLength = source.slice(diff, source.length);

        const casePicker = (options & 0x3f);
        const locale = cultureName ? cultureName : undefined;
        const cmpResult = compareStrings(sourceOfSuffixLength, suffix, locale, casePicker);
        const result = cmpResult === 0 ? 1 : 0; // equals ? true : false
        runtimeHelpers.setI32(resultPtr, result);
        return VoidPtrNull;
    } catch (ex: any) {
        runtimeHelpers.setI32(resultPtr, INDEXING_ERROR);
        return runtimeHelpers.stringToUTF16Ptr(ex.toString());
    }
}

export function mono_wasm_index_of (culture: number, cultureLength: number, needlePtr: number, needleLength: number, srcPtr: number, srcLength: number, options: number, fromBeginning: number, resultPtr: Int32Ptr): VoidPtr {
    try {
        const needle = decodeToCleanStringForIndexing(needlePtr, needleLength);
        // no need to look for an empty string
        if (cleanString(needle).length == 0) {
            runtimeHelpers.setI32(resultPtr, fromBeginning ? 0 : srcLength);
            return VoidPtrNull;
        }

        const source = decodeToCleanStringForIndexing(srcPtr, srcLength);
        // no need to look in an empty string
        if (cleanString(source).length == 0) {
            runtimeHelpers.setI32(resultPtr, fromBeginning ? 0 : srcLength);
            return VoidPtrNull;
        }
        const cultureName = runtimeHelpers.utf16ToString(<any>culture, <any>(culture + 2 * cultureLength));
        const locale = cultureName ? cultureName : undefined;
        const casePicker = (options & 0x3f);
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
        runtimeHelpers.setI32(resultPtr, result);
        return VoidPtrNull;
    } catch (ex: any) {
        runtimeHelpers.setI32(resultPtr, INDEXING_ERROR);
        return runtimeHelpers.stringToUTF16Ptr(ex.toString());
    }

    function checkMatchFound (str1: string, str2: string, locale: string | undefined, casePicker: number): boolean {
        return compareStrings(str1, str2, locale, casePicker) === 0;
    }
}

function compareStrings (string1: string, string2: string, locale: string | undefined, compareOptions: number): number {
    // 0: None - default algorithm for the platform OR
    //    StringSort - for ICU it gives the same result as None, see: https://github.com/dotnet/dotnet-api-docs/issues
    if (compareOptions === 0) {
        // does not work for "ja"
        if (locale && locale.split("-")[0].toLowerCase() === "ja") {
            return COMPARISON_ERROR;
        }

        return string1.localeCompare(string2, locale);
    }

    // If the user passed in only IgnoreKanaType then make sure locale supports it
    // JS supports kana type ignore if and only if ja, but we will only enforce this
    // if the options === CompareOptions.IgnoreKanaType to avoid erroring out too often.
    const ignoreKanaTypeKey = compareOptions & 0x8;
    if (compareOptions === ignoreKanaTypeKey) {
        // IgnoreKanaType works only for "ja"
        if (locale && locale.split("-")[0] !== "ja") {
            return COMPARISON_ERROR;
        }

        return string1.localeCompare(string2, locale);
    }

    // IgnoreWidth is not supported
    const ignoreWidth = (compareOptions & 0x10) != 0;
    if (ignoreWidth) {
        return COMPARISON_ERROR;
    }

    let options: Intl.CollatorOptions | undefined;

    const ignoreCaseFlag = 0x1;
    const ignoreNonSpaceFlag = 0x2;
    switch (compareOptions & (ignoreCaseFlag | ignoreNonSpaceFlag)) {
        case (ignoreCaseFlag | ignoreNonSpaceFlag):
            options = { sensitivity: "base" }; // a ≠ b, a = á, a = A
            break;
        case ignoreCaseFlag:
            options = { sensitivity: "accent" }; // a ≠ b, a ≠ á, a = A
            break;
        case ignoreNonSpaceFlag:
            options = { sensitivity: "case" }; // a ≠ b, a = á, a ≠ A
            break;
        // Default is options = { sensitivity: "variant" } := a ≠ b, a ≠ á, a ≠ A
    }

    const ignoreSymbols = (compareOptions & 0x4) != 0;
    if (ignoreSymbols) {
        (options ??= {}).ignorePunctuation = true;
    }

    const numericalOrdering = (compareOptions & 0x20) != 0;
    if (numericalOrdering) {
        (options ??= {}).numeric = true;
    }

    return string1.localeCompare(string2, locale, options);
}

function decodeToCleanString (strPtr: number, strLen: number) {
    const str = runtimeHelpers.utf16ToString(<any>strPtr, <any>(strPtr + 2 * strLen));
    return cleanString(str);
}

function cleanString (str: string) {
    const nStr = str.normalize();
    return nStr.replace(/[\u200B-\u200D\uFEFF\0\u00AD]/g, "");
}

// in ICU indexing only SoftHyphen is weightless
function decodeToCleanStringForIndexing (strPtr: number, strLen: number) {
    const str = runtimeHelpers.utf16ToString(<any>strPtr, <any>(strPtr + 2 * strLen));
    return str.replace(/[\u00AD]/g, "");
}
