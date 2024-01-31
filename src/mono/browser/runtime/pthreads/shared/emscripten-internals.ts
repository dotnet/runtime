// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../../globals";
import { pthreadPtr } from "./types";
import WasmEnableThreads from "consts:wasmEnableThreads";

/** @module emscripten-internals accessors to the functions in the emscripten PThreads library, including
 * the low-level representations of {@linkcode pthreadPtr} thread info structs, etc.
 * Additionally, note that some of these functions are replaced by {@linkcode file://./emscripten-replacements.ts}.
 * These have a hard dependency on the version of Emscripten that we are using and may need to be kept in sync with
 *    {@linkcode file://./../../../emsdk/upstream/emscripten/src/library_pthread.js}
 */

// This is what we know about the Emscripten PThread library
export interface PThreadLibrary {
    unusedWorkers: PThreadWorker[];
    pthreads: PThreadInfoMap;
    allocateUnusedWorker: () => void;
    loadWasmModuleToWorker: (worker: Worker) => Promise<Worker>;
    threadInitTLS: () => void,
    getNewWorker: () => PThreadWorker,
    returnWorkerToPool: (worker: PThreadWorker) => void,
}


/// N.B. emscripten deletes the `pthread` property from the worker when it is not actively running a pthread
export interface PThreadWorker extends Worker {
    pthread_ptr: pthreadPtr;
    loaded: boolean;
    interopInstalled: boolean;
}

interface PThreadObject {
    worker: PThreadWorker;
}

interface PThreadInfoMap {
    [key: pthreadPtr]: PThreadObject | undefined;
}


function isRunningPThreadWorker(w: Worker): w is PThreadWorker {
    return (<any>w).pthread !== undefined;
}

/// These utility functions dig into Emscripten internals
export const Internals = !WasmEnableThreads ? null as any : {
    get modulePThread(): PThreadLibrary {
        return (<any>Module).PThread as PThreadLibrary;
    },
    getWorker: (pthreadPtr: pthreadPtr): PThreadWorker | undefined => {
        return Internals.modulePThread.pthreads[pthreadPtr];
    },
    getThreadId: (worker: Worker): pthreadPtr | undefined => {
        /// See library_pthread.js in Emscripten.
        /// They hang a "pthread" object from the worker if the worker is running a thread, and remove it when the thread stops by doing `pthread_exit` or when it's joined using `pthread_join`.
        if (!isRunningPThreadWorker(worker))
            return undefined;
        return worker.pthread_ptr;
    },
    allocateUnusedWorker: (): void => {
        /// See library_pthread.js in Emscripten.
        /// This function allocates a new worker and adds it to the pool of workers.
        /// It's called when the pool of workers is empty and a new thread is created.
        Internals.modulePThread.allocateUnusedWorker();
    },
    getUnusedWorkerPool: (): Worker[] => {
        return Internals.modulePThread.unusedWorkers;
    },
    loadWasmModuleToWorker: (worker: Worker): Promise<Worker> => {
        return Internals.modulePThread.loadWasmModuleToWorker(worker);
    }
};
