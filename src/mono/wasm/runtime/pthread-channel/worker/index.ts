// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { Module } from "../../imports";
import { makeChannelCreatedMonoMessage } from "../shared";

function monoDedicatedChannelMessageFromMainToWorker(event: MessageEvent<string>): void {
    console.debug("got message from main on the dedicated channel", event.data);
}

/// Called in the worker thread from mono when a new pthread is started
export function mono_wasm_on_pthread_created(worker_notify_ptr: number): void {
    console.log("waiting for main thread to aknowledge us");
    Atomics.wait(Module.HEAP32, worker_notify_ptr, 0);  // FIXME: any way we can avoid this?
    console.debug("creating a channel");
    const channel = new MessageChannel();
    const workerPort = channel.port1;
    const mainPort = channel.port2;
    workerPort.addEventListener("message", monoDedicatedChannelMessageFromMainToWorker);
    self.postMessage(makeChannelCreatedMonoMessage(mainPort), [mainPort]);
}

