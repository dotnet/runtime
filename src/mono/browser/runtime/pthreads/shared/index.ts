// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import { ENVIRONMENT_IS_PTHREAD, Module, loaderHelpers, mono_assert, runtimeHelpers } from "../../globals";
import { mono_log_debug, mono_set_thread_name } from "../../logging";
import { bindings_init } from "../../startup";
import { forceDisposeProxies } from "../../gc-handles";
import { GCHandle, GCHandleNull, WorkerToMainMessageType, monoMessageSymbol } from "../../types/internal";
import { MonoWorkerToMainMessage } from "./types";
import { monoThreadInfo } from "../worker";

/// Messages sent on the dedicated mono channel between a pthread and the browser thread

// We use a namespacing scheme to avoid collisions: type/command should be unique.
export interface MonoThreadMessage {
    // Type of message.  Generally a subsystem like "diagnostic_server", or "event_pipe", "debugger", etc.
    type: string;
    // A particular kind of message. For example, "started", "stopped", "stopped_with_error", etc.
    cmd: string;
}

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
    if (!runtimeHelpers.proxy_context_gc_handle) {
        runtimeHelpers.proxy_context_gc_handle = context_gc_handle;
        mono_log_debug("Installed JSSynchronizationContext");
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
    mono_assert(runtimeHelpers.proxy_context_gc_handle, "JSSynchronizationContext is not installed on this worker.");

    forceDisposeProxies(true, runtimeHelpers.diagnosticTracing);
    Module.runtimeKeepalivePop();

    runtimeHelpers.proxy_context_gc_handle = GCHandleNull;
    runtimeHelpers.mono_wasm_bindings_is_ready = false;
    update_thread_info();
}

// this is just for Debug build of the runtime, making it easier to debug worker threads
export function update_thread_info(): void {
    const threadType = monoThreadInfo.isUI ? "main"
        : !monoThreadInfo.isAttached ? "emsc"
            : monoThreadInfo.isTimer ? "timr"
                : monoThreadInfo.isLongRunning ? "long"
                    : monoThreadInfo.isThreadPoolGate ? "gate"
                        : monoThreadInfo.isDebugger ? "dbgr"
                            : monoThreadInfo.isThreadPoolWorker ? "pool"
                                : monoThreadInfo.isExternalEventLoop ? "jsww"
                                    : monoThreadInfo.isBackground ? "back"
                                        : "norm";
    monoThreadInfo.threadPrefix = `0x${monoThreadInfo.pthreadId.toString(16).padStart(8, "0")}-${threadType}`;

    loaderHelpers.set_thread_prefix(monoThreadInfo.threadPrefix!);
    if (!loaderHelpers.config.forwardConsoleLogsToWS) {
        mono_set_thread_name(monoThreadInfo.threadPrefix!);
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

export function mono_wasm_pthread_ptr(): number {
    if (!WasmEnableThreads) return 0;
    return (<any>Module)["_pthread_self"]();
}

export function mono_wasm_main_thread_ptr(): number {
    if (!WasmEnableThreads) return 0;
    return (<any>Module)["_emscripten_main_runtime_thread_id"]();
}

export function postMessageToMain(message: MonoWorkerToMainMessage, transfer?: Transferable[]) {
    self.postMessage({
        [monoMessageSymbol]: message
    }, transfer ? transfer : []);
}