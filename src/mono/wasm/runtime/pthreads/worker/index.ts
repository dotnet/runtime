// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

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

/// This is the "public internal" API for runtime subsystems that wish to be notified about
/// pthreads that are running on the current worker.
/// Example:
///    currentWorkerThreadEvents.addEventListener(dotnetPthreadCreated, (ev: WorkerThreadEvent) => {
///       console.debug ("thread created on worker with id", ev.pthread_ptr);
///    });
export const currentWorkerThreadEvents: WorkerThreadEventTarget = new EventTarget();

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
export function afterThreadInit(): void {
    // don't do this callback for the main thread
    if (ENVIRONMENT_IS_PTHREAD) {
        const pthread_ptr = (<any>Module)["_pthread_self"]();
        mono_assert(!is_nullish(pthread_ptr), "pthread_self() returned null");
        console.debug("after thread init, pthread ptr", pthread_ptr);
        const port = setupChannelToMainThread(pthread_ptr);
        currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadCreated, pthread_ptr, port));
    }
}
