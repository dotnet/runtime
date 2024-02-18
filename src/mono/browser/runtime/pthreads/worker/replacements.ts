// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import { PThreadLibrary, PThreadPtr } from "../../types/internal";
import { mono_wasm_pthread_on_pthread_created, onRunMessage as on_emscripten_thread_init } from ".";
import { Module } from "../../globals";

export function replaceEmscriptenPThreadWorker(modulePThread: PThreadLibrary): void {
    if (!WasmEnableThreads) return;

    const originalThreadInitTLS = modulePThread.threadInitTLS;
    const original_emscripten_thread_init = (Module as any)["__emscripten_thread_init"];

    (Module as any)["__emscripten_thread_init"] = (pthread_ptr: PThreadPtr, isMainBrowserThread: number, isMainRuntimeThread: number, canBlock: number) => {
        on_emscripten_thread_init(pthread_ptr);
        original_emscripten_thread_init(pthread_ptr, isMainBrowserThread, isMainRuntimeThread, canBlock);
    };
    modulePThread.threadInitTLS = (): void => {
        originalThreadInitTLS();
        mono_wasm_pthread_on_pthread_created();
    };
}