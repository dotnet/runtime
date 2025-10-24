// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { VoidPtr } from "../types";
import { dotnetApi } from "../utils/cross-module";
import { getU16Local, setU16Local, viewOrCopy, zeroRegion } from "./memory";
import { Module } from "./cross-module";

let textDecoderUtf16: TextDecoder | undefined | null;
let stringsInitialized = false;

export function stringsInit(): void {
    if (!stringsInitialized) {
        // V8 does not provide TextDecoder
        if (typeof TextDecoder !== "undefined") {
            textDecoderUtf16 = new TextDecoder("utf-16le");
        }
        stringsInitialized = true;
    }
}

export function stringToUTF16(dstPtr: number, endPtr: number, text: string) {
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
    zeroRegion(ptr, str.length * 2);
    stringToUTF16(ptr, ptr + bytes, str);
    return ptr;
}

export function utf16ToString(startPtr: number, endPtr: number): string {
    stringsInit();
    if (textDecoderUtf16) {
        const subArray = viewOrCopy(dotnetApi.localHeapViewU8(), startPtr as any, endPtr as any);
        return textDecoderUtf16.decode(subArray);
    } else {
        return utf16ToStringLoop(startPtr, endPtr);
    }
}

// V8 does not provide TextDecoder
export function utf16ToStringLoop(startPtr: number, endPtr: number): string {
    let str = "";
    const heapU16 = dotnetApi.localHeapViewU16();
    for (let i = startPtr; i < endPtr; i += 2) {
        const char = getU16Local(heapU16, i);
        str += String.fromCharCode(char);
    }
    return str;
}
