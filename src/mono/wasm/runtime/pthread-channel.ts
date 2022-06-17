// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { Module } from "./imports";

/// pthread_t in C
type pthread_ptr = number;


/// Summary:
///   This is meant to provide a dedicated communication channel between a pthread and the main thread.
///   The Emscripten threading APIs don't provide a way to send  [Transferable objects](https://developer.mozilla.org/en-US/docs/Glossary/Transferable_objects)
///     from one pthread to another.  It is also not great for sending around JS objects in general.
///
///   Instead, we hook a single custom message that gets called when a messsage is received from the pthread when it's created.
///
///   This is how we set it up:
///   1. Something in the runtime calls mono_thread_create_internal()
///   2. the runtime creates a 32-bit integer location on the new thread's stack (worker_notify_ptr) that we can use to synchronize
///   2. the runtime thread start routine calls mono_wasm_on_pthread_created in the new thread
///      - that function immediately blocks on worker_notify_ptr using Atomics.wait
///   3. the runtime posts an async function to the main thread to run mono_wasm_pthread_on_pthread_created_main_thread
///     - that function adds does worker.addEventHandler("message", monoMessageFromWorkerHandlerForMainThread)
///     - that handler's only job is to receive a "channel_created" message from the worker.
///     - the main thread wakes the worker by doing Atomics.store and Atomics.notify
///   4. the worker wakes up and posts a "channel_created" message to the main thread
///   5. our custom message handler runs on the main thread and receives the MessagePort
///   6. now the main thread and the worker have a dedicated communication channel
///
///  This could get better if the following things changed in Emscripten:
///  1. There was some hook on Module.Pthread.loadWasmModuleToWorker so that we could add our own message handler at the same time that Emscripten adds its own.
///     That would let us eliminate queuing async work on the main thread and block the worker until it's done.
///     This is because we can't easily add a handler to _every_ new worker as it is added to the pthread worker pool.
///
///  2. If we could have a way to avoid collisions with Emscripten's own message handlers entirely, we wouldn't need a MessageChannel at all, we could just piggyback on the normal worker communication.
///
///
/// FIXME: we really need to hook deeper - if a third party library creates a thread we need to attach it when it attaches to the runtime.

/// a symbol that we use as a key on messages on the global worker-to-main channel to identify our own messages
/// we can't use an actual JS Symbol because those don't transfer between workers.
const monoSymbol = "__mono_message_please_dont_collide__"; //Symbol("mono");

type MonoMessageBody = {
    mono_cmd: string;
}

/// Messages on the global worker-to-main channel have this shape
interface MonoMessage<T extends MonoMessageBody> {
    [monoSymbol]: T;
}

interface MonoMessageBodyChannelCreated extends MonoMessageBody {
    mono_cmd: "channel_created";
    port: MessagePort;
}



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
    (<DedicatedWorkerGlobalScope>self).postMessage({ [monoSymbol]: { "mono_cmd": "channel_created", "port": mainPort } }, [mainPort]);
}

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
    const worker = mainThradGetWorkerForPthread(pthread_ptr);
    worker.addEventListener("message", monoMessageFromWorkerHandlerForMainThread);
    // wake the worker
    Atomics.store(Module.HEAP32, worker_notify_ptr, 1);
    Atomics.notify(Module.HEAP32, worker_notify_ptr, 1);
}

function mainThradGetWorkerForPthread(pthread_ptr: pthread_ptr): Worker {
    // see https://github.com/emscripten-core/emscripten/pull/16239
    return (<any>Module).PThread.pthreads[pthread_ptr].worker;
}
