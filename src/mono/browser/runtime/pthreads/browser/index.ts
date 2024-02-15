// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { MonoWorkerToMainMessage, PThreadInfo, PThreadPtr, PThreadPtrNull } from "../shared/types";
import { MonoThreadMessage, mono_wasm_pthread_ptr, update_thread_info } from "../shared";
import { PThreadWorker, allocateUnusedWorker, getRunningWorkers, getUnusedWorkerPool, getWorker, loadWasmModuleToWorker } from "../shared/emscripten-internals";
import { createPromiseController, mono_assert, runtimeHelpers } from "../../globals";
import { MainToWorkerMessageType, PromiseAndController, PromiseController, WorkerToMainMessageType, monoMessageSymbol } from "../../types/internal";
import { mono_log_info } from "../../logging";
import { monoThreadInfo } from "../worker";
import { mono_wasm_init_diagnostics } from "../../diagnostics";

const threadPromises: Map<PThreadPtr, PromiseController<Thread>[]> = new Map();

export interface Thread {
    readonly pthreadPtr: PThreadPtr;
    readonly port: MessagePort;
    postMessageToWorker<T extends MonoThreadMessage>(message: T): void;
}

class ThreadImpl implements Thread {
    constructor(readonly pthreadPtr: PThreadPtr, readonly worker: Worker, readonly port: MessagePort) { }
    postMessageToWorker<T extends MonoThreadMessage>(message: T): void {
        this.port.postMessage(message);
    }
}

