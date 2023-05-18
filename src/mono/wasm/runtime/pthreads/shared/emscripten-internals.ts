// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../../globals";
import { pthread_ptr } from "./types";

/** @module emscripten-internals accessors to the functions in the emscripten PThreads library, including
 * the low-level representations of {@linkcode pthread_ptr} thread info structs, etc.
 * Additionally, note that some of these functions are replaced by {@linkcode file://./emscripten-replacements.ts}.
 * These have a hard dependency on the version of Emscripten that we are using and may need to be kept in sync with
 *    {@linkcode file://./../../../emsdk/upstream/emscripten/src/library_pthread.js}
 */

// This is what we know about the Emscripten PThread library
interface PThreadLibrary {
    unusedWorkers: Worker[];
    pthreads: PThreadInfoMap;
    allocateUnusedWorker: () => void;
    loadWasmModuleToWorker: (worker: Worker) => Promise<Worker>;
}

interface EmscriptenPThreadInfo {
    threadInfoStruct: pthread_ptr;
}

/// N.B. emscripten deletes the `pthread` property from the worker when it is not actively running a pthread
interface PThreadWorker extends Worker {
    pthread: EmscriptenPThreadInfo;
}

interface PThreadObject {
    worker: PThreadWorker;
}

interface PThreadInfoMap {
    [key: pthread_ptr]: PThreadObject | undefined;
}


function isRunningPThreadWorker(w: Worker): w is PThreadWorker {
    return (<any>w).pthread !== undefined;
}

/// These utility functions dig into Emscripten internals
const Internals = {
    get modulePThread(): PThreadLibrary {
        return (<any>Module).PThread as PThreadLibrary;
    },
    getWorker: (pthread_ptr: pthread_ptr): PThreadWorker | undefined => {
        // see https://github.com/emscripten-core/emscripten/pull/16239
        return Internals.modulePThread.pthreads[pthread_ptr]?.worker;
    },
    getThreadId: (worker: Worker): pthread_ptr | undefined => {
        /// See library_pthread.js in Emscripten.
        /// They hang a "pthread" object from the worker if the worker is running a thread, and remove it when the thread stops by doing `pthread_exit` or when it's joined using `pthread_join`.
        if (!isRunningPThreadWorker(worker))
            return undefined;
        const emscriptenThreadInfo = worker.pthread;
        return emscriptenThreadInfo.threadInfoStruct;
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


export default Internals;
