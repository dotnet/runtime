// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { Module, ENVIRONMENT_IS_PTHREAD } from "../../imports";
import { makeChannelCreatedMonoMessage, pthread_ptr } from "../shared";

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

/// Called in the worker thread from mono when a new pthread is started
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

export function mono_wasm_pthread_on_pthread_attached(pthread_id: pthread_ptr): void {
    console.debug("attaching pthread to runtime", pthread_id);
}

/// Called by emscripten when a pthread is setup to run on a worker.  Can be called multiple times
/// for the same worker, since emscripten can reuse workers.
export function afterThreadInit(): void {
    console.debug("after thread init");
    const pthread_ptr = (<any>Module)["_pthread_self"]();
    console.debug("pthread ptr", pthread_ptr);
    // don't do this callback for the main thread
    if (ENVIRONMENT_IS_PTHREAD) {
        mono_wasm_pthread_on_pthread_created(pthread_ptr);
    }
}
