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

function addThread(pthread_ptr: pthread_ptr, worker: Worker, port: MessagePort): Thread {
    const thread = { pthread_ptr, worker, port };
    threads.set(pthread_ptr, thread);
    return thread;
}

function removeThread(pthread_ptr: pthread_ptr): void {
    threads.delete(pthread_ptr);
}

/// Given a thread id, return the thread object with the worker where the thread is running, and a message port.
export function getThread(pthread_ptr: pthread_ptr): Thread | undefined {
    const thread = threads.get(pthread_ptr);
    if (thread === undefined) {
        return undefined;
    }
    // validate that the worker is still running pthread_ptr
    const worker = thread.worker;
    if (Internals.getThreadId(worker) !== pthread_ptr) {
        removeThread(pthread_ptr);
        thread.port.close();
        return undefined;
    }
    return thread;
}

/// Returns all the threads we know about
export const getThreadIds = (): IterableIterator<pthread_ptr> => threads.keys();

function monoDedicatedChannelMessageFromWorkerToMain(event: MessageEvent<unknown>, thread: Thread): void {
    // TODO: add callbacks that will be called from here
    console.debug("got message from worker on the dedicated channel", event.data, thread);
}

// handler that runs in the main thread when a message is received from a pthread worker
function monoWorkerMessageHandler(pthread_ptr: pthread_ptr, worker: Worker, ev: MessageEvent<MonoWorkerMessageChannelCreated<MessagePort>>): void {
    /// N.B. important to ignore messages we don't recognize - Emscripten uses the message event to send internal messages
    if (isMonoWorkerMessage(ev.data)) {
        const port = getPortFromMonoWorkerMessage(ev.data);
        if (port !== undefined) {
            const thread = addThread(pthread_ptr, worker, port);
            port.addEventListener("message", (ev) => monoDedicatedChannelMessageFromWorkerToMain(ev, thread));
            port.start();
        }
    }
}

/// Called asynchronously in the main thread from mono when a new pthread is started
export function mono_wasm_pthread_on_pthread_created_main_thread(pthread_ptr: pthread_ptr, worker_notify_ptr: number): void {
    console.log("pthread created");
    const worker = Internals.getWorker(pthread_ptr);
    worker.addEventListener("message", (ev) => monoWorkerMessageHandler(pthread_ptr, worker, ev));
    // wake the worker
    Atomics.store(Module.HEAP32, worker_notify_ptr, 1);
    Atomics.notify(Module.HEAP32, worker_notify_ptr, 1);
}

/// These utility functions dig into Emscripten internals
const Internals = {
    getWorker: (pthread_ptr: pthread_ptr): Worker => {
        // see https://github.com/emscripten-core/emscripten/pull/16239
        return (<any>Module).PThread.pthreads[pthread_ptr].worker;
    },
    getThreadId: (worker: Worker): pthread_ptr | undefined => {
        /// See library_pthread.js in Emscripten.
        /// They hang a "pthread" object from the worker if the worker is running a thread, and remove it when the thread stops by doing `pthread_exit` or when it's joined using `pthread_join`.
        const emscriptenThreadInfo = (<any>worker)["pthread"];
        if (emscriptenThreadInfo === undefined) {
            return undefined;
        }
        return emscriptenThreadInfo.threadInfoStruct;
    }
};


