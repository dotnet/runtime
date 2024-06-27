// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import type { GCHandle } from "../types/internal";

import { ENVIRONMENT_IS_PTHREAD, Module, mono_assert, runtimeHelpers } from "../globals";
import { bindings_init } from "../startup";
import { forceDisposeProxies } from "../gc-handles";
import { GCHandleNull, WorkerToMainMessageType } from "../types/internal";
import { monoThreadInfo, postMessageToMain, update_thread_info } from "./shared";

export function mono_wasm_install_js_worker_interop (context_gc_handle: GCHandle): void {
    if (!WasmEnableThreads) return;
    bindings_init();
    mono_assert(!runtimeHelpers.proxyGCHandle, "JS interop should not be already installed on this worker.");
    runtimeHelpers.proxyGCHandle = context_gc_handle;
    if (ENVIRONMENT_IS_PTHREAD) {
        runtimeHelpers.managedThreadTID = runtimeHelpers.currentThreadTID;
        runtimeHelpers.isManagedRunningOnCurrentThread = true;
    }
    Module.runtimeKeepalivePush();
    monoThreadInfo.isDirtyBecauseOfInterop = true;
    update_thread_info();
    if (ENVIRONMENT_IS_PTHREAD) {
        postMessageToMain({
            monoCmd: WorkerToMainMessageType.enabledInterop,
            info: monoThreadInfo,
        });
    }
}

export function mono_wasm_uninstall_js_worker_interop (): void {
    if (!WasmEnableThreads) return;
    mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "JS interop is not installed on this worker.");
    mono_assert(runtimeHelpers.proxyGCHandle, "JSSynchronizationContext is not installed on this worker.");

    forceDisposeProxies(true, runtimeHelpers.diagnosticTracing);
    Module.runtimeKeepalivePop();

    runtimeHelpers.proxyGCHandle = GCHandleNull;
    runtimeHelpers.mono_wasm_bindings_is_ready = false;
    update_thread_info();
}
