// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import WasmEnableThreads from "consts:wasmEnableThreads";

import { ENVIRONMENT_IS_PTHREAD, Module, loaderHelpers, mono_assert, runtimeHelpers } from "../globals";
import { PThreadSelf, monoThreadInfo, mono_wasm_pthread_ptr, postMessageToMain, update_thread_info } from "./shared";
import { PThreadLibrary, MonoThreadMessage, PThreadInfo, PThreadPtr, WorkerToMainMessageType } from "../types/internal";
import {
    makeWorkerThreadEvent,
    dotnetPthreadCreated,
    dotnetPthreadAttached,
    WorkerThreadEventTarget
} from "./worker-events";
import { postRunWorker, preRunWorker } from "../startup";
import { mono_log_debug, mono_log_error } from "../logging";
import { CharPtr } from "../types/emscripten";
import { utf8ToString } from "../strings";
import { forceThreadMemoryViewRefresh } from "../memory";

// re-export some of the events types
export {
    WorkerThreadEventMap,
    dotnetPthreadAttached,
    dotnetPthreadCreated,
    WorkerThreadEvent,
    WorkerThreadEventTarget,
} from "./worker-events";

export let pthread_self: PThreadSelf = null as any as PThreadSelf;

class WorkerSelf implements PThreadSelf {
    constructor(public info: PThreadInfo, public portToBrowser: MessagePort) {
    }

    postMessageToBrowser(message: MonoThreadMessage, transfer?: Transferable[]) {
        if (transfer) {
            this.portToBrowser.postMessage(message, transfer);
        } else {
            this.portToBrowser.postMessage(message);
        }
    }
    addEventListenerFromBrowser(listener: (event: MessageEvent<MonoThreadMessage>) => void) {
        this.portToBrowser.addEventListener("message", listener);
    }
}

/// This is the "public internal" API for runtime subsystems that wish to be notified about
/// pthreads that are running on the current worker.
/// Example:
///    currentWorkerThreadEvents.addEventListener(dotnetPthreadCreated, (ev: WorkerThreadEvent) => {
///       mono_trace("thread created on worker with id", ev.pthread_ptr);
///    });
export let currentWorkerThreadEvents: WorkerThreadEventTarget = undefined as any;

// this is very very early in the worker startup
export function initWorkerThreadEvents() {
    // treeshake if threads are disabled
    currentWorkerThreadEvents = WasmEnableThreads ? new globalThis.EventTarget() : null as any as WorkerThreadEventTarget;
    Object.assign(monoThreadInfo, runtimeHelpers.monoThreadInfo);
}

// this is the message handler for the worker that receives messages from the main thread
// extend this with new cases as needed
function monoDedicatedChannelMessageFromMainToWorker(event: MessageEvent<string>): void {
    mono_log_debug("got message from main on the dedicated channel", event.data);
}

export function on_emscripten_thread_init(pthread_ptr: PThreadPtr) {
    runtimeHelpers.currentThreadTID = monoThreadInfo.pthreadId = pthread_ptr;
    forceThreadMemoryViewRefresh();
}

/// Called by emscripten when a pthread is setup to run on a worker.  Can be called multiple times
/// for the same webworker, since emscripten can reuse workers.
/// This is an implementation detail, that shouldn't be used directly.
export function mono_wasm_pthread_on_pthread_created(): void {
    if (!WasmEnableThreads) return;
    try {
        forceThreadMemoryViewRefresh();
        const pthread_id = mono_wasm_pthread_ptr();
        mono_assert(pthread_id == monoThreadInfo.pthreadId, `needs to match (mono_wasm_pthread_ptr ${pthread_id}, threadId from thread info ${monoThreadInfo.pthreadId})`);

        monoThreadInfo.reuseCount++;
        monoThreadInfo.updateCount++;
        monoThreadInfo.threadName = "pthread-assigned";
        update_thread_info();

        // don't do this callback for the main thread
        if (!ENVIRONMENT_IS_PTHREAD) return;

        currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadCreated, pthread_self));

        const channel = new MessageChannel();
        const workerPort = channel.port1;
        const mainPort = channel.port2;
        workerPort.addEventListener("message", monoDedicatedChannelMessageFromMainToWorker);
        workerPort.start();

        // this could be replacement
        if (pthread_self && pthread_self.portToBrowser) {
            pthread_self.portToBrowser.close();
        }

        pthread_self = new WorkerSelf(monoThreadInfo, workerPort);
        postMessageToMain({
            monoCmd: WorkerToMainMessageType.pthreadCreated,
            info: monoThreadInfo,
            port: mainPort,
        }, [mainPort]);
    }
    catch (err) {
        mono_log_error("mono_wasm_pthread_on_pthread_created () failed", err);
        loaderHelpers.mono_exit(1, err);
        throw err;
    }
}

