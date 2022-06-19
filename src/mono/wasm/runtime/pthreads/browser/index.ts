// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../../imports";
import { isMonoWorkerMessage, getPortFromMonoWorkerMessage, pthread_ptr, MonoWorkerMessageChannelCreated } from "../shared";

const threads: Map<pthread_ptr, Thread> = new Map();

export interface Thread {
    readonly pthread_ptr: pthread_ptr;
    readonly worker: Worker;
    readonly port: MessagePort;
}

function addThread(pthread_ptr: pthread_ptr, worker: Worker, port: MessagePort): void {
    threads.set(pthread_ptr, { pthread_ptr, worker, port });
}

/// Given a thread id, return the thread object with the worker where the thread is running, and a message port.
export const getThread = (pthread_ptr: pthread_ptr): Thread | undefined => threads.get(pthread_ptr);

/// Returns all the threads we know about
export const getThreadIds = (): IterableIterator<pthread_ptr> => threads.keys();

// handler that runs in the main thread when a message is received from a pthread
function monoWorkerMessageHandler(pthread_ptr: pthread_ptr, worker: Worker, ev: MessageEvent<MonoWorkerMessageChannelCreated<MessagePort>>): void {
    /// N.B. important to ignore messages we don't recognize - Emscripten uses the message event to send internal messages
    if (isMonoWorkerMessage(ev.data)) {
        const port = getPortFromMonoWorkerMessage(ev.data);
        if (port !== undefined) {
            addThread(pthread_ptr, worker, port);
        }
    }
}

/// Called asynchronously in the main thread from mono when a new pthread is started
export function mono_wasm_pthread_on_pthread_created_main_thread(pthread_ptr: pthread_ptr, worker_notify_ptr: number): void {
    console.log("pthread created");
    const worker = getWorker(pthread_ptr);
    worker.addEventListener("message", (ev) => monoWorkerMessageHandler(pthread_ptr, worker, ev));
    // wake the worker
    Atomics.store(Module.HEAP32, worker_notify_ptr, 1);
    Atomics.notify(Module.HEAP32, worker_notify_ptr, 1);
}

function getWorker(pthread_ptr: pthread_ptr): Worker {
    // see https://github.com/emscripten-core/emscripten/pull/16239
    return (<any>Module).PThread.pthreads[pthread_ptr].worker;
}


