// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_root_buffer } from "./roots";
import { MonoString, MonoStringNull, is_nullish, WasmRoot, WasmRootBuffer } from "./types";
import { Module } from "./globals";
import cwraps from "./cwraps";
import { mono_wasm_new_root } from "./roots";
import { getI32, getU32 } from "./memory";
import { NativePointer, CharPtr } from "./types/emscripten";

export class StringDecoder {

    private mono_wasm_string_root: any;
    private mono_text_decoder: TextDecoder | undefined | null;
    private mono_wasm_string_decoder_buffer: NativePointer | undefined;

    init_fields(): void {
        if (!this.mono_wasm_string_decoder_buffer) {
            this.mono_text_decoder = typeof TextDecoder !== "undefined" ? new TextDecoder("utf-16le") : null;
            this.mono_wasm_string_root = mono_wasm_new_root();
            this.mono_wasm_string_decoder_buffer = Module._malloc(12);
        }
    }

    /**
     * @deprecated Not GC or thread safe
     */
    copy(mono_string: MonoString): string | null {
        this.init_fields();
        if (mono_string === MonoStringNull)
            return null;

        this.mono_wasm_string_root.value = mono_string;
        const result = this.copy_root(this.mono_wasm_string_root);
        this.mono_wasm_string_root.value = MonoStringNull;
        return result;
    }

    copy_root(root: WasmRoot<MonoString>): string | null {
        this.init_fields();
        if (root.value === MonoStringNull)
            return null;

        const ppChars = <any>this.mono_wasm_string_decoder_buffer + 0,
            pLengthBytes = <any>this.mono_wasm_string_decoder_buffer + 4,
            pIsInterned = <any>this.mono_wasm_string_decoder_buffer + 8;

        cwraps.mono_wasm_string_get_data_ref(root.address, <any>ppChars, <any>pLengthBytes, <any>pIsInterned);

        let result = undefined;
        const lengthBytes = getI32(pLengthBytes),
            pChars = getU32(ppChars),
            isInterned = getI32(pIsInterned);

        if (isInterned)
            result = interned_string_table.get(root.value)!;

        if (result === undefined) {
            if (lengthBytes && pChars) {
                result = this.decode(<any>pChars, <any>pChars + lengthBytes);
                if (isInterned)
                    interned_string_table.set(root.value, result);
            } else
                result = mono_wasm_empty_string;
        }

        if (result === undefined)
            throw new Error(`internal error when decoding string at location ${root.value}`);

        return result;
    }

    decode(start: CharPtr, end: CharPtr): string {
        let str = "";
        if (this.mono_text_decoder) {
            // When threading is enabled, TextDecoder does not accept a view of a
            // SharedArrayBuffer, we must make a copy of the array first.
            // See https://github.com/whatwg/encoding/issues/172
            const subArray = typeof SharedArrayBuffer !== "undefined" && Module.HEAPU8.buffer instanceof SharedArrayBuffer
                ? Module.HEAPU8.slice(<any>start, <any>end)
                : Module.HEAPU8.subarray(<any>start, <any>end);

            str = this.mono_text_decoder.decode(subArray);
        } else {
            for (let i = 0; i < <any>end - <any>start; i += 2) {
                const char = Module.getValue(<any>start + i, "i16");
                str += String.fromCharCode(char);
            }
        }

        return str;
    }
}

const interned_string_table = new Map<MonoString, string>();
export const interned_js_string_table = new Map<string, MonoString>();
let _empty_string_ptr: MonoString = <any>0;
const _interned_string_full_root_buffers = [];
let _interned_string_current_root_buffer: WasmRootBuffer | null = null;
let _interned_string_current_root_buffer_count = 0;
export const string_decoder = new StringDecoder();
export const mono_wasm_empty_string = "";

/**
 * @deprecated Not GC or thread safe
 */
export function conv_string(mono_obj: MonoString): string | null {
    return string_decoder.copy(mono_obj);
}

export function conv_string_root(root: WasmRoot<MonoString>): string | null {
    return string_decoder.copy_root(root);
}

// Ensures the string is already interned on both the managed and JavaScript sides,
//  then returns the interned string value (to provide fast reference comparisons like C#)
export function mono_intern_string(string: string): string {
    if (string.length === 0)
        return mono_wasm_empty_string;

    // HACK: This would normally be unsafe, but the return value of js_string_to_mono_string_interned is always an
    //  interned string, so the address will never change and it is safe for us to use the raw pointer. Don't do this though
    const ptr = js_string_to_mono_string_interned(string);
    const result = interned_string_table.get(ptr);
    if (is_nullish(result))
        throw new Error("internal error: interned_string_table did not contain string after js_string_to_mono_string_interned");
    return result;
}

