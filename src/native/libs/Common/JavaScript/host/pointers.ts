// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { _ems_ } from "../ems-ambient";

export function pointerSize(): 4 | 8 {
    return typeof _ems_.wasmExports?.__stack_pointer?.value === "bigint" ? 8 : 4;
}

export function readPointer(offset: number): number {
    return pointerSize() === 8
        ? Number(_ems_.HEAPU64[Math.floor(offset / 8)])
        : _ems_.HEAPU32[offset >>> 2];
}

export function writePointer(offset: number, value: number): void {
    if (pointerSize() === 8) {
        _ems_.HEAPU64[Math.floor(offset / 8)] = BigInt(value);
    } else {
        _ems_.HEAPU32[offset >>> 2] = value;
    }
}
