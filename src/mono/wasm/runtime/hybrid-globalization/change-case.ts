// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_external_root } from "../roots";
import { monoStringToString, utf16ToStringLoop, stringToUTF16 } from "../strings";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { Int32Ptr } from "../types/emscripten";
import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { localHeapViewU16, setU16_local } from "../memory";

export function mono_wasm_change_case_invariant(src: number, srcLength: number, dst: number, dstLength: number, toUpper: number, is_exception: Int32Ptr, ex_address: MonoObjectRef): void {
    const exceptionRoot = mono_wasm_new_external_root<MonoObject>(ex_address);
    try {
        const input = utf16ToStringLoop(src, src + 2 * srcLength);
        const result = toUpper ? input.toUpperCase() : input.toLowerCase();
        // Unicode defines some codepoints which expand into multiple codepoints,
        // originally we do not support this expansion
        if (result.length <= dstLength)
        {
            stringToUTF16(dst, dst + 2 * dstLength, result);
            wrap_no_error_root(is_exception, exceptionRoot);
            return;
        }

        // workaround to maintain the ICU-like behavior
        const heapI16 = localHeapViewU16();
        if (toUpper)
        {
            for (let i=0; i < input.length; i++)
            {
                const upperChar = input[i].toUpperCase();
                const appendedChar = upperChar.length > 1 ? input[i] : upperChar;
                setU16_local(heapI16, dst + i*2, appendedChar.charCodeAt(0));
            }
        }
        else
        {
            for (let i=0; i < input.length; i++)
            {
                const lowerChar = input[i].toLowerCase();
                const appendedChar = lowerChar.length > 1 ? input[i] : lowerChar;
                setU16_local(heapI16, dst + i*2, appendedChar.charCodeAt(0));
            }
        }
    }
    catch (ex: any) {
        wrap_error_root(is_exception, ex, exceptionRoot);
    }
    finally {
        exceptionRoot.release();
    }
}

export function mono_wasm_change_case(culture: MonoStringRef, src: number, srcLength: number, dst: number, dstLength: number, toUpper: number, is_exception: Int32Ptr, ex_address: MonoObjectRef): void {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(ex_address);
    try {
        const cultureName = monoStringToString(cultureRoot);
        if (!cultureName)
            throw new Error("Cannot change case, the culture name is null.");
        const input = utf16ToStringLoop(src, src + 2 * srcLength);
        const result = toUpper ? input.toLocaleUpperCase(cultureName) : input.toLocaleLowerCase(cultureName);

        if (result.length <= input.length)
        {
            stringToUTF16(dst, dst + 2 * dstLength, result);
            wrap_no_error_root(is_exception, exceptionRoot);
            return;
        }
        // workaround to maintain the ICU-like behavior
        const heapI16 = localHeapViewU16();
        if (toUpper)
        {
            for (let i=0; i < input.length; i++)
            {
                const upperChar = input[i].toLocaleUpperCase(cultureName);
                const appendedChar = upperChar.length > 1 ? input[i] : upperChar;
                setU16_local(heapI16, dst + i*2, appendedChar.charCodeAt(0));
            }
        }
        else
        {
            for (let i=0; i < input.length; i++)
            {
                const lowerChar = input[i].toLocaleLowerCase(cultureName);
                const appendedChar = lowerChar.length > 1 ? input[i] : lowerChar;
                setU16_local(heapI16, dst + i*2, appendedChar.charCodeAt(0));
            }
        }
        wrap_no_error_root(is_exception, exceptionRoot);
    }
    catch (ex: any) {
        wrap_error_root(is_exception, ex, exceptionRoot);
    }
    finally {
        cultureRoot.release();
        exceptionRoot.release();
    }
}
