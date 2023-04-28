// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../imports";
import { setU16 } from "../memory";
import { mono_wasm_new_external_root } from "../roots";
import { conv_string_root } from "../strings";
import { MonoString, MonoStringRef } from "../types";
import { Int32Ptr } from "../types/emscripten";
import { pass_exception_details } from "./common";

export function mono_wasm_change_case_invariant(exceptionMessage: Int32Ptr, src: number, srcLength: number, dst: number, dstLength: number, toUpper: number) : void{
    try{
        const input = get_utf16_string(src, srcLength);
        let result = toUpper ? input.toUpperCase() : input.toLowerCase();
        // Unicode defines some codepoints which expand into multiple codepoints,
        // originally we do not support this expansion
        if (result.length > dstLength)
            result = input;

        for (let i = 0; i < result.length; i++)
            setU16(dst + i*2, result.charCodeAt(i));
    }
    catch (ex: any) {
        pass_exception_details(ex, exceptionMessage);
    }
}

export function mono_wasm_change_case(exceptionMessage: Int32Ptr, culture: MonoStringRef, src: number, srcLength: number, dst: number, destLength: number, toUpper: number) : void{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture);
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
    }
    catch (ex: any) {
        pass_exception_details(ex, exceptionMessage);
    }
    finally {
        cultureRoot.release();
    }
}

function get_utf16_string(ptr: number, length: number): string{
    const view = new Uint16Array(Module.HEAPU16.buffer, ptr, length);
    let string = "";
    for (let i = 0; i < length; i++)
        string += String.fromCharCode(view[i]);
    return string;
}
