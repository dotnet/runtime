// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { normalize_exception } from "../invoke-js";
import { receiveWorkerHeapViews, setI32_unchecked } from "../memory";
import { stringToMonoStringRoot } from "../strings";
import { Int32Ptr } from "../types/emscripten";
import { MonoObject, WasmRoot } from "../types/internal";

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

export function normalizeSpaces (pattern: string) {
    if (!pattern.includes("\u202F"))
        return pattern;

    // if U+202F present, replace them with spaces
    return pattern.replace("\u202F", "\u0020");
}


export function isSurrogate (str: string, startIdx: number): boolean {
    return SURROGATE_HIGHER_START <= str[startIdx] &&
        str[startIdx] <= SURROGATE_HIGHER_END &&
        startIdx + 1 < str.length &&
        SURROGATE_LOWER_START <= str[startIdx + 1] &&
        str[startIdx + 1] <= SURROGATE_LOWER_END;
}

function _wrap_error_flag (is_exception: Int32Ptr | null, ex: any): string {
    const res = normalize_exception(ex);
    if (is_exception) {
        receiveWorkerHeapViews();
        setI32_unchecked(is_exception, 1);
    }
    return res;
}

export function wrap_error_root (is_exception: Int32Ptr | null, ex: any, result: WasmRoot<MonoObject>): void {
    const res = _wrap_error_flag(is_exception, ex);
    stringToMonoStringRoot(res, <any>result);
}

// TODO replace it with replace it with UTF16 char*, no GC root needed
// https://github.com/dotnet/runtime/issues/98365
export function wrap_no_error_root (is_exception: Int32Ptr | null, result?: WasmRoot<MonoObject>): void {
    if (is_exception) {
        receiveWorkerHeapViews();
        setI32_unchecked(is_exception, 0);
    }
    if (result) {
        result.clear();
    }
}