function _store_string_in_intern_table(string: string, root: WasmRoot<MonoString>, internIt: boolean): void {
    if (!root.value)
        throw new Error("null pointer passed to _store_string_in_intern_table");

    const internBufferSize = 8192;

    if (_interned_string_current_root_buffer_count >= internBufferSize) {
        _interned_string_full_root_buffers.push(_interned_string_current_root_buffer);
        _interned_string_current_root_buffer = null;
    }
    if (!_interned_string_current_root_buffer) {
        _interned_string_current_root_buffer = mono_wasm_new_root_buffer(internBufferSize, "interned strings");
        _interned_string_current_root_buffer_count = 0;
    }

    const rootBuffer = _interned_string_current_root_buffer;
    const index = _interned_string_current_root_buffer_count++;

    // Store the managed string into the managed intern table. This can theoretically
    //  provide a different managed object than the one we passed in, so update our
    //  pointer (stored in the root) with the result.
    if (internIt) {
        cwraps.mono_wasm_intern_string_ref(root.address);
        if (!root.value)
            throw new Error("mono_wasm_intern_string_ref produced a null pointer");
    }

    interned_js_string_table.set(string, root.value);
    interned_string_table.set(root.value, string);

    if ((string.length === 0) && !_empty_string_ptr)
        _empty_string_ptr = root.value;

    // Copy the final pointer into our interned string root buffer to ensure the string
    //  remains rooted. TODO: Is this actually necessary?
    rootBuffer.copy_value_from_address(index, root.address);
}

export function js_string_to_mono_string_interned_root(string: string | symbol, result: WasmRoot<MonoString>): void {
    let text: string | undefined;
    if (typeof (string) === "symbol") {
        text = string.description;
        if (typeof (text) !== "string")
            text = Symbol.keyFor(string);
        if (typeof (text) !== "string")
            text = "<unknown Symbol>";
    } else if (typeof (string) === "string") {
        text = string;
    }

    if (typeof (text) !== "string") {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        throw new Error(`Argument to js_string_to_mono_string_interned must be a string but was ${string}`);
    }

    if ((text.length === 0) && _empty_string_ptr) {
        result.set(_empty_string_ptr);
        return;
    }

    const ptr = interned_js_string_table.get(text);
    if (ptr) {
        result.set(ptr);
        return;
    }

    js_string_to_mono_string_new_root(text, result);
    _store_string_in_intern_table(text, result, true);
}

export function js_string_to_mono_string_root(string: string, result: WasmRoot<MonoString>): void {
    result.clear();

    if (string === null)
        return;
    else if (typeof (string) === "symbol")
        js_string_to_mono_string_interned_root(string, result);
    else if (typeof (string) !== "string")
        throw new Error("Expected string argument, got " + typeof (string));
    else if (string.length === 0)
        // Always use an interned pointer for empty strings
        js_string_to_mono_string_interned_root(string, result);
    else {
        // Looking up large strings in the intern table will require the JS runtime to
        //  potentially hash them and then do full byte-by-byte comparisons, which is
        //  very expensive. Because we can not guarantee it won't happen, try to minimize
        //  the cost of this and prevent performance issues for large strings
        if (string.length <= 256) {
            const interned = interned_js_string_table.get(string);
            if (interned) {
                result.set(interned);
                return;
            }
        }

        js_string_to_mono_string_new_root(string, result);
    }
}

export function js_string_to_mono_string_new_root(string: string, result: WasmRoot<MonoString>): void {
    const buffer = Module._malloc((string.length + 1) * 2);
    const buffer16 = (<any>buffer >>> 1) | 0;
    for (let i = 0; i < string.length; i++)
        Module.HEAP16[buffer16 + i] = string.charCodeAt(i);
    Module.HEAP16[buffer16 + string.length] = 0;
    cwraps.mono_wasm_string_from_utf16_ref(<any>buffer, string.length, result.address);
    Module._free(buffer);
}

/**
 * @deprecated Not GC or thread safe
 */
export function js_string_to_mono_string_interned(string: string | symbol): MonoString {
    const temp = mono_wasm_new_root<MonoString>();
    try {
        js_string_to_mono_string_interned_root(string, temp);
        return temp.value;
    } finally {
        temp.release();
    }
}

/**
 * @deprecated Not GC or thread safe
 */
export function js_string_to_mono_string(string: string): MonoString {
    const temp = mono_wasm_new_root<MonoString>();
    try {
        js_string_to_mono_string_root(string, temp);
        return temp.value;
    } finally {
        temp.release();
    }
}

/**
 * @deprecated Not GC or thread safe
 */
export function js_string_to_mono_string_new(string: string): MonoString {
    const temp = mono_wasm_new_root<MonoString>();
    try {
        js_string_to_mono_string_new_root(string, temp);
        return temp.value;
    } finally {
        temp.release();
    }
}
