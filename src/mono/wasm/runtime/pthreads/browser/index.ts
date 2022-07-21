// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../../imports";
import { pthread_ptr, MonoWorkerMessageChannelCreated, isMonoWorkerMessageChannelCreated, monoSymbol } from "../shared";
import { MonoThreadMessage } from "../shared";
import { PromiseController, createPromiseController } from "../../promise-controller";

const threads: Map<pthread_ptr, Thread> = new Map();

export interface Thread {
    readonly pthread_ptr: pthread_ptr;
    readonly worker: Worker;
    readonly port: MessagePort;
    postMessageToWorker<T extends MonoThreadMessage>(message: T): void;
}

class ThreadImpl implements Thread {
    constructor(readonly pthread_ptr: pthread_ptr, readonly worker: Worker, readonly port: MessagePort) { }
    postMessageToWorker<T extends MonoThreadMessage>(message: T): void {
        this.port.postMessage(message);
    }
}

const thread_promises: Map<pthread_ptr, PromiseController<Thread>[]> = new Map();

/// wait until the thread with the given id has set up a message port to the runtime
export function waitForThread(pthread_ptr: pthread_ptr): Promise<Thread> {
    if (threads.has(pthread_ptr)) {
        return Promise.resolve(threads.get(pthread_ptr)!);
    }
    const promiseAndController = createPromiseController<Thread>();
    const arr = thread_promises.get(pthread_ptr);
    if (arr === undefined) {
        thread_promises.set(pthread_ptr, [promiseAndController.promise_control]);
    } else {
        arr.push(promiseAndController.promise_control);
    }
    return promiseAndController.promise;
}

function resolvePromises(pthread_ptr: pthread_ptr, thread: Thread): void {
    const arr = thread_promises.get(pthread_ptr);
    if (arr !== undefined) {
        arr.forEach((controller) => controller.resolve(thread));
        thread_promises.delete(pthread_ptr);
    }
}

function addThread(pthread_ptr: pthread_ptr, worker: Worker, port: MessagePort): Thread {
    const thread = new ThreadImpl(pthread_ptr, worker, port);
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
    console.debug("MONO_WASM: got message from worker on the dedicated channel", event.data, thread);
}

// handler that runs in the main thread when a message is received from a pthread worker
function monoWorkerMessageHandler(worker: Worker, ev: MessageEvent<MonoWorkerMessageChannelCreated<MessagePort>>): void {
    /// N.B. important to ignore messages we don't recognize - Emscripten uses the message event to send internal messages
    const data = ev.data;
    if (isMonoWorkerMessageChannelCreated(data)) {
        console.debug("MONO_WASM: received the channel created message", data, worker);
        const port = data[monoSymbol].port;
        const pthread_id = data[monoSymbol].thread_id;
        const thread = addThread(pthread_id, worker, port);
        port.addEventListener("message", (ev) => monoDedicatedChannelMessageFromWorkerToMain(ev, thread));
        port.start();
        resolvePromises(pthread_id, thread);
    }
}

/// Called by Emscripten internals on the browser thread when a new pthread worker is created and added to the pthread worker pool.
/// At this point the worker doesn't have any pthread assigned to it, yet.
export function afterLoadWasmModuleToWorker(worker: Worker): void {
    worker.addEventListener("message", (ev) => monoWorkerMessageHandler(worker, ev));
    console.debug("MONO_WASM: afterLoadWasmModuleToWorker added message event handler", worker);
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


