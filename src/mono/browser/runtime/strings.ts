// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_root, mono_wasm_new_root_buffer } from "./roots";
import { MonoString, MonoStringNull, WasmRoot, WasmRootBuffer } from "./types/internal";
import { Module } from "./globals";
import cwraps from "./cwraps";
import { isSharedArrayBuffer, localHeapViewU8, getU32_local, setU16_local, localHeapViewU32, getU16_local, localHeapViewU16 } from "./memory";
import { NativePointer, CharPtr } from "./types/emscripten";

export const interned_js_string_table = new Map<string, MonoString>();
export const mono_wasm_empty_string = "";
let mono_wasm_string_decoder_buffer: NativePointer | undefined;
export const interned_string_table = new Map<MonoString, string>();
let _empty_string_ptr: MonoString = <any>0;
const _interned_string_full_root_buffers = [];
let _interned_string_current_root_buffer: WasmRootBuffer | null = null;
let _interned_string_current_root_buffer_count = 0;
let _text_decoder_utf16: TextDecoder | undefined | null;
let _text_decoder_utf8_relaxed: TextDecoder | undefined = undefined;
let _text_decoder_utf8_validating: TextDecoder | undefined = undefined;
let _text_encoder_utf8: TextEncoder | undefined = undefined;

export function strings_init(): void {
    if (!mono_wasm_string_decoder_buffer) {
        if (typeof TextDecoder !== "undefined") {
            _text_decoder_utf16 = new TextDecoder("utf-16le");
            _text_decoder_utf8_relaxed = new TextDecoder("utf-8", { fatal: false });
            _text_decoder_utf8_validating = new TextDecoder("utf-8");
            _text_encoder_utf8 = new TextEncoder();
        }
        mono_wasm_string_decoder_buffer = Module._malloc(12);
    }
}

export function stringToUTF8(str: string): Uint8Array {
    if (_text_encoder_utf8 === undefined) {
        const buffer = new Uint8Array(str.length * 2);
        Module.stringToUTF8Array(str, buffer, 0, str.length * 2);
        return buffer;
    }
    return _text_encoder_utf8.encode(str);
}

export function utf8ToStringRelaxed(buffer: Uint8Array): string {
    if (_text_decoder_utf8_relaxed === undefined) {
        return Module.UTF8ArrayToString(buffer, 0, buffer.byteLength);
    }
    return _text_decoder_utf8_relaxed.decode(buffer);
}

export function utf8ToString(ptr: CharPtr): string {
    const heapU8 = localHeapViewU8();
    return utf8BufferToString(heapU8, ptr as any, heapU8.length - (ptr as any));
}

export function utf8BufferToString(heapOrArray: Uint8Array, idx: number, maxBytesToRead: number): string {
    const endIdx = idx + maxBytesToRead;
    let endPtr = idx;
    while (heapOrArray[endPtr] && !(endPtr >= endIdx)) ++endPtr;
    if (endPtr - idx <= 16) {
        return Module.UTF8ArrayToString(heapOrArray, idx, maxBytesToRead);
    }
    if (_text_decoder_utf8_validating === undefined) {
        return Module.UTF8ArrayToString(heapOrArray, idx, maxBytesToRead);
    }
    const view = viewOrCopy(heapOrArray, idx as any, endPtr as any);
    return _text_decoder_utf8_validating.decode(view);
}

export function utf16ToString(startPtr: number, endPtr: number): string {
    if (_text_decoder_utf16) {
        const subArray = viewOrCopy(localHeapViewU8(), startPtr as any, endPtr as any);
        return _text_decoder_utf16.decode(subArray);
    } else {
        return utf16ToStringLoop(startPtr, endPtr);
    }
}

export function utf16ToStringLoop(startPtr: number, endPtr: number): string {
    let str = "";
    const heapU16 = localHeapViewU16();
    for (let i = startPtr; i < endPtr; i += 2) {
        const char = getU16_local(heapU16, i);
        str += String.fromCharCode(char);
    }
    return str;
}

