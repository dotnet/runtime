// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { isMonoWorkerMessageChannelCreated, monoSymbol, makeMonoThreadMessageApplyMonoConfig, isMonoWorkerMessagePreload, MonoWorkerMessage } from "../shared";
import { pthreadPtr } from "../shared/types";
import { MonoThreadMessage } from "../shared";
import Internals from "../shared/emscripten-internals";
import { createPromiseController, runtimeHelpers } from "../../globals";
import { PromiseController } from "../../types/internal";
import { MonoConfig } from "../../types";
import { mono_log_debug } from "../../logging";

const threads: Map<pthreadPtr, Thread> = new Map();

export interface Thread {
    readonly pthreadPtr: pthreadPtr;
    readonly worker: Worker;
    readonly port: MessagePort;
    postMessageToWorker<T extends MonoThreadMessage>(message: T): void;
}

class ThreadImpl implements Thread {
    constructor(readonly pthreadPtr: pthreadPtr, readonly worker: Worker, readonly port: MessagePort) { }
    postMessageToWorker<T extends MonoThreadMessage>(message: T): void {
        this.port.postMessage(message);
    }
}

const threadPromises: Map<pthreadPtr, PromiseController<Thread>[]> = new Map();

/// wait until the thread with the given id has set up a message port to the runtime
export function waitForThread(pthreadPtr: pthreadPtr): Promise<Thread> {
    if (threads.has(pthreadPtr)) {
        return Promise.resolve(threads.get(pthreadPtr)!);
    }
    const promiseAndController = createPromiseController<Thread>();
    const arr = threadPromises.get(pthreadPtr);
    if (arr === undefined) {
        threadPromises.set(pthreadPtr, [promiseAndController.promise_control]);
    } else {
        arr.push(promiseAndController.promise_control);
    }
    return promiseAndController.promise;
}

function resolvePromises(pthreadPtr: pthreadPtr, thread: Thread): void {
    const arr = threadPromises.get(pthreadPtr);
    if (arr !== undefined) {
        arr.forEach((controller) => controller.resolve(thread));
        threadPromises.delete(pthreadPtr);
    }
}

function addThread(pthreadPtr: pthreadPtr, worker: Worker, port: MessagePort): Thread {
    const thread = new ThreadImpl(pthreadPtr, worker, port);
    threads.set(pthreadPtr, thread);
    return thread;
}

function removeThread(pthreadPtr: pthreadPtr): void {
    threads.delete(pthreadPtr);
}

/// Given a thread id, return the thread object with the worker where the thread is running, and a message port.
export function getThread(pthreadPtr: pthreadPtr): Thread | undefined {
    const thread = threads.get(pthreadPtr);
    if (thread === undefined) {
        return undefined;
    }
    // validate that the worker is still running pthreadPtr
    const worker = thread.worker;
    if (Internals.getThreadId(worker) !== pthreadPtr) {
        removeThread(pthreadPtr);
        thread.port.close();
        return undefined;
    }
    return thread;
}

/// Returns all the threads we know about
export const getThreadIds = (): IterableIterator<pthreadPtr> => threads.keys();

function monoDedicatedChannelMessageFromWorkerToMain(event: MessageEvent<unknown>, thread: Thread): void {
    // TODO: add callbacks that will be called from here
    mono_log_debug("got message from worker on the dedicated channel", event.data, thread);
}

// handler that runs in the main thread when a message is received from a pthread worker
function monoWorkerMessageHandler(worker: Worker, ev: MessageEvent<MonoWorkerMessage<MessagePort>>): void {
    /// N.B. important to ignore messages we don't recognize - Emscripten uses the message event to send internal messages
    const data = ev.data;
    if (isMonoWorkerMessagePreload(data)) {
        const port = data[monoSymbol].port;
        port.postMessage(makeMonoThreadMessageApplyMonoConfig(runtimeHelpers.config));
    }
    else if (isMonoWorkerMessageChannelCreated(data)) {
        mono_log_debug("received the channel created message", data, worker);
        const port = data[monoSymbol].port;
        const pthreadId = data[monoSymbol].threadId;
        const thread = addThread(pthreadId, worker, port);
        port.addEventListener("message", (ev) => monoDedicatedChannelMessageFromWorkerToMain(ev, thread));
        port.start();
        resolvePromises(pthreadId, thread);
    }
}

/// Called by Emscripten internals on the browser thread when a new pthread worker is created and added to the pthread worker pool.
/// At this point the worker doesn't have any pthread assigned to it, yet.
export function afterLoadWasmModuleToWorker(worker: Worker): void {
    worker.addEventListener("message", (ev) => monoWorkerMessageHandler(worker, ev));
    mono_log_debug("afterLoadWasmModuleToWorker added message event handler", worker);
}

/// We call on the main thread this during startup to pre-allocate a pool of pthread workers.
/// At this point asset resolution needs to be working (ie we loaded MonoConfig).
/// This is used instead of the Emscripten PThread.initMainThread because we call it later.
export function preAllocatePThreadWorkerPool(defaultPthreadPoolSize: number, config: MonoConfig): void {
    const poolSizeSpec = config?.pthreadPoolSize;
    let n: number;
    if (poolSizeSpec === undefined) {
        n = defaultPthreadPoolSize;
    } else {
        mono_assert(typeof poolSizeSpec === "number", "pthreadPoolSize must be a number");
        if (poolSizeSpec < 0)
            n = defaultPthreadPoolSize;
        else
            n = poolSizeSpec;
    }
    for (let i = 0; i < n; i++) {
        Internals.allocateUnusedWorker();
    }
}

/// We call this on the main thread during startup once we fetched WasmModule.
/// This sends a message to each pre-allocated worker to load the WasmModule and dotnet.js and to set up
/// message handling.
/// This is used instead of the Emscripten "receiveInstance" in "createWasm" because that code is
/// conditioned on a non-zero PTHREAD_POOL_SIZE (but we set it to 0 to avoid early worker allocation).
export async function instantiateWasmPThreadWorkerPool(): Promise<void> {
    // this is largely copied from emscripten's "receiveInstance" in "createWasm" in "src/preamble.js"
    const workers = Internals.getUnusedWorkerPool();
    if (workers.length > 0) {
        const promises = workers.map(Internals.loadWasmModuleToWorker);
        await Promise.all(promises);
    }
}
