// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../../imports";

/// pthread_t in C
export type pthread_ptr = number;

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
        browser_thread_id_lazy = (<any>Module)["_emscripten_main_browser_thread_id"]() as pthread_ptr;
    }
    return browser_thread_id_lazy;
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

/// Messages sent using the worker object's postMessage() method ///

/// a symbol that we use as a key on messages on the global worker-to-main channel to identify our own messages
/// we can't use an actual JS Symbol because those don't transfer between workers.
export const monoSymbol = "__mono_message_please_dont_collide__"; //Symbol("mono");

/// Messages sent from the main thread using Worker.postMessage or from the worker using DedicatedWorkerGlobalScope.postMessage
/// should use this interface.  The message event is also used by emscripten internals (and possibly by 3rd party libraries targeting Emscripten).
/// We should just use this to establish a dedicated MessagePort for Mono's uses.
export interface MonoWorkerMessage {
    [monoSymbol]: object;
}

/// The message sent early during pthread creation to set up a dedicated MessagePort for Mono between the main thread and the pthread.
export interface MonoWorkerMessageChannelCreated<TPort> extends MonoWorkerMessage {
    [monoSymbol]: {
        mono_cmd: "channel_created";
        thread_id: pthread_ptr;
        port: TPort;
    };
}

export function makeChannelCreatedMonoMessage<TPort>(thread_id: pthread_ptr, port: TPort): MonoWorkerMessageChannelCreated<TPort> {
    return {
        [monoSymbol]: {
            mono_cmd: "channel_created",
            thread_id,
            port
        }
    };
}

export function isMonoWorkerMessage(message: unknown): message is MonoWorkerMessage {
    return message !== undefined && typeof message === "object" && message !== null && monoSymbol in message;
}

export function isMonoWorkerMessageChannelCreated<TPort>(message: MonoWorkerMessageChannelCreated<TPort>): message is MonoWorkerMessageChannelCreated<TPort> {
    if (isMonoWorkerMessage(message)) {
        const monoMessage = message[monoSymbol];
        if (monoMessage.mono_cmd === "channel_created") {
            return true;
        }
    }
    return false;
}
