// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import MonoWasmThreads from "consts:monoWasmThreads";
import { Module, ENVIRONMENT_IS_PTHREAD } from "../../imports";
import { makeChannelCreatedMonoMessage, pthread_ptr } from "../shared";
import { mono_assert, is_nullish } from "../../types";
import {
    makeWorkerThreadEvent,
    dotnetPthreadCreated,
    dotnetPthreadAttached,
    WorkerThreadEventTarget
} from "./events";

// re-export some of the events types
export {
    WorkerThreadEventMap,
    dotnetPthreadAttached,
    dotnetPthreadCreated,
    WorkerThreadEvent,
    WorkerThreadEventTarget,
} from "./events";

export interface PThreadSelf {
    readonly pthread_id: pthread_ptr;
    postMessage: <T = object>(message: T, transfer?: Transferable[]) => void;
    addEventListener: <T>(listener: (event: MessageEvent<T>) => void) => void;
}

class WorkerSelf implements PThreadSelf {
    readonly port: MessagePort;
    readonly pthread_id: pthread_ptr;
    constructor(pthread_id: pthread_ptr, port: MessagePort) {
        this.port = port;
        this.pthread_id = pthread_id;
    }
    postMessage<T = object>(message: T, transfer?: Transferable[]) {
        this.port.postMessage(message, transfer);
    }
    addEventListener<T>(listener: (event: MessageEvent<T>) => void) {
        this.port.addEventListener("message", listener);
    }
}

export let pthread_self: PThreadSelf | null = null;

/// This is the "public internal" API for runtime subsystems that wish to be notified about
/// pthreads that are running on the current worker.
/// Example:
///    currentWorkerThreadEvents.addEventListener(dotnetPthreadCreated, (ev: WorkerThreadEvent) => {
///       console.debug ("thread created on worker with id", ev.pthread_ptr);
///    });
export const currentWorkerThreadEvents: WorkerThreadEventTarget =
    MonoWasmThreads ? new EventTarget() : null as any as WorkerThreadEventTarget; // treeshake if threads are disabled

function monoDedicatedChannelMessageFromMainToWorker(event: MessageEvent<string>): void {
    console.debug("got message from main on the dedicated channel", event.data);
}

let portToMain: MessagePort | null = null;

function setupChannelToMainThread(pthread_ptr: pthread_ptr): MessagePort {
    console.debug("creating a channel", pthread_ptr);
    const channel = new MessageChannel();
    const workerPort = channel.port1;
    const mainPort = channel.port2;
    workerPort.addEventListener("message", monoDedicatedChannelMessageFromMainToWorker);
    workerPort.start();
    portToMain = workerPort;
    pthread_self = new WorkerSelf(pthread_ptr, workerPort);
    self.postMessage(makeChannelCreatedMonoMessage(pthread_ptr, mainPort), [mainPort]);
    return workerPort;
}

/// This is an implementation detail function.
/// Called in the worker thread from mono when a pthread becomes attached to the mono runtime.
export function mono_wasm_pthread_on_pthread_attached(pthread_id: pthread_ptr): void {
    const port = portToMain;
    mono_assert(port !== null, "expected a port to the main thread");
    console.debug("attaching pthread to runtime", pthread_id);
    currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadAttached, pthread_id, port));
}

/// This is an implementation detail function.
/// Called by emscripten when a pthread is setup to run on a worker.  Can be called multiple times
/// for the same worker, since emscripten can reuse workers.  This is an implementation detail, that shouldn't be used directly.
export function afterThreadInitTLS(): void {
    // don't do this callback for the main thread
    if (ENVIRONMENT_IS_PTHREAD) {
        const pthread_ptr = (<any>Module)["_pthread_self"]();
        mono_assert(!is_nullish(pthread_ptr), "pthread_self() returned null");
        console.debug("after thread init, pthread ptr", pthread_ptr);
        const port = setupChannelToMainThread(pthread_ptr);
        currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadCreated, pthread_ptr, port));
    }
}