/// Called in the worker thread (not main thread) from mono when a pthread becomes registered to the mono runtime.
export function mono_wasm_pthread_on_pthread_registered(pthread_id: PThreadPtr): void {
    if (!WasmEnableThreads) return;
    try {
        mono_assert(monoThreadInfo !== null && monoThreadInfo.pthreadId == pthread_id, "expected monoThreadInfo to be set already when registering");
        monoThreadInfo.isRegistered = true;
        update_thread_info();
        postMessageToMain({
            monoCmd: WorkerToMainMessageType.monoRegistered,
            info: monoThreadInfo,
        });
        preRunWorker();
    }
    catch (err) {
        mono_log_error("mono_wasm_pthread_on_pthread_registered () failed", err);
        loaderHelpers.mono_exit(1, err);
        throw err;
    }
}

/// Called in the worker thread (not main thread) from mono when a pthread becomes attached to the mono runtime.
export function mono_wasm_pthread_on_pthread_attached(pthread_id: PThreadPtr, thread_name: CharPtr, background_thread: number, threadpool_thread: number, external_eventloop: number, debugger_thread: number): void {
    if (!WasmEnableThreads) return;
    try {
        mono_assert(monoThreadInfo !== null && monoThreadInfo.pthreadId == pthread_id, "expected monoThreadInfo to be set already when attaching");

        const name = monoThreadInfo.threadName = utf8ToString(thread_name);
        monoThreadInfo.isAttached = true;
        monoThreadInfo.isThreadPoolWorker = threadpool_thread !== 0;
        monoThreadInfo.isExternalEventLoop = external_eventloop !== 0;
        monoThreadInfo.isBackground = background_thread !== 0;
        monoThreadInfo.isDebugger = debugger_thread !== 0;

        // FIXME: this is a hack to get constant length thread names
        monoThreadInfo.threadName = name;
        monoThreadInfo.isTimer = name == ".NET Timer";
        monoThreadInfo.isLongRunning = name == ".NET Long Running Task";
        monoThreadInfo.isThreadPoolGate = name == ".NET TP Gate";
        update_thread_info();
        currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadAttached, pthread_self));
        postMessageToMain({
            monoCmd: WorkerToMainMessageType.monoAttached,
            info: monoThreadInfo,
        });
    }
    catch (err) {
        mono_log_error("mono_wasm_pthread_on_pthread_attached () failed", err);
        loaderHelpers.mono_exit(1, err);
        throw err;
    }
}

export function mono_wasm_pthread_set_name(name: CharPtr): void {
    if (!WasmEnableThreads) return;
    if (!ENVIRONMENT_IS_PTHREAD) return;
    monoThreadInfo.threadName = utf8ToString(name);
    update_thread_info();
    postMessageToMain({
        monoCmd: WorkerToMainMessageType.updateInfo,
        info: monoThreadInfo,
    });
}

/// Called in the worker thread (not main thread) from mono when a pthread becomes detached from the mono runtime.
export function mono_wasm_pthread_on_pthread_unregistered(pthread_id: PThreadPtr): void {
    if (!WasmEnableThreads) return;
    try {
        mono_assert(pthread_id === monoThreadInfo.pthreadId, "expected pthread_id to match when un-registering");
        postRunWorker();
        monoThreadInfo.isAttached = false;
        monoThreadInfo.isRegistered = false;
        monoThreadInfo.threadName = "unregistered:(" + monoThreadInfo.threadName + ")";
        update_thread_info();
        postMessageToMain({
            monoCmd: WorkerToMainMessageType.monoUnRegistered,
            info: monoThreadInfo,
        });
    }
    catch (err) {
        mono_log_error("mono_wasm_pthread_on_pthread_unregistered () failed", err);
        loaderHelpers.mono_exit(1, err);
        throw err;
    }
}

export function replaceEmscriptenPThreadWorker(modulePThread: PThreadLibrary): void {
    if (!WasmEnableThreads) return;

    const originalThreadInitTLS = modulePThread.threadInitTLS;

    modulePThread.threadInitTLS = (): void => {
        originalThreadInitTLS();
        mono_wasm_pthread_on_pthread_created();
    };
}

export function replaceEmscriptenPThreadWorker2(): void {
    const original_emscripten_thread_init = Module["__emscripten_thread_init"];
    function emscripten_thread_init_wrapper(pthread_ptr: PThreadPtr, isMainBrowserThread: number, isMainRuntimeThread: number, canBlock: number) {
        on_emscripten_thread_init(pthread_ptr);
        original_emscripten_thread_init(pthread_ptr, isMainBrowserThread, isMainRuntimeThread, canBlock);
        // re-install self
        Module["__emscripten_thread_init"] = emscripten_thread_init_wrapper;
    }
    Module["__emscripten_thread_init"] = emscripten_thread_init_wrapper;
}
