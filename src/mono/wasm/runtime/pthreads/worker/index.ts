// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { Module, ENVIRONMENT_IS_PTHREAD } from "../../imports";
import { makeChannelCreatedMonoMessage, pthread_ptr } from "../shared";
import { mono_assert, is_nullish } from "../../types";

export type ThreadCreatedCallback = (pthread_ptr: pthread_ptr, main_port: MessagePort) => void;

const threadCreatedCallbacks = Array<ThreadCreatedCallback>();

/// Adds a callback to be called when a new pthread is created.
/// It is passed a MessagePort that can be used to communicate with the main thread by adding an event listener to it using addEventListener("message", ...)
export function addThreadCreatedCallback(fn: ThreadCreatedCallback): void {
    threadCreatedCallbacks.push(fn);
}

function monoDedicatedChannelMessageFromMainToWorker(event: MessageEvent<string>): void {
    console.debug("got message from main on the dedicated channel", event.data);
}

/// Called in the worker thread when a new pthread is started by Emscripten.
/// The pthread may have nothing to do with Mono yet.
/// It could have been started on behalf of a third party native library.
export function mono_wasm_pthread_on_pthread_created(pthread_id: pthread_ptr): void {
    console.debug("creating a channel", pthread_id);
    const channel = new MessageChannel();
    const workerPort = channel.port1;
    const mainPort = channel.port2;
    workerPort.addEventListener("message", monoDedicatedChannelMessageFromMainToWorker);
    workerPort.start();
    self.postMessage(makeChannelCreatedMonoMessage(pthread_id, mainPort), [mainPort]);
    for (const fn of threadCreatedCallbacks) {
        fn(pthread_id, workerPort);
    }
}

/// Called in the worker thread from mono when a pthread becomes attached to the mono runtime.
export function mono_wasm_pthread_on_pthread_attached(pthread_id: pthread_ptr): void {
    console.debug("attaching pthread to runtime", pthread_id);
}

/// Called by emscripten when a pthread is setup to run on a worker.  Can be called multiple times
/// for the same worker, since emscripten can reuse workers.  This is an implementation detail, that shouldn't be used directly.
/// See mono_wasm_pthread_on_pthread_created for the substantive part of this.
export function afterThreadInit(): void {
    // don't do this callback for the main thread
    if (ENVIRONMENT_IS_PTHREAD) {
        const pthread_ptr = (<any>Module)["_pthread_self"]();
        mono_assert(!is_nullish(pthread_ptr), "pthread_self() returned null");
        console.debug("after thread init, pthread ptr", pthread_ptr);
        mono_wasm_pthread_on_pthread_created(pthread_ptr);
    }
}
