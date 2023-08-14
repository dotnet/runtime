// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import MonoWasmThreads from "consts:monoWasmThreads";

import { Module, ENVIRONMENT_IS_PTHREAD, mono_assert } from "../../globals";
import { makeChannelCreatedMonoMessage, set_thread_info } from "../shared";
import type { pthreadPtr } from "../shared/types";
import { is_nullish } from "../../types/internal";
import type { MonoThreadMessage } from "../shared";
import {
    PThreadSelf,
    makeWorkerThreadEvent,
    dotnetPthreadCreated,
    dotnetPthreadAttached,
    WorkerThreadEventTarget
} from "./events";
import { postRunWorker, preRunWorker } from "../../startup";
import { mono_log_debug } from "../../logging";
import { mono_set_thread_id } from "../../logging";

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
    constructor(readonly pthreadId: pthreadPtr, readonly portToBrowser: MessagePort) { }
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
///       mono_trace("thread created on worker with id", ev.pthread_ptr);
///    });
export let currentWorkerThreadEvents: WorkerThreadEventTarget = undefined as any;

export function initWorkerThreadEvents() {
    // treeshake if threads are disabled
    currentWorkerThreadEvents = MonoWasmThreads ? new globalThis.EventTarget() : null as any as WorkerThreadEventTarget;
}

// this is the message handler for the worker that receives messages from the main thread
// extend this with new cases as needed
function monoDedicatedChannelMessageFromMainToWorker(event: MessageEvent<string>): void {
    mono_log_debug("got message from main on the dedicated channel", event.data);
}


function setupChannelToMainThread(pthread_ptr: pthreadPtr): PThreadSelf {
    mono_log_debug("creating a channel", pthread_ptr);
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
/// Called in the worker thread (not main thread) from mono when a pthread becomes attached to the mono runtime.
export function mono_wasm_pthread_on_pthread_attached(pthread_id: number): void {
    const self = pthread_self;
    mono_assert(self !== null && self.pthreadId == pthread_id, "expected pthread_self to be set already when attaching");
    mono_set_thread_id("0x" + pthread_id.toString(16));
    mono_log_debug("attaching pthread to mono runtime 0x" + pthread_id.toString(16));
    preRunWorker();
    set_thread_info(pthread_id, true, false, false);
    currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadAttached, self));
}

/// Called in the worker thread (not main thread) from mono when a pthread becomes detached from the mono runtime.
export function mono_wasm_pthread_on_pthread_detached(pthread_id: number): void {
    mono_log_debug("detaching pthread from mono runtime 0x" + pthread_id.toString(16));
    postRunWorker();
    set_thread_info(pthread_id, false, false, false);
    mono_set_thread_id("");
}

/// This is an implementation detail function.
/// Called by emscripten when a pthread is setup to run on a worker.  Can be called multiple times
/// for the same worker, since emscripten can reuse workers.  This is an implementation detail, that shouldn't be used directly.
export function afterThreadInitTLS(): void {
    // don't do this callback for the main thread
    if (ENVIRONMENT_IS_PTHREAD) {
        const pthread_ptr = (<any>Module)["_pthread_self"]();
        mono_assert(!is_nullish(pthread_ptr), "pthread_self() returned null");
        mono_log_debug("after thread init, pthread ptr 0x" + pthread_ptr.toString(16));
        const self = setupChannelToMainThread(pthread_ptr);
        currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadCreated, self));
    }
}
