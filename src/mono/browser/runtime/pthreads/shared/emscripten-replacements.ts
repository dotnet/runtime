// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import MonoWasmThreads from "consts:monoWasmThreads";

import { onWorkerLoadInitiated } from "../browser";
import { afterThreadInitTLS } from "../worker";
import { Internals, PThreadLibrary, PThreadWorker } from "./emscripten-internals";
import { loaderHelpers, mono_assert } from "../../globals";
import { mono_log_warn } from "../../logging";

/** @module emscripten-replacements Replacements for individual functions in the emscripten PThreads library.
 * These have a hard dependency on the version of Emscripten that we are using and may need to be kept in sync with
 *    {@linkcode file://./../../../emsdk/upstream/emscripten/src/library_pthread.js}
 */

export function replaceEmscriptenPThreadLibrary(modulePThread: PThreadLibrary): void {
    if (!MonoWasmThreads) return;

    const originalLoadWasmModuleToWorker = modulePThread.loadWasmModuleToWorker;
    const originalThreadInitTLS = modulePThread.threadInitTLS;
    const originalReturnWorkerToPool = modulePThread.returnWorkerToPool;

    modulePThread.loadWasmModuleToWorker = (worker: Worker): Promise<Worker> => {
        const afterLoaded = originalLoadWasmModuleToWorker(worker);
        afterLoaded.then(() => {
            availableThreadCount++;
        });
        onWorkerLoadInitiated(worker, afterLoaded);
        return afterLoaded;
    };
    modulePThread.threadInitTLS = (): void => {
        originalThreadInitTLS();
        afterThreadInitTLS();
    };
    modulePThread.allocateUnusedWorker = allocateUnusedWorker;
    modulePThread.getNewWorker = () => getNewWorker(modulePThread);
    modulePThread.returnWorkerToPool = (worker: PThreadWorker) => {
        // when JS interop is installed on JSWebWorker
        // we can't reuse the worker, because user code could leave the worker JS globals in a dirty state
        if (worker.interopInstalled) {
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
}

let availableThreadCount = 0;
export function is_thread_available() {
    return availableThreadCount > 0;
}

function getNewWorker(modulePThread: PThreadLibrary): PThreadWorker {
    if (!MonoWasmThreads) return null as any;

    if (modulePThread.unusedWorkers.length == 0) {
        mono_log_warn("Failed to find unused WebWorker, this may deadlock. Please increase the pthreadPoolSize.");
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
    mono_log_warn("Failed to find loaded WebWorker, this may deadlock. Please increase the pthreadPoolSize.");
    availableThreadCount--; // negative value
    return modulePThread.unusedWorkers.pop()!;
}

/// We replace Module["PThreads"].allocateUnusedWorker with this version that knows about assets
function allocateUnusedWorker(): PThreadWorker {
    if (!MonoWasmThreads) return null as any;

    const asset = loaderHelpers.resolve_single_asset_path("js-module-threads");
    const uri = asset.resolvedUrl;
    mono_assert(uri !== undefined, "could not resolve the uri for the js-module-threads asset");
    const worker = new Worker(uri) as PThreadWorker;
    Internals.getUnusedWorkerPool().push(worker);
    worker.loaded = false;
    worker.interopInstalled = false;
    return worker;
}
