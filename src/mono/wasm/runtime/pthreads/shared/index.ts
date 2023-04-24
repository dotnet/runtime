// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../../imports";
import { MonoConfig } from "../../types";
import { pthread_ptr } from "./types";

export interface PThreadInfo {
    readonly pthread_id: pthread_ptr;
    readonly isBrowserThread: boolean;
}

export const MainThread: PThreadInfo = {
    get pthread_id(): pthread_ptr {
        return getBrowserThreadID();
    },
    isBrowserThread: true
};

let browser_thread_id_lazy: pthread_ptr | undefined;
export function getBrowserThreadID(): pthread_ptr {
    if (browser_thread_id_lazy === undefined) {
        browser_thread_id_lazy = (<any>Module)["_emscripten_main_runtime_thread_id"]() as pthread_ptr;
    }
    return browser_thread_id_lazy;
}

const enum WorkerMonoCommandType {
    channel_created = "channel_created",
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
        mono_cmd: WorkerMonoCommandType;
        port: TPort;
    };
}

/// The message sent early during pthread creation to set up a dedicated MessagePort for Mono between the main thread and the pthread.
export interface MonoWorkerMessageChannelCreated<TPort> extends MonoWorkerMessage<TPort> {
    [monoSymbol]: {
        mono_cmd: WorkerMonoCommandType.channel_created;
        thread_id: pthread_ptr;
        port: TPort;
    };
}

export interface MonoWorkerMessagePreload<TPort> extends MonoWorkerMessage<TPort> {
    [monoSymbol]: {
        mono_cmd: WorkerMonoCommandType.preload;
        port: TPort;
    };
}

export function makeChannelCreatedMonoMessage<TPort>(thread_id: pthread_ptr, port: TPort): MonoWorkerMessageChannelCreated<TPort> {
    return {
        [monoSymbol]: {
            mono_cmd: WorkerMonoCommandType.channel_created,
            thread_id,
            port
        }
    };
}

export function makePreloadMonoMessage<TPort>(port: TPort): MonoWorkerMessagePreload<TPort> {
    return {
        [monoSymbol]: {
            mono_cmd: WorkerMonoCommandType.preload,
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
        if (monoMessage.mono_cmd === WorkerMonoCommandType.channel_created) {
            return true;
        }
    }
    return false;
}

export function isMonoWorkerMessagePreload<TPort>(message: MonoWorkerMessage<TPort>): message is MonoWorkerMessagePreload<TPort> {
    if (isMonoWorkerMessage(message)) {
        const monoMessage = message[monoSymbol];
        if (monoMessage.mono_cmd === WorkerMonoCommandType.preload) {
            return true;
        }
    }
    return false;
}
