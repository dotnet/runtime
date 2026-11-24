// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VoidPtr } from "../types";
import { dotnetApi, Module } from "../utils/cross-module";
import { getU16Local, arrayBufferNeedsCopy, setU16Local, viewOrCopy, zeroRegion } from "./memory";
import { _ems_ } from "../../Common/JavaScript/ems-ambient";

let textDecoderUtf16: TextDecoder | undefined = undefined;
let textEncoderUtf8: TextEncoder | undefined = undefined;
let textDecoderUtf8Relaxed: TextDecoder | undefined = undefined;
let stringsInitialized = false;

export function stringsInit(sharedUI = false): void {
    if (!stringsInitialized) {
        // V8 does not provide TextDecoder
        if (typeof globalThis.TextDecoder !== "undefined") {
            textDecoderUtf16 = new globalThis.TextDecoder("utf-16le");
            textDecoderUtf8Relaxed = new globalThis.TextDecoder("utf-8", { fatal: false });
            if (!sharedUI && !_ems_.UTF8Decoder) {
                _ems_.UTF8Decoder = new globalThis.TextDecoder("utf-8", { fatal: false });
            }
            if (!sharedUI) {
                const decoder = _ems_.UTF8Decoder!;
                const originalUTF8DecoderDecode = decoder.decode;
                decoder.decode = function (input: Uint8Array): string {
                    const needsCopy = arrayBufferNeedsCopy(input.buffer);
                    const view = needsCopy ? input.slice() : input;
                    return originalUTF8DecoderDecode.call(this, view);
                };
            }
        }
        if (typeof globalThis.TextEncoder !== "undefined") {
            textEncoderUtf8 = new globalThis.TextEncoder();
        }
        stringsInitialized = true;
    }
}

export function stringToUTF16(dstPtr: number, endPtr: number, text: string) {
    dstPtr = dstPtr >>> 0;
    endPtr = endPtr >>> 0;
    const heapI16 = dotnetApi.localHeapViewU16();
    const len = text.length;
    for (let i = 0; i < len; i++) {
        setU16Local(heapI16, dstPtr, text.charCodeAt(i));
        dstPtr += 2;
        if (dstPtr >= endPtr) break;
    }
}

export function stringToUTF16Ptr(str: string): VoidPtr {
    const bytes = (str.length + 1) * 2;
    const ptr = Module._malloc(bytes) as any;
    zeroRegion(ptr, bytes);
    stringToUTF16(ptr, ptr + bytes, str);
    return ptr;
}

export function stringToUTF8Ptr(str: string): CharPtr {
    const encoded = stringToUTF8(str);
    const ptr = Module._malloc(encoded.length + 1) as any;
    const heap = dotnetApi.localHeapViewU8();
    heap.set(encoded, ptr);
    heap[ptr + encoded.length] = 0;
    return ptr;
}

export function stringToUTF8(str: string): Uint8Array {
    if (textEncoderUtf8 === undefined) {
        const len = _ems_.lengthBytesUTF8(str);
        const buffer = new Uint8Array(len + 1);
        _ems_.stringToUTF8Array(str, buffer, 0, buffer.length);
        return buffer.subarray(0, len);
    }
    return textEncoderUtf8.encode(str);
}

export function utf16ToString(startPtr: number, endPtr: number): string {
    startPtr = startPtr >>> 0;
    endPtr = endPtr >>> 0;
    if (textDecoderUtf16) {
        const subArray = viewOrCopy(dotnetApi.localHeapViewU8(), startPtr as any, endPtr as any);
        // SharedArrayBuffer or resizable, we must make a copy of the array first.
        // See https://github.com/whatwg/encoding/issues/172
        return textDecoderUtf16.decode(subArray);
    } else {
        return utf16ToStringLoop(startPtr, endPtr);
    }
}

// V8 does not provide TextDecoder
export function utf16ToStringLoop(startPtr: number, endPtr: number): string {
    startPtr = startPtr >>> 0;
    endPtr = endPtr >>> 0;
    let str = "";
    const heapU16 = dotnetApi.localHeapViewU16();
    for (let i = startPtr; i < endPtr; i += 2) {
        const char = getU16Local(heapU16, i);
        str += String.fromCharCode(char);
    }
    return str;
}

export function utf8ToStringRelaxed(buffer: Uint8Array): string {
    // SharedArrayBuffer, we must make a copy of the array first.
    // See https://github.com/whatwg/encoding/issues/172
    const subArray = viewOrCopy(buffer, 0 as any, buffer.byteLength as any);

    if (!textDecoderUtf8Relaxed) {
        return _ems_.UTF8ArrayToString(subArray, 0, subArray.byteLength);
    }

    return textDecoderUtf8Relaxed.decode(subArray);
}
