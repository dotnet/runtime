// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import { } from "../globals";
import { mono_log_warn } from "../logging";
import { MonoWorkerToMainMessage, monoThreadInfo, mono_wasm_pthread_ptr, update_thread_info, worker_empty_prefix } from "./shared";
import { Module, ENVIRONMENT_IS_WORKER, createPromiseController, loaderHelpers, mono_assert, runtimeHelpers } from "../globals";
import { PThreadLibrary, MainToWorkerMessageType, MonoThreadMessage, PThreadInfo, PThreadPtr, PThreadPtrNull, PThreadWorker, PromiseAndController, PromiseController, Thread, WorkerToMainMessageType, monoMessageSymbol } from "../types/internal";
import { mono_log_error, mono_log_info } from "../logging";
import { threads_c_functions as cwraps } from "../cwraps";

const threadPromises: Map<PThreadPtr, PromiseController<Thread>[]> = new Map();

class ThreadImpl implements Thread {
    constructor(readonly pthreadPtr: PThreadPtr, readonly worker: Worker, readonly port: MessagePort) { }
    postMessageToWorker<T extends MonoThreadMessage>(message: T): void {
        this.port.postMessage(message);
    }
}

/// wait until the thread with the given id has set up a message port to the runtime
export function waitForThread(pthreadPtr: PThreadPtr): Promise<Thread> {
    if (!WasmEnableThreads) return null as any;
    mono_assert(!ENVIRONMENT_IS_WORKER, "waitForThread should only be called from the UI thread");
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
    worker.info = Object.assign({}, worker.info, message.info);
    switch (message.monoCmd) {
        case WorkerToMainMessageType.preload:
            // this one shot port from setupPreloadChannelToMainThread
            port = message.port!;
            port.postMessage({
                type: "pthread",
                cmd: MainToWorkerMessageType.applyConfig,
                config: JSON.stringify(runtimeHelpers.config),
                monoThreadInfo: JSON.stringify(worker.info),
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

export function populateEmscriptenPool(): void {
    if (!WasmEnableThreads) return;
    const unused = getUnusedWorkerPool();
    for (const worker of loaderHelpers.loadingWorkers) {
        unused.push(worker);
    }
    loaderHelpers.loadingWorkers = [];
}

export async function mono_wasm_init_threads() {
    if (!WasmEnableThreads) return;

    // setup the UI thread
    monoThreadInfo.pthreadId = mono_wasm_pthread_ptr();
    monoThreadInfo.threadName = "UI Thread";
    monoThreadInfo.isUI = true;
    monoThreadInfo.isRunning = true;
    update_thread_info();

    // wait until all workers in the pool are loaded - ready to be used as pthread synchronously
    const workers = getUnusedWorkerPool();
    if (workers.length > 0) {
        const promises = workers.map(loadWasmModuleToWorker);
        await Promise.all(promises);
    }
}

// when we create threads with browser event loop, it's not able to be joined by mono's thread join during shutdown and blocks process exit
export function cancelThreads() {
    if (!WasmEnableThreads) return;
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
    const emptyInfo: PThreadInfo = {
        workerNumber: -1,
        pthreadId: PThreadPtrNull,
        threadPrefix: worker_empty_prefix,
        threadName: "????",
        isRunning: false,
        isAttached: false,
        isExternalEventLoop: false,
        reuseCount: 0,
        updateCount: 0,
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
    threadInfos.forEach((info) => {
        const idx = info.workerNumber.toString().padStart(3, "0");
        const isRunning = (info.isRunning + "").padStart(5, " ");
        const isAttached = (info.isAttached + "").padStart(5, " ");
        const isEventLoop = (info.isExternalEventLoop + "").padStart(5, " ");
        const reuseCount = (info.reuseCount + "").padStart(3, " ");
        // eslint-disable-next-line no-console
        console.info(`${idx} | ${info.threadPrefix}: isRunning:${isRunning} isAttached:${isAttached} isEventLoop:${isEventLoop} reuseCount:${reuseCount} - ${info.threadName}`);
    });
}

export function init_finalizer_thread() {
    // we don't need it immediately, so we can wait a bit, to keep CPU working on normal startup
    setTimeout(() => {
        try {
            cwraps.mono_wasm_init_finalizer_thread();
        }
        catch (err) {
            mono_log_error("init_finalizer_thread() failed", err);
            loaderHelpers.mono_exit(1, err);
        }
    }, loaderHelpers.config.finalizerThreadStartDelayMs);
}

export function replaceEmscriptenPThreadUI(modulePThread: PThreadLibrary): void {
    if (!WasmEnableThreads) return;

    const originalLoadWasmModuleToWorker = modulePThread.loadWasmModuleToWorker;
    const originalReturnWorkerToPool = modulePThread.returnWorkerToPool;

    modulePThread.loadWasmModuleToWorker = (worker: PThreadWorker): Promise<PThreadWorker> => {
        const afterLoaded = originalLoadWasmModuleToWorker(worker);
        afterLoaded.then(() => {
            availableThreadCount++;
        });
        onWorkerLoadInitiated(worker, afterLoaded);
        if (loaderHelpers.config.exitOnUnhandledError) {
            worker.onerror = (e) => {
                loaderHelpers.mono_exit(1, e);
            };
        }
        return afterLoaded;
    };
    modulePThread.allocateUnusedWorker = allocateUnusedWorker;
    modulePThread.getNewWorker = () => getNewWorker(modulePThread);
    modulePThread.returnWorkerToPool = (worker: PThreadWorker) => {
        // when JS interop is installed on JSWebWorker
        // we can't reuse the worker, because user code could leave the worker JS globals in a dirty state
        worker.info.isRunning = false;
        resolveThreadPromises(worker.pthread_ptr, undefined);
        worker.info.pthreadId = PThreadPtrNull;
        if (worker.thread?.port) {
            worker.thread.port.close();
        }
        worker.thread = undefined;
        if (worker.info && worker.info.isDirtyBecauseOfInterop) {
            // we are on UI thread, invoke the handler directly to destroy the dirty worker
            worker.onmessage!(new MessageEvent("message", {
                data: {
                    "cmd": "killThread",
                    thread: worker.pthread_ptr
                }
            }));
        } else {
            availableThreadCount++;
            originalReturnWorkerToPool(worker);
        }
    };
    if (BuildConfiguration === "Debug") {
        (globalThis as any).dumpThreads = dumpThreads;
        (globalThis as any).getModulePThread = getModulePThread;
    }
}

let availableThreadCount = 0;
export function is_thread_available() {
    if (!WasmEnableThreads) return true;
    return availableThreadCount > 0;
}

function getNewWorker(modulePThread: PThreadLibrary): PThreadWorker {
    if (!WasmEnableThreads) return null as any;

    if (modulePThread.unusedWorkers.length == 0) {
        mono_log_warn(`Failed to find unused WebWorker, this may deadlock. Please increase the pthreadPoolReady. Running threads ${modulePThread.runningWorkers.length}. Loading workers: ${modulePThread.unusedWorkers.length}`);
        const worker = allocateUnusedWorker();
        modulePThread.loadWasmModuleToWorker(worker);
        availableThreadCount--;
        return worker;
    }

    // keep them pre-allocated all the time, not just during startup
    if (modulePThread.unusedWorkers.length <= loaderHelpers.config.pthreadPoolUnusedSize!) {
        const worker = allocateUnusedWorker();
        modulePThread.loadWasmModuleToWorker(worker);
    }

    for (let i = 0; i < modulePThread.unusedWorkers.length; i++) {
        const worker = modulePThread.unusedWorkers[i];
        if (worker.loaded) {
            modulePThread.unusedWorkers.splice(i, 1);
            availableThreadCount--;
            return worker;
        }
    }
    mono_log_warn(`Failed to find loaded WebWorker, this may deadlock. Please increase the pthreadPoolReady. Running threads ${modulePThread.runningWorkers.length}. Loading workers: ${modulePThread.unusedWorkers.length}`);
    availableThreadCount--; // negative value
    return modulePThread.unusedWorkers.pop()!;
}

/// We replace Module["PThreads"].allocateUnusedWorker with this version that knows about assets
function allocateUnusedWorker(): PThreadWorker {
    if (!WasmEnableThreads) return null as any;

    const asset = loaderHelpers.resolve_single_asset_path("js-module-threads");
    const uri = asset.resolvedUrl;
    mono_assert(uri !== undefined, "could not resolve the uri for the js-module-threads asset");
    const workerNumber = loaderHelpers.workerNextNumber++;
    const worker = new Worker(uri, {
        name: "dotnet-worker-" + workerNumber.toString().padStart(3, "0"),
    }) as PThreadWorker;
    getUnusedWorkerPool().push(worker);
    worker.loaded = false;
    worker.info = {
        workerNumber,
        pthreadId: PThreadPtrNull,
        reuseCount: 0,
        updateCount: 0,
        threadPrefix: worker_empty_prefix,
        threadName: "emscripten-pool",
    };
    return worker;
}

export function getWorker(pthreadPtr: PThreadPtr): PThreadWorker | undefined {
    return getModulePThread().pthreads[pthreadPtr as any];
}

export function getUnusedWorkerPool(): PThreadWorker[] {
    return getModulePThread().unusedWorkers;
}

export function getRunningWorkers(): PThreadWorker[] {
    return getModulePThread().runningWorkers;
}

export function loadWasmModuleToWorker(worker: PThreadWorker): Promise<PThreadWorker> {
    return getModulePThread().loadWasmModuleToWorker(worker);
}

export function getModulePThread(): PThreadLibrary {
    return (<any>Module).PThread as PThreadLibrary;
}
