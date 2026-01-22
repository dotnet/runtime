// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VoidPtr } from "../types";
import { dotnetApi } from "../utils/cross-module";
import { getU16Local, setU16Local, viewOrCopy, zeroRegion } from "./memory";
import { _ems_ } from "../../Common/JavaScript/ems-ambient";

let textDecoderUtf16: TextDecoder | undefined = undefined;
let textEncoderUtf8: TextEncoder | undefined = undefined;
let stringsInitialized = false;

export function stringsInit(): void {
    if (!stringsInitialized) {
        // V8 does not provide TextDecoder
        if (typeof TextDecoder !== "undefined") {
            textDecoderUtf16 = new TextDecoder("utf-16le");
        }
        if (typeof TextEncoder !== "undefined") {
            textEncoderUtf8 = new TextEncoder();
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
    const ptr = _ems_._malloc(bytes) as any;
    zeroRegion(ptr, str.length * 2);
    stringToUTF16(ptr, ptr + bytes, str);
    return ptr;
}

export function stringToUTF8Ptr(str: string): CharPtr {
    const size = _ems_.lengthBytesUTF8(str) + 1;
    const ptr = _ems_._malloc(size) as any;
    _ems_.stringToUTF8Array(str, _ems_.HEAPU8, ptr, size);
    return ptr;
}

export function stringToUTF8(str: string): Uint8Array {
    if (textEncoderUtf8 === undefined) {
        const len = _ems_.lengthBytesUTF8(str);
        const buffer = new Uint8Array(len);
        _ems_.stringToUTF8Array(str, buffer, 0, len);
        return buffer;
    }
    return textEncoderUtf8.encode(str);
}

export function utf16ToString(startPtr: number, endPtr: number): string {
    startPtr = startPtr >>> 0;
    endPtr = endPtr >>> 0;
    stringsInit();
    if (textDecoderUtf16) {
        const subArray = viewOrCopy(dotnetApi.localHeapViewU8(), startPtr as any, endPtr as any);
        // TODO-WASM: When threading is enabled, TextDecoder does not accept a view of a
        // SharedArrayBuffer, we must make a copy of the array first.
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
