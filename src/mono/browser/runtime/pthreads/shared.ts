// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import type { GCHandle, MonoThreadMessage, PThreadInfo, PThreadPtr } from "../types/internal";

import { ENVIRONMENT_IS_PTHREAD, Module, loaderHelpers, mono_assert, runtimeHelpers } from "../globals";
import { set_thread_prefix } from "../logging";
import { bindings_init } from "../startup";
import { forceDisposeProxies } from "../gc-handles";
import { monoMessageSymbol, GCHandleNull, PThreadPtrNull, WorkerToMainMessageType } from "../types/internal";

// A duplicate in loader/assets.ts
export const worker_empty_prefix = "          -    ";

const monoThreadInfoPartial: Partial<PThreadInfo> = {
    pthreadId: PThreadPtrNull,
    reuseCount: 0,
    updateCount: 0,
    threadPrefix: worker_empty_prefix,
    threadName: "emscripten-loaded",
};
export const monoThreadInfo: PThreadInfo = monoThreadInfoPartial as PThreadInfo;

export function isMonoThreadMessage(x: unknown): x is MonoThreadMessage {
    if (typeof (x) !== "object" || x === null) {
        return false;
    }
    const xmsg = x as MonoThreadMessage;
    return typeof (xmsg.type) === "string" && typeof (xmsg.cmd) === "string";
}

export function mono_wasm_install_js_worker_interop(context_gc_handle: GCHandle): void {
    if (!WasmEnableThreads) return;
    bindings_init();
    mono_assert(!runtimeHelpers.proxyGCHandle, "JS interop should not be already installed on this worker.");
    runtimeHelpers.proxyGCHandle = context_gc_handle;
    if (ENVIRONMENT_IS_PTHREAD) {
        runtimeHelpers.managedThreadTID = mono_wasm_pthread_ptr();
        runtimeHelpers.isCurrentThread = true;
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

export function mono_wasm_uninstall_js_worker_interop(): void {
    if (!WasmEnableThreads) return;
    mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "JS interop is not installed on this worker.");
    mono_assert(runtimeHelpers.proxyGCHandle, "JSSynchronizationContext is not installed on this worker.");

    forceDisposeProxies(true, runtimeHelpers.diagnosticTracing);
    Module.runtimeKeepalivePop();

    runtimeHelpers.proxyGCHandle = GCHandleNull;
    runtimeHelpers.mono_wasm_bindings_is_ready = false;
    update_thread_info();
}

// this is just for Debug build of the runtime, making it easier to debug worker threads
export function update_thread_info(): void {
    if (!WasmEnableThreads) return;
    const threadType = !monoThreadInfo.isRegistered ? "emsc"
        : monoThreadInfo.isUI ? "-UI-"
            : monoThreadInfo.isTimer ? "timr"
                : monoThreadInfo.isLongRunning ? "long"
                    : monoThreadInfo.isThreadPoolGate ? "gate"
                        : monoThreadInfo.isDebugger ? "dbgr"
                            : monoThreadInfo.isThreadPoolWorker ? "pool"
                                : monoThreadInfo.isExternalEventLoop ? "jsww"
                                    : monoThreadInfo.isBackground ? "back"
                                        : "norm";
    const hexPtr = (monoThreadInfo.pthreadId as any).toString(16).padStart(8, "0");
    const hexPrefix = monoThreadInfo.isRegistered ? "0x" : "--";
    monoThreadInfo.threadPrefix = `${hexPrefix}${hexPtr}-${threadType}`;

    loaderHelpers.set_thread_prefix(monoThreadInfo.threadPrefix!);
    if (!loaderHelpers.config.forwardConsoleLogsToWS) {
        set_thread_prefix(monoThreadInfo.threadPrefix!);
    }

    (globalThis as any).monoThreadInfo = monoThreadInfo;
    if (WasmEnableThreads && BuildConfiguration === "Debug" && !runtimeHelpers.cspPolicy) {
        monoThreadInfo.updateCount++;
        try {
            (globalThis as any).monoThreadInfoFn = new Function(`//# sourceURL=https://${monoThreadInfo.updateCount}WorkerInfo${monoThreadInfo.isAttached ? monoThreadInfo.threadPrefix : ""}/\r\nconsole.log("${JSON.stringify(monoThreadInfo)}");`);
        }
        catch (ex) {
            runtimeHelpers.cspPolicy = true;
        }
    }
}

export function mono_wasm_pthread_ptr(): PThreadPtr {
    if (!WasmEnableThreads) return PThreadPtrNull;
    return (<any>Module)["_pthread_self"]();
}

export function mono_wasm_main_thread_ptr(): PThreadPtr {
    if (!WasmEnableThreads) return PThreadPtrNull;
    return (<any>Module)["_emscripten_main_runtime_thread_id"]();
}

export function postMessageToMain(message: MonoWorkerToMainMessage, transfer?: Transferable[]) {
    self.postMessage({
        [monoMessageSymbol]: message
    }, transfer ? transfer : []);
}

export interface MonoWorkerToMainMessage {
    monoCmd: WorkerToMainMessageType;
    info: PThreadInfo;
    port?: MessagePort;
}

/// Identification of the current thread executing on a worker
export interface PThreadSelf {
    info: PThreadInfo;
    portToBrowser: MessagePort;
    postMessageToBrowser: <T extends MonoThreadMessage>(message: T, transfer?: Transferable[]) => void;
    addEventListenerFromBrowser: (listener: <T extends MonoThreadMessage>(event: MessageEvent<T>) => void) => void;
}
