// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import { ENVIRONMENT_IS_PTHREAD } from "./globals";
import cwraps from "./cwraps";

export let gc_locked = false;

// TODO https://github.com/dotnet/runtime/issues/100411
// after Blazor stops using mono_wasm_gc_lock, mono_wasm_gc_unlock

export function monoWasmGcLock (): void {
    if (gc_locked) {
        throw new Error("GC is already locked");
    }
    if (WasmEnableThreads) {
        if (ENVIRONMENT_IS_PTHREAD) {
            throw new Error("GC lock only supported in main thread");
        }
        cwraps.mono_wasm_gc_lock();
    }
    gc_locked = true;
}

export function monoWasmGcUnlock (): void {
    if (!gc_locked) {
        throw new Error("GC is not locked");
    }
    if (WasmEnableThreads) {
        if (ENVIRONMENT_IS_PTHREAD) {
            throw new Error("GC lock only supported in main thread");
        }
        cwraps.mono_wasm_gc_unlock();
    }
    gc_locked = false;
}
