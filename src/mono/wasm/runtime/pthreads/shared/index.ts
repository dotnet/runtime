// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import MonoWasmThreads from "consts:monoWasmThreads";

import { ENVIRONMENT_IS_PTHREAD, Module, runtimeHelpers } from "../../globals";
import { MonoConfig } from "../../types";
import { pthreadPtr } from "./types";

export interface PThreadInfo {
    readonly pthreadId: pthreadPtr;
    readonly isBrowserThread: boolean;
}

export const MainThread: PThreadInfo = {
    get pthreadId(): pthreadPtr {
        return getBrowserThreadID();
    },
    isBrowserThread: true
};

let browserThreadIdLazy: pthreadPtr | undefined;
export function getBrowserThreadID(): pthreadPtr {
    if (browserThreadIdLazy === undefined) {
        browserThreadIdLazy = (<any>Module)["_emscripten_main_runtime_thread_id"]() as pthreadPtr;
    }
    return browserThreadIdLazy;
}

const enum WorkerMonoCommandType {
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
export interface MonoWorkerMessage<TPort> {
    [monoSymbol]: {
        monoCmd: WorkerMonoCommandType;
        port: TPort;
    };
}

/// The message sent early during pthread creation to set up a dedicated MessagePort for Mono between the main thread and the pthread.
export interface MonoWorkerMessageChannelCreated<TPort> extends MonoWorkerMessage<TPort> {
    [monoSymbol]: {
        monoCmd: WorkerMonoCommandType.channelCreated;
        threadId: pthreadPtr;
        port: TPort;
    };
}

export interface MonoWorkerMessagePreload<TPort> extends MonoWorkerMessage<TPort> {
    [monoSymbol]: {
        monoCmd: WorkerMonoCommandType.preload;
        port: TPort;
    };
}

export function makeChannelCreatedMonoMessage<TPort>(threadId: pthreadPtr, port: TPort): MonoWorkerMessageChannelCreated<TPort> {
    return {
        [monoSymbol]: {
            monoCmd: WorkerMonoCommandType.channelCreated,
            threadId: threadId,
            port
        }
    };
}

export function isMonoWorkerMessage(message: unknown): message is MonoWorkerMessage<any> {
    return message !== undefined && typeof message === "object" && message !== null && monoSymbol in message;
}

export function isMonoWorkerMessageChannelCreated<TPort>(message: MonoWorkerMessage<TPort>): message is MonoWorkerMessageChannelCreated<TPort> {
    if (isMonoWorkerMessage(message)) {
        const monoMessage = message[monoSymbol];
        if (monoMessage.monoCmd === WorkerMonoCommandType.channelCreated) {
            return true;
        }
    }
    return false;
}

export function isMonoWorkerMessagePreload<TPort>(message: MonoWorkerMessage<TPort>): message is MonoWorkerMessagePreload<TPort> {
    if (isMonoWorkerMessage(message)) {
        const monoMessage = message[monoSymbol];
        if (monoMessage.monoCmd === WorkerMonoCommandType.preload) {
            return true;
        }
    }
    return false;
}

let synchronization_context_installed = false;
export function install_synchronization_context(): void {
    if (MonoWasmThreads && !synchronization_context_installed) {
        runtimeHelpers.javaScriptExports.install_synchronization_context();
        synchronization_context_installed = true;
    }
}

export function assert_synchronization_context(): void {
    if (MonoWasmThreads) {
        // TODO mono_assert(synchronization_context_installed, "Synchronization context not installed on the current worker. Please use dedicated worker for working with JavaScript interop.");
    }
}

export function assert_legacy_interop(): void {
    if (MonoWasmThreads) {
        mono_assert(!ENVIRONMENT_IS_PTHREAD, "Legacy interop is not supported with WebAssembly threads.");
    }
}

