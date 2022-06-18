// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../../imports";
import { monoSymbol, pthread_ptr, MonoMessage, MonoMessageBodyChannelCreated } from "../shared";

// handler that runs in the main thread when a message is received from a pthread
function monoMessageFromWorkerHandlerForMainThread(event: MessageEvent<MonoMessage<MonoMessageBodyChannelCreated>>): void {
    if (event.data[monoSymbol] !== undefined) {
        const message = event.data[monoSymbol];
        console.debug("received message", message);
        switch (message.mono_cmd) {
            case "channel_created": {
                const port = message.port;
                port.postMessage("You exist!");
            }
        }
    }
}

/// Called asynchronously in the main thread from mono when a new pthread is started
export function mono_wasm_pthread_on_pthread_created_main_thread(pthread_ptr: pthread_ptr, worker_notify_ptr: number): void {
    console.log("pthread created");
    const worker = mainThreadGetWorkerForPthread(pthread_ptr);
    worker.addEventListener("message", monoMessageFromWorkerHandlerForMainThread);
    // wake the worker
    Atomics.store(Module.HEAP32, worker_notify_ptr, 1);
    Atomics.notify(Module.HEAP32, worker_notify_ptr, 1);
}

function mainThreadGetWorkerForPthread(pthread_ptr: pthread_ptr): Worker {
    // see https://github.com/emscripten-core/emscripten/pull/16239
    return (<any>Module).PThread.pthreads[pthread_ptr].worker;
}
