// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { setU16 } from "../memory";
import { mono_wasm_new_external_root } from "../roots";
import { conv_string_root } from "../strings";
import { MonoString, MonoStringRef } from "../types/internal";
import { Int32Ptr } from "../types/emscripten";
import { pass_exception_details } from "./common";

const NORMALIZATION_FORM_MAP = [undefined, "NFC", "NFD", undefined, undefined, "NFKC", "NFKD"];

export function mono_wasm_is_normalized(exceptionMessage: Int32Ptr, normalizationForm: number, inputStr: MonoStringRef) : number{
    const inputRoot = mono_wasm_new_external_root<MonoString>(inputStr);
    try{
        const jsString = conv_string_root(inputRoot);
        if (!jsString)
            throw new Error("Invalid string was received.");

        const normalization = normalization_to_string(normalizationForm);
        const result = jsString.normalize(normalization);
        return result === jsString ? 1 : 0;
    }
    catch (ex) {
        pass_exception_details(ex, exceptionMessage);
        return -1;
    } finally {
        inputRoot.release();
    }
}

export function mono_wasm_normalize_string(exceptionMessage: Int32Ptr, normalizationForm: number, inputStr: MonoStringRef, dstPtr: number, dstLength: number) : number{
    const inputRoot = mono_wasm_new_external_root<MonoString>(inputStr);
    try {
        const jsString = conv_string_root(inputRoot);
        if (!jsString)
            throw new Error("Invalid string was received.");

        const normalization = normalization_to_string(normalizationForm);
        const result = jsString.normalize(normalization);

        // increase the dest buffer
        if (result.length > dstLength)
            return result.length;
        for (let i = 0; i < result.length; i++)
            setU16(dstPtr + i*2, result.charCodeAt(i));
        return result.length;
    } catch (ex) {
        pass_exception_details(ex, exceptionMessage);
        return -1;
    } finally {
        inputRoot.release();
    }
}

const normalization_to_string = (normalizationForm: number): string => NORMALIZATION_FORM_MAP[normalizationForm] ?? "NFC";

