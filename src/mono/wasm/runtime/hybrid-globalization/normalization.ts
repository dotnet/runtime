// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_external_root } from "../roots";
import { monoStringToString, stringToUTF16 } from "../strings";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { Int32Ptr } from "../types/emscripten";
import { wrap_error_root, wrap_no_error_root } from "../invoke-js";

const NORMALIZATION_FORM_MAP = [undefined, "NFC", "NFD", undefined, undefined, "NFKC", "NFKD"];
const ERROR = -1;

export function mono_wasm_is_normalized(normalizationForm: number, inputStr: MonoStringRef, is_exception: Int32Ptr, ex_address: MonoObjectRef): number {
    const inputRoot = mono_wasm_new_external_root<MonoString>(inputStr),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(ex_address);
    try {
        const jsString = monoStringToString(inputRoot);
        if (!jsString)
            throw new Error("Invalid string was received.");

        const normalization = normalization_to_string(normalizationForm);
        const result = jsString.normalize(normalization);
        wrap_no_error_root(is_exception, exceptionRoot);
        return result === jsString ? 1 : 0;
    }
    catch (ex) {
        wrap_error_root(is_exception, ex, exceptionRoot);
        return ERROR;
    } finally {
        inputRoot.release();
        exceptionRoot.release();
    }
}

export function mono_wasm_normalize_string(normalizationForm: number, inputStr: MonoStringRef, dstPtr: number, dstLength: number, is_exception: Int32Ptr, ex_address: MonoObjectRef): number {
    const inputRoot = mono_wasm_new_external_root<MonoString>(inputStr),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(ex_address);
    try {
        const jsString = monoStringToString(inputRoot);
        if (!jsString)
            throw new Error("Invalid string was received.");

        const normalization = normalization_to_string(normalizationForm);
        const result = jsString.normalize(normalization);

        // increase the dest buffer
        if (result.length > dstLength)
            return result.length;
        stringToUTF16(dstPtr, dstPtr + 2 * dstLength, result);
        return result.length;
    } catch (ex) {
        wrap_error_root(is_exception, ex, exceptionRoot);
        return ERROR;
    } finally {
        inputRoot.release();
        exceptionRoot.release();
    }
}

const normalization_to_string = (normalizationForm: number): string => NORMALIZATION_FORM_MAP[normalizationForm] ?? "NFC";

