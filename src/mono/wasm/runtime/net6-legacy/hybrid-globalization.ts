// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../imports";
import { mono_wasm_new_external_root } from "../roots";
import {MonoString, MonoStringRef } from "../types";
import { Int32Ptr } from "../types/emscripten";
import { conv_string_root, js_string_to_mono_string_root } from "../strings";
import { setU16 } from "../memory";

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

export function mono_wasm_compare_string(exceptionMessage: Int32Ptr, culture: MonoStringRef, str1: number, str1Length: number, str2: number, str2Length: number, options: number) : number{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture);
    try{
        const cultureName = conv_string_root(cultureRoot);
        const string1  = get_utf16_string(str1, str1Length);
        const string2 = get_utf16_string(str2, str2Length);
        const casePicker = (options & 0x1f);
        const locale = cultureName ? cultureName : undefined;
        const result = compare_strings(string1, string2, locale, casePicker);
        if (result == -2)
            throw new Error("$Invalid comparison option.");
        return result;
    }
    catch (ex: any) {
        pass_exception_details(ex, exceptionMessage);
        return -2;
    }
    finally {
        cultureRoot.release();
    }
}

export function get_utf16_string(ptr: number, length: number): string{
    const view = new Uint16Array(Module.HEAPU16.buffer, ptr, length);
    let string = "";
    for (let i = 0; i < length; i++)
        string += String.fromCharCode(view[i]);
    return string;
}

export function pass_exception_details(ex: any, exceptionMessage: Int32Ptr){
    const exceptionJsString = ex.message + "\n" + ex.stack;
    const exceptionRoot = mono_wasm_new_external_root<MonoString>(<any>exceptionMessage);
    js_string_to_mono_string_root(exceptionJsString, exceptionRoot);
    exceptionRoot.release();
}

export function compare_strings(string1: string, string2: string, locale: string | undefined, casePicker: number) : number{
    switch (casePicker)
    {
        case 0:
            // 0: None - default algorithm for the platform OR
            //    StringSort - since .Net 5 StringSort gives the same result as None, even for hyphen etc.
            //    does not work for "ja"
            if (locale && locale.split("-")[0] === "ja")
                return -2;
            return string1.localeCompare(string2, locale); // a ≠ b, a ≠ á, a ≠ A
        case 8:
            // 8: IgnoreKanaType works only for "ja"
            if (locale && locale.split("-")[0] !== "ja")
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
            return string1.localeCompare(string2, locale, { sensitivity: "accent", ignorePunctuation: true });  // a ≠ b, a ≠ á, a = A
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
            return -2;
    }
}
