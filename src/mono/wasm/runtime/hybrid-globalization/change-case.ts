// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../globals";
import { setU16 } from "../memory";
import { mono_wasm_new_external_root } from "../roots";
import { conv_string_root } from "../strings";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { Int32Ptr } from "../types/emscripten";
import { wrap_error_root, wrap_no_error_root } from "../invoke-js";

export function mono_wasm_change_case_invariant(src: number, srcLength: number, dst: number, dstLength: number, toUpper: number, is_exception: Int32Ptr, ex_address: MonoObjectRef) : void{
    const exceptionRoot = mono_wasm_new_external_root<MonoObject>(ex_address);
    try{
        const input = get_utf16_string(src, srcLength);
        let result = toUpper ? input.toUpperCase() : input.toLowerCase();
        // Unicode defines some codepoints which expand into multiple codepoints,
        // originally we do not support this expansion
        if (result.length > dstLength)
            result = input;

        for (let i = 0; i < result.length; i++)
            setU16(dst + i*2, result.charCodeAt(i));
        wrap_no_error_root(is_exception, exceptionRoot);
    }
    catch (ex: any) {
        wrap_error_root(is_exception, ex, exceptionRoot);
    }
    finally {
        exceptionRoot.release();
    }
}

export function mono_wasm_change_case(culture: MonoStringRef, src: number, srcLength: number, dst: number, destLength: number, toUpper: number, is_exception: Int32Ptr, ex_address: MonoObjectRef) : void{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(ex_address);
    try{
        const cultureName = conv_string_root(cultureRoot);
        if (!cultureName)
            throw new Error("Cannot change case, the culture name is null.");
        const input = get_utf16_string(src, srcLength);
        let result = toUpper ? input.toLocaleUpperCase(cultureName) : input.toLocaleLowerCase(cultureName);
        if (result.length > destLength)
            result = input;

        for (let i = 0; i < destLength; i++)
            setU16(dst + i*2, result.charCodeAt(i));
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

function get_utf16_string(ptr: number, length: number): string{
    const view = new Uint16Array(Module.HEAPU16.buffer, ptr, length);
    let string = "";
    for (let i = 0; i < length; i++)
        string += String.fromCharCode(view[i]);
    return string;
}