export function stringToUTF16(dstPtr: number, endPtr: number, text: string) {
    const heapI16 = localHeapViewU16();
    const len = text.length;
    for (let i = 0; i < len; i++) {
        setU16_local(heapI16, dstPtr, text.charCodeAt(i));
        dstPtr += 2;
        if (dstPtr >= endPtr) break;
    }
}

export function monoStringToString(root: WasmRoot<MonoString>): string | null {
    if (root.value === MonoStringNull)
        return null;

    const ppChars = <any>mono_wasm_string_decoder_buffer + 0,
        pLengthBytes = <any>mono_wasm_string_decoder_buffer + 4,
        pIsInterned = <any>mono_wasm_string_decoder_buffer + 8;

    cwraps.mono_wasm_string_get_data_ref(root.address, <any>ppChars, <any>pLengthBytes, <any>pIsInterned);

    let result = undefined;
    const heapU32 = localHeapViewU32();
    const lengthBytes = getU32_local(heapU32, pLengthBytes),
        pChars = getU32_local(heapU32, ppChars),
        isInterned = getU32_local(heapU32, pIsInterned);

    if (isInterned)
        result = interned_string_table.get(root.value)!;

    if (result === undefined) {
        if (lengthBytes && pChars) {
            result = utf16ToString(<any>pChars, <any>pChars + lengthBytes);
            if (isInterned)
                interned_string_table.set(root.value, result);
        } else
            result = mono_wasm_empty_string;
    }

    if (result === undefined)
        throw new Error(`internal error when decoding string at location ${root.value}`);

    return result;
}

export function stringToMonoStringRoot(string: string, result: WasmRoot<MonoString>): void {
    result.clear();

    if (string === null)
        return;
    else if (typeof (string) === "symbol")
        stringToInternedMonoStringRoot(string, result);
    else if (typeof (string) !== "string")
        throw new Error("Expected string argument, got " + typeof (string));
    else if (string.length === 0)
        // Always use an interned pointer for empty strings
        stringToInternedMonoStringRoot(string, result);
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

        stringToMonoStringNewRoot(string, result);
    }
}

export function stringToInternedMonoStringRoot(string: string | symbol, result: WasmRoot<MonoString>): void {
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
        throw new Error(`Argument to stringToInternedMonoStringRoot must be a string but was ${string}`);
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

    stringToMonoStringNewRoot(text, result);
    storeStringInInternTable(text, result, true);
}

function storeStringInInternTable(string: string, root: WasmRoot<MonoString>, internIt: boolean): void {
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

function stringToMonoStringNewRoot(string: string, result: WasmRoot<MonoString>): void {
    const bufferLen = (string.length + 1) * 2;
    const buffer = Module._malloc(bufferLen);
    stringToUTF16(buffer as any, buffer as any + bufferLen, string);
    cwraps.mono_wasm_string_from_utf16_ref(<any>buffer, string.length, result.address);
    Module._free(buffer);
}

// When threading is enabled, TextDecoder does not accept a view of a
// SharedArrayBuffer, we must make a copy of the array first.
// See https://github.com/whatwg/encoding/issues/172
export function viewOrCopy(view: Uint8Array, start: CharPtr, end: CharPtr): Uint8Array {
    // this condition should be eliminated by rollup on non-threading builds
    const needsCopy = isSharedArrayBuffer(view.buffer);
    return needsCopy
        ? view.slice(<any>start, <any>end)
        : view.subarray(<any>start, <any>end);
}

// below is minimal legacy support for Blazor
let mono_wasm_string_root: any;

/* @deprecated not GC safe, use monoStringToString */
export function monoStringToStringUnsafe(mono_string: MonoString): string | null {
    if (mono_string === MonoStringNull)
        return null;
    if (!mono_wasm_string_root)
        mono_wasm_string_root = mono_wasm_new_root();

    mono_wasm_string_root.value = mono_string;
    const result = monoStringToString(mono_wasm_string_root);
    mono_wasm_string_root.value = MonoStringNull;
    return result;
}