/// wait until the thread with the given id has set up a message port to the runtime
export function waitForThread(pthreadPtr: PThreadPtr): Promise<Thread> {
    if (!WasmEnableThreads) return null as any;
    const worker = getWorker(pthreadPtr);
    if (worker?.thread) {
        return Promise.resolve(worker?.thread);
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

export function resolveThreadPromises(pthreadPtr: PThreadPtr, thread?: Thread): void {
    if (!WasmEnableThreads) return;
    const arr = threadPromises.get(pthreadPtr);
    if (arr !== undefined) {
        arr.forEach((controller) => {
            if (thread) {
                controller.resolve(thread);
            } else {
                controller.reject();
            }
        });
        threadPromises.delete(pthreadPtr);
    }
}

// handler that runs in the main thread when a message is received from a pthread worker
function monoWorkerMessageHandler(worker: PThreadWorker, ev: MessageEvent<any>): void {
    if (!WasmEnableThreads) return;
    let pthreadId: PThreadPtr;
    // this is emscripten message
    if (ev.data.cmd === "killThread") {
        pthreadId = ev.data["thread"];
        mono_assert(pthreadId == worker.info.pthreadId, "expected pthreadId to match");
        worker.info.isRunning = false;
        worker.info.pthreadId = PThreadPtrNull;
        return;
    }

    const message = ev.data[monoMessageSymbol] as MonoWorkerToMainMessage;
    if (message === undefined) {
        /// N.B. important to ignore messages we don't recognize - Emscripten uses the message event to send internal messages
        return;
    }

    let port: MessagePort;
    let thread: Thread;
    pthreadId = message.info?.pthreadId ?? 0;

    worker.info = Object.assign(worker.info, message.info, {});
    switch (message.monoCmd) {
        case WorkerToMainMessageType.preload:
            // this one shot port from setupPreloadChannelToMainThread
            port = message.port!;
            port.postMessage({
                type: "pthread",
                cmd: MainToWorkerMessageType.applyConfig,
                config: JSON.stringify(runtimeHelpers.config)
            });
            port.close();
            break;
        case WorkerToMainMessageType.pthreadCreated:
            port = message.port!;
            thread = new ThreadImpl(pthreadId, worker, port);
            worker.thread = thread;
            worker.info.isRunning = true;
            resolveThreadPromises(pthreadId, thread);
            break;
        case WorkerToMainMessageType.monoRegistered:
        case WorkerToMainMessageType.monoAttached:
        case WorkerToMainMessageType.enabledInterop:
        case WorkerToMainMessageType.monoUnRegistered:
        case WorkerToMainMessageType.updateInfo:
            // just worker.info updates above
            break;
        default:
            throw new Error(`Unhandled message from worker: ${message.monoCmd}`);
    }
}

let pendingWorkerLoad: PromiseAndController<void> | undefined;

/// Called by Emscripten internals on the browser thread when a new pthread worker is created and added to the pthread worker pool.
/// At this point the worker doesn't have any pthread assigned to it, yet.
export function onWorkerLoadInitiated(worker: PThreadWorker, loaded: Promise<Worker>): void {
    if (!WasmEnableThreads) return;
    worker.addEventListener("message", (ev) => monoWorkerMessageHandler(worker, ev));
    if (pendingWorkerLoad == undefined) {
        pendingWorkerLoad = createPromiseController<void>();
    }
    loaded.then(() => {
        worker.info.isLoaded = true;
        if (pendingWorkerLoad != undefined) {
            pendingWorkerLoad.promise_control.resolve();
            pendingWorkerLoad = undefined;
        }
    });
}

export function thread_available(): Promise<void> {
    if (!WasmEnableThreads) return null as any;
    if (pendingWorkerLoad == undefined) {
        return Promise.resolve();
    }
    return pendingWorkerLoad.promise;
}

export async function mono_wasm_init_threads() {
    if (!WasmEnableThreads) return;
    monoThreadInfo.pthreadId = mono_wasm_pthread_ptr();
    monoThreadInfo.threadName = "UI Thread";
    monoThreadInfo.isUI = true;
    monoThreadInfo.isRunning = true;
    update_thread_info();
    await instantiateWasmPThreadWorkerPool();
    await mono_wasm_init_diagnostics();
}

/// We call on the main thread this during startup to pre-allocate a pool of pthread workers.
/// At this point asset resolution needs to be working (ie we loaded MonoConfig).
/// This is used instead of the Emscripten PThread.initMainThread because we call it later.
export function preAllocatePThreadWorkerPool(pthreadPoolSize: number): void {
    if (!WasmEnableThreads) return;
    for (let i = 0; i < pthreadPoolSize; i++) {
        allocateUnusedWorker();
    }
}

/// We call this on the main thread during startup once we fetched WasmModule.
/// This sends a message to each pre-allocated worker to load the WasmModule and dotnet.js and to set up
/// message handling.
/// This is used instead of the Emscripten "receiveInstance" in "createWasm" because that code is
/// conditioned on a non-zero PTHREAD_POOL_SIZE (but we set it to 0 to avoid early worker allocation).
export async function instantiateWasmPThreadWorkerPool(): Promise<void> {
    if (!WasmEnableThreads) return null as any;
    // this is largely copied from emscripten's "receiveInstance" in "createWasm" in "src/preamble.js"
    const workers = getUnusedWorkerPool();
    if (workers.length > 0) {
        const promises = workers.map(loadWasmModuleToWorker);
        await Promise.all(promises);
    }
}

// when we create threads with browser event loop, it's not able to be joined by mono's thread join during shutdown and blocks process exit
export function cancelThreads() {
    const workers: PThreadWorker[] = getRunningWorkers();
    for (const worker of workers) {
        if (worker.info.isExternalEventLoop) {
            worker.postMessage({ cmd: "cancel" });
        }
    }
}

export function dumpThreads(): void {
    if (!WasmEnableThreads) return;
    mono_log_info("Dumping web worker info as seen by UI thread, it could be stale: ");
    const emptyInfo = {
        pthreadId: 0,
        threadPrefix: "          -    ",
        threadName: "????",
        isRunning: false,
        isAttached: false,
        isExternalEventLoop: false,
        reuseCount: 0,
    };
    const threadInfos: PThreadInfo[] = [
        Object.assign({}, emptyInfo, monoThreadInfo), // UI thread
    ];
    for (const worker of getRunningWorkers()) {
        threadInfos.push(Object.assign({}, emptyInfo, worker.info));
    }
    for (const worker of getUnusedWorkerPool()) {
        threadInfos.push(Object.assign({}, emptyInfo, worker.info));
    }
    threadInfos.forEach((info, i) => {
        const idx = (i + "").padStart(2, "0");
        const isRunning = (info.isRunning + "").padStart(5, " ");
        const isAttached = (info.isAttached + "").padStart(5, " ");
        const isEventLoop = (info.isExternalEventLoop + "").padStart(5, " ");
        const reuseCount = (info.reuseCount + "").padStart(3, " ");
        // eslint-disable-next-line no-console
        console.info(`${idx} | ${info.threadPrefix}: isRunning:${isRunning} isAttached:${isAttached} isEventLoop:${isEventLoop} reuseCount:${reuseCount} - ${info.threadName}`);
    });
}
