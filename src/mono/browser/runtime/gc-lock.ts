// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import { ENVIRONMENT_IS_PTHREAD } from "./globals";
import cwraps from "./cwraps";

let locked = false;

export function mono_wasm_gc_lock(): void {
    if (locked) {
        throw new Error("GC is already locked");
    }
    if (WasmEnableThreads) {
        if (ENVIRONMENT_IS_PTHREAD) {
            throw new Error("GC lock only supported in main thread");
        }
        cwraps.mono_wasm_gc_lock();
    }
    locked = true;
}

export function mono_wasm_gc_unlock(): void {
    if (!locked) {
        throw new Error("GC is not locked");
    }
    if (WasmEnableThreads) {
        if (ENVIRONMENT_IS_PTHREAD) {
            throw new Error("GC lock only supported in main thread");
        }
        cwraps.mono_wasm_gc_unlock();
    }
    locked = false;
}
