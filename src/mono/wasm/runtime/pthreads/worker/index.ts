// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import MonoWasmThreads from "consts:monoWasmThreads";
import { Module, ENVIRONMENT_IS_PTHREAD } from "../../imports";
import { makeChannelCreatedMonoMessage, pthread_ptr } from "../shared";
import { mono_assert, is_nullish } from "../../types";
import type { MonoThreadMessage } from "../shared";
import {
    PThreadSelf,
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

class WorkerSelf implements PThreadSelf {
    readonly isBrowserThread = false;
    constructor(readonly pthread_id: pthread_ptr, readonly portToBrowser: MessagePort) { }
    postMessageToBrowser(message: MonoThreadMessage, transfer?: Transferable[]) {
        if (transfer) {
            this.portToBrowser.postMessage(message, transfer);
        } else {
            this.portToBrowser.postMessage(message);
        }
    }
    addEventListenerFromBrowser(listener: (event: MessageEvent<MonoThreadMessage>) => void) {
        this.portToBrowser.addEventListener("message", listener);
    }
}

// we are lying that this is never null, but afterThreadInit should be the first time we get to run any code
// in the worker, so this becomes non-null very early.
export let pthread_self: PThreadSelf = null as any as PThreadSelf;

/// This is the "public internal" API for runtime subsystems that wish to be notified about
/// pthreads that are running on the current worker.
/// Example:
///    currentWorkerThreadEvents.addEventListener(dotnetPthreadCreated, (ev: WorkerThreadEvent) => {
///       console.debug("MONO_WASM: thread created on worker with id", ev.pthread_ptr);
///    });
export const currentWorkerThreadEvents: WorkerThreadEventTarget =
    MonoWasmThreads ? new EventTarget() : null as any as WorkerThreadEventTarget; // treeshake if threads are disabled

function monoDedicatedChannelMessageFromMainToWorker(event: MessageEvent<string>): void {
    console.debug("MONO_WASM: got message from main on the dedicated channel", event.data);
}

function setupChannelToMainThread(pthread_ptr: pthread_ptr): PThreadSelf {
    console.debug("MONO_WASM: creating a channel", pthread_ptr);
    const channel = new MessageChannel();
    const workerPort = channel.port1;
    const mainPort = channel.port2;
    workerPort.addEventListener("message", monoDedicatedChannelMessageFromMainToWorker);
    workerPort.start();
    pthread_self = new WorkerSelf(pthread_ptr, workerPort);
    self.postMessage(makeChannelCreatedMonoMessage(pthread_ptr, mainPort), [mainPort]);
    return pthread_self;
}

/// This is an implementation detail function.
/// Called in the worker thread from mono when a pthread becomes attached to the mono runtime.
export function mono_wasm_pthread_on_pthread_attached(pthread_id: pthread_ptr): void {
    const self = pthread_self;
    mono_assert(self !== null && self.pthread_id == pthread_id, "expected pthread_self to be set already when attaching");
    console.debug("MONO_WASM: attaching pthread to runtime", pthread_id);
    currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadAttached, self));
}

/// This is an implementation detail function.
/// Called by emscripten when a pthread is setup to run on a worker.  Can be called multiple times
/// for the same worker, since emscripten can reuse workers.  This is an implementation detail, that shouldn't be used directly.
export function afterThreadInitTLS(): void {
    // don't do this callback for the main thread
    if (ENVIRONMENT_IS_PTHREAD) {
        const pthread_ptr = (<any>Module)["_pthread_self"]();
        mono_assert(!is_nullish(pthread_ptr), "pthread_self() returned null");
        console.debug("MONO_WASM: after thread init, pthread ptr", pthread_ptr);
        const self = setupChannelToMainThread(pthread_ptr);
        currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadCreated, self));
    }
}
