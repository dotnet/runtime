// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import { ENVIRONMENT_IS_PTHREAD, Module, mono_assert, runtimeHelpers } from "../../globals";
import { MonoConfig } from "../../types";
import { pthreadPtr } from "./types";
import { mono_log_debug } from "../../logging";
import { bindings_init } from "../../startup";
import { forceDisposeProxies } from "../../gc-handles";
import { pthread_self } from "../worker";
import { GCHandle, GCHandleNull } from "../../types/internal";

export interface PThreadInfo {
    readonly pthreadId: pthreadPtr;
    readonly isBrowserThread: boolean;
}

export const MainThread: PThreadInfo = {
    get pthreadId(): pthreadPtr {
        return mono_wasm_main_thread_ptr();
    },
    isBrowserThread: true
};

const enum WorkerMonoCommandType {
    enabledInterop = "notify_enabled_interop",
    channelCreated = "channel_created",
    preload = "preload",
}

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

// message from the main thread to the pthread worker that passes the MonoConfig to the worker
export interface MonoThreadMessageApplyMonoConfig extends MonoThreadMessage {
    type: "pthread";
    cmd: "apply_mono_config";
    config: string;
}

export function makeMonoThreadMessageApplyMonoConfig(config: MonoConfig): MonoThreadMessageApplyMonoConfig {
    return {
        type: "pthread",
        cmd: "apply_mono_config",
        config: JSON.stringify(config)
    };
}

/// Messages sent using the worker object's postMessage() method ///

/// a symbol that we use as a key on messages on the global worker-to-main channel to identify our own messages
/// we can't use an actual JS Symbol because those don't transfer between workers.
export const monoSymbol = "__mono_message_please_dont_collide__"; //Symbol("mono");

/// Messages sent from the main thread using Worker.postMessage or from the worker using DedicatedWorkerGlobalScope.postMessage
/// should use this interface.  The message event is also used by emscripten internals (and possibly by 3rd party libraries targeting Emscripten).
/// We should just use this to establish a dedicated MessagePort for Mono's uses.
export interface MonoWorkerMessage {
    [monoSymbol]: {
        monoCmd: WorkerMonoCommandType;
    };
}
export type MonoWorkerMessagePort = MonoWorkerMessage & {
    [monoSymbol]: {
        port: MessagePort;
    };
}

/// The message sent early during pthread creation to set up a dedicated MessagePort for Mono between the main thread and the pthread.
export interface MonoWorkerMessageChannelCreated extends MonoWorkerMessage {
    [monoSymbol]: {
        monoCmd: WorkerMonoCommandType.channelCreated;
        threadId: pthreadPtr;
        port: MessagePort;
    };
}

export interface MonoWorkerMessageEnabledInterop extends MonoWorkerMessage {
    [monoSymbol]: {
        monoCmd: WorkerMonoCommandType.enabledInterop;
        threadId: pthreadPtr;
    };
}

export interface MonoWorkerMessagePreload extends MonoWorkerMessagePort {
    [monoSymbol]: {
        monoCmd: WorkerMonoCommandType.preload;
        port: MessagePort;
    };
}

export function makeChannelCreatedMonoMessage(threadId: pthreadPtr, port: MessagePort): MonoWorkerMessageChannelCreated {
    return {
        [monoSymbol]: {
            monoCmd: WorkerMonoCommandType.channelCreated,
            threadId: threadId,
            port
        }
    };
}
export function makeEnabledInteropMonoMessage(threadId: pthreadPtr): MonoWorkerMessageEnabledInterop {
    return {
        [monoSymbol]: {
            monoCmd: WorkerMonoCommandType.enabledInterop,
            threadId: threadId,
        }
    };
}

export function isMonoWorkerMessage(message: unknown): message is MonoWorkerMessage {
    return message !== undefined && typeof message === "object" && message !== null && monoSymbol in message;
}

export function isMonoWorkerMessageChannelCreated(message: MonoWorkerMessage): message is MonoWorkerMessageChannelCreated {
    if (isMonoWorkerMessage(message)) {
        const monoMessage = message[monoSymbol];
        if (monoMessage.monoCmd === WorkerMonoCommandType.channelCreated) {
            return true;
        }
    }
    return false;
}

export function isMonoWorkerMessageEnabledInterop(message: MonoWorkerMessage): message is MonoWorkerMessageEnabledInterop {
    if (isMonoWorkerMessage(message)) {
        const monoMessage = message[monoSymbol];
        if (monoMessage.monoCmd === WorkerMonoCommandType.enabledInterop) {
            return true;
        }
    }
    return false;
}

export function isMonoWorkerMessagePreload(message: MonoWorkerMessage): message is MonoWorkerMessagePreload {
    if (isMonoWorkerMessage(message)) {
        const monoMessage = message[monoSymbol];
        if (monoMessage.monoCmd === WorkerMonoCommandType.preload) {
            return true;
        }
    }
    return false;
}

export function mono_wasm_install_js_worker_interop(context_gc_handle: GCHandle): void {
    if (!WasmEnableThreads) return;
    bindings_init();
    if (!runtimeHelpers.proxy_context_gc_handle) {
        runtimeHelpers.proxy_context_gc_handle = context_gc_handle;
        mono_log_debug("Installed JSSynchronizationContext");
    }
    Module.runtimeKeepalivePush();
    if (ENVIRONMENT_IS_PTHREAD) {
        self.postMessage(makeEnabledInteropMonoMessage(pthread_self.pthreadId), []);
    }

    set_thread_info(pthread_self ? pthread_self.pthreadId : 0, true, true, true);
}

export function mono_wasm_uninstall_js_worker_interop(): void {
    if (!WasmEnableThreads) return;
    mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "JS interop is not installed on this worker.");
    mono_assert(runtimeHelpers.proxy_context_gc_handle, "JSSynchronizationContext is not installed on this worker.");

    forceDisposeProxies(true, runtimeHelpers.diagnosticTracing);
    Module.runtimeKeepalivePop();

    runtimeHelpers.proxy_context_gc_handle = GCHandleNull;
    runtimeHelpers.mono_wasm_bindings_is_ready = false;
    set_thread_info(pthread_self ? pthread_self.pthreadId : 0, true, false, false);
}

// this is just for Debug build of the runtime, making it easier to debug worker threads
export function set_thread_info(pthread_ptr: number, isAttached: boolean, hasInterop: boolean, hasSynchronization: boolean): void {
    if (WasmEnableThreads && BuildConfiguration === "Debug" && !runtimeHelpers.cspPolicy) {
        try {
            (globalThis as any).monoThreadInfo = new Function(`//# sourceURL=https://WorkerInfo/\r\nconsole.log("tid:0x${pthread_ptr.toString(16)} isAttached:${isAttached} hasInterop:${!!hasInterop} hasSynchronization:${hasSynchronization}" );`);
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
