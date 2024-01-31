// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import type { pthreadPtr } from "../shared/types";
import type { PThreadInfo, MonoThreadMessage } from "../shared";

/// Identification of the current thread executing on a worker
export interface PThreadSelf extends PThreadInfo {
    readonly pthreadId: pthreadPtr;
    readonly portToBrowser: MessagePort;
    readonly isBrowserThread: boolean;
    postMessageToBrowser: <T extends MonoThreadMessage>(message: T, transfer?: Transferable[]) => void;
    addEventListenerFromBrowser: (listener: <T extends MonoThreadMessage>(event: MessageEvent<T>) => void) => void;
}

export const dotnetPthreadCreated = "dotnet:pthread:created" as const;
export const dotnetPthreadAttached = "dotnet:pthread:attached" as const;

/// Events emitted on the current worker when Emscripten uses it to run a pthread.
export interface WorkerThreadEventMap {
    /// Emitted on the current worker when Emscripten first creates a pthread on the current worker.
    /// May be fired multiple times because Emscripten reuses workers to run a new pthread after the current one is finished.
    [dotnetPthreadCreated]: WorkerThreadEvent;
    // Emitted on the current worker when a pthread attaches to Mono.
    // Threads may attach and detach to Mono multiple times during their lifetime.
    [dotnetPthreadAttached]: WorkerThreadEvent;
}

export interface WorkerThreadEvent extends Event {
    readonly pthread_self: PThreadSelf;
}

export interface WorkerThreadEventTarget extends EventTarget {
    dispatchEvent(event: WorkerThreadEvent): boolean;

    addEventListener<K extends keyof WorkerThreadEventMap>(type: K, listener: ((this: WorkerThreadEventTarget, ev: WorkerThreadEventMap[K]) => any) | null, options?: boolean | AddEventListenerOptions): void;
    addEventListener(type: string, callback: EventListenerOrEventListenerObject | null, options?: boolean | AddEventListenerOptions): void;
}

let WorkerThreadEventClassConstructor: new (type: keyof WorkerThreadEventMap, pthread_self: PThreadSelf) => WorkerThreadEvent;
export const makeWorkerThreadEvent: (type: keyof WorkerThreadEventMap, pthread_self: PThreadSelf) => WorkerThreadEvent = !WasmEnableThreads
    ? (() => { throw new Error("threads support disabled"); })
    : ((type: keyof WorkerThreadEventMap, pthread_self: PThreadSelf) => {
        if (!WorkerThreadEventClassConstructor) WorkerThreadEventClassConstructor = class WorkerThreadEventImpl extends Event implements WorkerThreadEvent {
            constructor(type: keyof WorkerThreadEventMap, readonly pthread_self: PThreadSelf) {
                super(type);
            }
        };
        return new WorkerThreadEventClassConstructor(type, pthread_self);
    });

