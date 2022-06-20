// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { Module } from "../../imports";
import { makeChannelCreatedMonoMessage, pthread_ptr } from "../shared";

export type ThreadCreatedCallback = (pthread_ptr: pthread_ptr, main_port: MessagePort) => void;

const threadCreatedCallbacks = Array<ThreadCreatedCallback>();

/// Adds a callback to be called when a new pthread is created.
/// It is passed a MessagePort that can be used to communicate with the main thread by adding an event listener to it using addEventListener("message", ...)
///
/// FIXME: when do we want this to get called? has to be early in the worker lifecycle
export function addThreadCreatedCallback(fn: ThreadCreatedCallback): void {
    threadCreatedCallbacks.push(fn);
}

function monoDedicatedChannelMessageFromMainToWorker(event: MessageEvent<string>): void {
    console.debug("got message from main on the dedicated channel", event.data);
}

/// Called in the worker thread from mono when a new pthread is started
export function mono_wasm_pthread_on_pthread_created(pthread_id: pthread_ptr, worker_notify_ptr: number): void {
    console.log("waiting for main thread to aknowledge us");
    Atomics.wait(Module.HEAP32, worker_notify_ptr, 0);  // FIXME: any way we can avoid this?
    console.debug("creating a channel");
    const channel = new MessageChannel();
    const workerPort = channel.port1;
    const mainPort = channel.port2;
    workerPort.addEventListener("message", monoDedicatedChannelMessageFromMainToWorker);
    workerPort.start();
    self.postMessage(makeChannelCreatedMonoMessage(mainPort), [mainPort]);
    for (const fn of threadCreatedCallbacks) {
        fn(pthread_id, workerPort);
    }
}
