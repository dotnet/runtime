// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Wasm64 from "consts:wasm64";
import { _ems_ } from "../ems-ambient";

export function readPointer(offset: number): number {
    return Wasm64
        ? Number(_ems_.HEAPU64[Math.floor(offset / 8)])
        : _ems_.HEAPU32[offset >>> 2];
}

export function writePointer(offset: number, value: number): void {
    if (Wasm64) {
        _ems_.HEAPU64[Math.floor(offset / 8)] = BigInt(value);
    } else {
        _ems_.HEAPU32[offset >>> 2] = value;
    }
}
