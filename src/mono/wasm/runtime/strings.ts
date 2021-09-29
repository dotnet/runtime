// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_root_buffer, WasmRootBuffer } from './roots';
import { MonoString } from './types';
import { Module } from './modules'
import cwraps from './cwraps'
import { mono_wasm_new_root } from './roots';

export class StringDecoder {

    private mono_wasm_string_root: any;
    private mono_text_decoder: TextDecoder | undefined | null;
    private mono_wasm_string_decoder_buffer: NativePointer | undefined;
    private interned_string_table: Map<number, string> | undefined;

    copy(mono_string: MonoString) {
        if (!this.interned_string_table || !this.mono_wasm_string_decoder_buffer) {
            this.mono_text_decoder = typeof TextDecoder !== 'undefined' ? new TextDecoder('utf-16le') : null;
            this.mono_wasm_string_root = mono_wasm_new_root();
            this.mono_wasm_string_decoder_buffer = Module._malloc(12);
            this.interned_string_table = new Map();
        }
        if (mono_string === 0)
            return null;

        this.mono_wasm_string_root.value = mono_string;

        let ppChars = <number>this.mono_wasm_string_decoder_buffer + 0,
            pLengthBytes = <number>this.mono_wasm_string_decoder_buffer + 4,
            pIsInterned = <number>this.mono_wasm_string_decoder_buffer + 8;

        cwraps.mono_wasm_string_get_data(mono_string, ppChars, pLengthBytes, pIsInterned);

        let result = mono_wasm_empty_string;
        let lengthBytes = Module.HEAP32[pLengthBytes / 4],
            pChars = Module.HEAP32[ppChars / 4],
            isInterned = Module.HEAP32[pIsInterned / 4];

        if (pLengthBytes && pChars) {
            if (
                isInterned &&
                this.interned_string_table &&
                this.interned_string_table.has(<number>mono_string) //TODO remove 2x lookup
            ) {
                result = this.interned_string_table.get(<number>mono_string)!;
                // console.log("intern table cache hit", mono_string, result.length);
            } else {
                result = this.decode(pChars, pChars + lengthBytes);
                if (isInterned) {
                    // console.log("interned", mono_string, result.length);
                    this.interned_string_table.set(<number>mono_string, result);
                }
            }
        }

        this.mono_wasm_string_root.value = 0;
        return result;
    }

    private decode(start: number, end: number) {
        var str = "";
        if (this.mono_text_decoder) {
            // When threading is enabled, TextDecoder does not accept a view of a
            // SharedArrayBuffer, we must make a copy of the array first.
            // See https://github.com/whatwg/encoding/issues/172
            var subArray = typeof SharedArrayBuffer !== 'undefined' && Module.HEAPU8.buffer instanceof SharedArrayBuffer
                ? Module.HEAPU8.slice(start, end)
                : Module.HEAPU8.subarray(start, end);

            str = this.mono_text_decoder.decode(subArray);
        } else {
            for (var i = 0; i < end - start; i += 2) {
                var char = Module.getValue(start + i, 'i16');
                str += String.fromCharCode(char);
            }
        }

        return str;
    }
}

const interned_string_table = new Map();
const interned_js_string_table = new Map();
let _empty_string_ptr: CharPtr = 0;
const _interned_string_full_root_buffers = [];
let _interned_string_current_root_buffer: WasmRootBuffer | null = null;
let _interned_string_current_root_buffer_count = 0;
export let string_decoder = new StringDecoder();
export const mono_wasm_empty_string = "";

export function conv_string(mono_obj: CharPtr) {
    return string_decoder.copy(mono_obj);
}

// Ensures the string is already interned on both the managed and JavaScript sides,
//  then returns the interned string value (to provide fast reference comparisons like C#)
export function mono_intern_string(string: string) {
    if (string.length === 0)
        return mono_wasm_empty_string;

    var ptr = js_string_to_mono_string_interned(string);
    var result = interned_string_table.get(ptr);
    return result;
}

function _store_string_in_intern_table(string: string, ptr: CharPtr, internIt: boolean) {
    if (!ptr)
        throw new Error("null pointer passed to _store_string_in_intern_table");
    else if (typeof (ptr) !== "number")
        throw new Error(`non-pointer passed to _store_string_in_intern_table: ${typeof (ptr)}`);

    const internBufferSize = 8192;

    if (_interned_string_current_root_buffer_count >= internBufferSize) {
        _interned_string_full_root_buffers.push(_interned_string_current_root_buffer);
        _interned_string_current_root_buffer = null;
    }
    if (!_interned_string_current_root_buffer) {
        _interned_string_current_root_buffer = mono_wasm_new_root_buffer(internBufferSize, "interned strings");
        _interned_string_current_root_buffer_count = 0;
    }

    var rootBuffer = _interned_string_current_root_buffer;
    var index = _interned_string_current_root_buffer_count++;
    rootBuffer.set(index, ptr);

    // Store the managed string into the managed intern table. This can theoretically
    //  provide a different managed object than the one we passed in, so update our
    //  pointer (stored in the root) with the result.
    if (internIt)
        rootBuffer.set(index, ptr = cwraps.mono_wasm_intern_string(ptr));

    if (!ptr)
        throw new Error("mono_wasm_intern_string produced a null pointer");

    interned_js_string_table.set(string, ptr);
    interned_string_table.set(ptr, string);

    if ((string.length === 0) && !_empty_string_ptr)
        _empty_string_ptr = ptr;

    return ptr;
}

export function js_string_to_mono_string_interned(string: string | symbol) {
    var text = (typeof (string) === "symbol")
        ? (string.description || Symbol.keyFor(string) || "<unknown Symbol>")
        : string;

    if ((text.length === 0) && _empty_string_ptr)
        return _empty_string_ptr;

    var ptr = interned_js_string_table.get(text);
    if (ptr)
        return ptr;

    ptr = js_string_to_mono_string_new(text);
    ptr = _store_string_in_intern_table(text, ptr, true);

    return ptr;
}

export function js_string_to_mono_string(string: string) {
    if (string === null)
        return null;
    else if (typeof (string) === "symbol")
        return js_string_to_mono_string_interned(string);
    else if (typeof (string) !== "string")
        throw new Error("Expected string argument, got " + typeof (string));

    // Always use an interned pointer for empty strings
    if (string.length === 0)
        return js_string_to_mono_string_interned(string);

    // Looking up large strings in the intern table will require the JS runtime to
    //  potentially hash them and then do full byte-by-byte comparisons, which is
    //  very expensive. Because we can not guarantee it won't happen, try to minimize
    //  the cost of this and prevent performance issues for large strings
    if (string.length <= 256) {
        var interned = interned_js_string_table.get(string);
        if (interned)
            return interned;
    }

    return js_string_to_mono_string_new(string);
}

function js_string_to_mono_string_new(string: string) {
    var buffer = Module._malloc((string.length + 1) * 2);
    var buffer16 = (<number>buffer / 2) | 0;
    for (var i = 0; i < string.length; i++)
        Module.HEAP16[buffer16 + i] = string.charCodeAt(i);
    Module.HEAP16[buffer16 + string.length] = 0;
    var result = cwraps.mono_wasm_string_from_utf16(buffer, string.length);
    Module._free(buffer);
    return result;
}