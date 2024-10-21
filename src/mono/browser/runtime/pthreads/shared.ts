// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import type { GCHandle, MonoThreadMessage, PThreadInfo, PThreadPtr } from "../types/internal";

import { Module, loaderHelpers, runtimeHelpers } from "../globals";
import { set_thread_prefix } from "../logging";
import { monoMessageSymbol, PThreadPtrNull, WorkerToMainMessageType } from "../types/internal";
import { threads_c_functions as tcwraps } from "../cwraps";
import { forceThreadMemoryViewRefresh } from "../memory";

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

export function isMonoThreadMessage (x: unknown): x is MonoThreadMessage {
    if (typeof (x) !== "object" || x === null) {
        return false;
    }
    const xmsg = x as MonoThreadMessage;
    return typeof (xmsg.type) === "string" && typeof (xmsg.cmd) === "string";
}

// this is just for Debug build of the runtime, making it easier to debug worker threads
export function update_thread_info (): void {
    if (!WasmEnableThreads) return;
    const threadType = !monoThreadInfo.isRegistered ? "emsc"
        : monoThreadInfo.isUI ? "-UI-"
            : monoThreadInfo.isDeputy ? "dpty"
                : monoThreadInfo.isIo ? "-IO-"
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

    // this is just to make debugging easier by naming the thread debugger window.
    // It's not CSP compliant and possibly not performant, that's why it's only enabled in debug builds
    // in Release configuration, it would be a trimmed by rollup
    if (WasmEnableThreads && BuildConfiguration === "Debug" && !runtimeHelpers.cspPolicy) {
        monoThreadInfo.updateCount++;
        try {
            const url = `//# sourceURL=https://dotnet/thread/${monoThreadInfo.updateCount}-${monoThreadInfo.threadPrefix}`;
            const infoJson = JSON.stringify(monoThreadInfo, null, 2);
            const body = `const monoThreadInfo=${infoJson};\r\nconsole.log(monoThreadInfo);`;
            (globalThis as any).monoThreadInfoFn = new Function(body + "\r\n" + url);
        } catch (ex) {
            runtimeHelpers.cspPolicy = true;
        }
    }
}

export function exec_synchronization_context_pump (): void {
    if (!loaderHelpers.is_runtime_running()) {
        return;
    }
    forceThreadMemoryViewRefresh();
    try {
        tcwraps.mono_wasm_synchronization_context_pump();
    } catch (ex) {
        loaderHelpers.mono_exit(1, ex);
    }
}

export function mono_wasm_schedule_synchronization_context (): void {
    if (!WasmEnableThreads) return;
    Module.safeSetTimeout(exec_synchronization_context_pump, 0);
}

export function mono_wasm_pthread_ptr (): PThreadPtr {
    if (!WasmEnableThreads) return PThreadPtrNull;
    return (<any>Module)["_pthread_self"]();
}

export function mono_wasm_main_thread_ptr (): PThreadPtr {
    if (!WasmEnableThreads) return PThreadPtrNull;
    return (<any>Module)["_emscripten_main_runtime_thread_id"]();
}

export function postMessageToMain (message: MonoWorkerToMainMessage, transfer?: Transferable[]) {
    self.postMessage({
        [monoMessageSymbol]: message
    }, transfer ? transfer : []);
}

export interface MonoWorkerToMainMessage {
    monoCmd: WorkerToMainMessageType;
    info: PThreadInfo;
    port?: MessagePort;
    error?: string;
    deputyProxyGCHandle?: GCHandle;
}

/// Identification of the current thread executing on a worker
export interface PThreadSelf {
    info: PThreadInfo;
    portToBrowser: MessagePort;
    postMessageToBrowser: <T extends MonoThreadMessage>(message: T, transfer?: Transferable[]) => void;
    addEventListenerFromBrowser: (listener: <T extends MonoThreadMessage>(event: MessageEvent<T>) => void) => void;
}
