// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import { dumpThreads, onWorkerLoadInitiated, resolveThreadPromises } from "../browser";
import { mono_wasm_pthread_on_pthread_created, onRunMessage } from "../worker";
import { PThreadLibrary, PThreadWorker, getModulePThread, getUnusedWorkerPool } from "./emscripten-internals";
import { Module, loaderHelpers, mono_assert } from "../../globals";
import { mono_log_warn } from "../../logging";
import { PThreadPtr, PThreadPtrNull } from "./types";

/** @module emscripten-replacements Replacements for individual functions in the emscripten PThreads library.
 * These have a hard dependency on the version of Emscripten that we are using and may need to be kept in sync with
 *    {@linkcode file://./../../../emsdk/upstream/emscripten/src/library_pthread.js}
 */

export function replaceEmscriptenPThreadLibrary(modulePThread: PThreadLibrary): void {
    if (!WasmEnableThreads) return;

    const originalLoadWasmModuleToWorker = modulePThread.loadWasmModuleToWorker;
    const originalThreadInitTLS = modulePThread.threadInitTLS;
    const originalReturnWorkerToPool = modulePThread.returnWorkerToPool;
    const original_emscripten_thread_init = (Module as any)["__emscripten_thread_init"];


    (Module as any)["__emscripten_thread_init"] = (pthread_ptr: PThreadPtr, isMainBrowserThread: number, isMainRuntimeThread: number, canBlock: number) => {
        onRunMessage(pthread_ptr);
        original_emscripten_thread_init(pthread_ptr, isMainBrowserThread, isMainRuntimeThread, canBlock);
    };

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
    modulePThread.threadInitTLS = (): void => {
        originalThreadInitTLS();
        mono_wasm_pthread_on_pthread_created();
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
    return availableThreadCount > 0;
}

function getNewWorker(modulePThread: PThreadLibrary): PThreadWorker {
    if (!WasmEnableThreads) return null as any;

    if (modulePThread.unusedWorkers.length == 0) {
        mono_log_warn(`Failed to find unused WebWorker, this may deadlock. Please increase the pthreadPoolSize. Running threads ${modulePThread.runningWorkers.length}. Loading workers: ${modulePThread.unusedWorkers.length}`);
        const worker = allocateUnusedWorker();
        modulePThread.loadWasmModuleToWorker(worker);
        availableThreadCount--;
        return worker;
    }

    // keep them pre-allocated all the time, not just during startup
    if (loaderHelpers.config.pthreadPoolSize && modulePThread.unusedWorkers.length <= loaderHelpers.config.pthreadPoolSize) {
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
    mono_log_warn(`Failed to find loaded WebWorker, this may deadlock. Please increase the pthreadPoolSize. Running threads ${modulePThread.runningWorkers.length}. Loading workers: ${modulePThread.unusedWorkers.length}`);
    availableThreadCount--; // negative value
    return modulePThread.unusedWorkers.pop()!;
}

/// We replace Module["PThreads"].allocateUnusedWorker with this version that knows about assets
function allocateUnusedWorker(): PThreadWorker {
    if (!WasmEnableThreads) return null as any;

    const asset = loaderHelpers.resolve_single_asset_path("js-module-threads");
    const uri = asset.resolvedUrl;
    mono_assert(uri !== undefined, "could not resolve the uri for the js-module-threads asset");
    const worker = new Worker(uri) as PThreadWorker;
    getUnusedWorkerPool().push(worker);
    worker.loaded = false;
    worker.info = {
        pthreadId: PThreadPtrNull,
        reuseCount: 0,
        updateCount: 0,
        threadPrefix: "          -    ",
        threadName: "emscripten-pool",
    };
    return worker;
}


