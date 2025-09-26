// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { CharPtr, MemOffset, VoidPtr } from "./types";
import { dotnetBrowserHostExports, dotnetApi } from "./cross-module";

// does not check for growable heap
export function getU16Local(localView: Uint16Array, offset: MemOffset): number {
    return localView[<any>offset >>> 1];
}

// does not check for growable heap
export function setU16Local(localView: Uint16Array, offset: MemOffset, value: number): void {
    assertIntInRange(value, 0, 0xFFFF);
    localView[<any>offset >>> 1] = value;
}

// When threading is enabled, TextDecoder does not accept a view of a
// SharedArrayBuffer, we must make a copy of the array first.
// See https://github.com/whatwg/encoding/issues/172
export function viewOrCopy(view: Uint8Array, start: CharPtr, end: CharPtr): Uint8Array {
    // this condition should be eliminated by rollup on non-threading builds
    const needsCopy = dotnetBrowserHostExports.isSharedArrayBuffer(view.buffer);
    return needsCopy
        ? view.slice(<any>start, <any>end)
        : view.subarray(<any>start, <any>end);
}

export function assertIntInRange(value: Number, min: Number, max: Number) {
    dotnetAssert.check(Number.isSafeInteger(value), () => `Value is not an integer: ${value} (${typeof (value)})`);
    dotnetAssert.check(value >= min && value <= max, () => `Overflow: value ${value} is out of ${min} ${max} range`);
}

export function ZeroRegion(byteOffset: VoidPtr, sizeBytes: number): void {
    dotnetApi.localHeapViewU8().fill(0, <any>byteOffset, <any>byteOffset + sizeBytes);
}

