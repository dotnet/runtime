// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../../globals";
import { Thread } from "../browser";
import { PThreadInfo, PThreadPtr } from "./types";

/** @module emscripten-internals accessors to the functions in the emscripten PThreads library, including
 * the low-level representations of {@linkcode PThreadPtr} thread info structs, etc.
 * Additionally, note that some of these functions are replaced by {@linkcode file://./emscripten-replacements.ts}.
 * These have a hard dependency on the version of Emscripten that we are using and may need to be kept in sync with
 *    {@linkcode file://./../../../emsdk/upstream/emscripten/src/library_pthread.js}
 */

// This is what we know about the Emscripten PThread library
export interface PThreadLibrary {
    unusedWorkers: PThreadWorker[];
    runningWorkers: PThreadWorker[];
    pthreads: PThreadInfoMap;
    allocateUnusedWorker: () => void;
    loadWasmModuleToWorker: (worker: PThreadWorker) => Promise<PThreadWorker>;
    threadInitTLS: () => void,
    getNewWorker: () => PThreadWorker,
    returnWorkerToPool: (worker: PThreadWorker) => void,
}


/// N.B. emscripten deletes the `pthread` property from the worker when it is not actively running a pthread
export interface PThreadWorker extends Worker {
    pthread_ptr: PThreadPtr;
    loaded: boolean;
    // this info is updated via async messages from the worker, it could be stale
    info: PThreadInfo;
    thread?: Thread;
}

interface PThreadInfoMap {
    [key: number]: PThreadWorker;
}


export function getWorker(pthreadPtr: PThreadPtr): PThreadWorker | undefined {
    return getModulePThread().pthreads[pthreadPtr as any];
}

export function allocateUnusedWorker(): void {
    /// See library_pthread.js in Emscripten.
    /// This function allocates a new worker and adds it to the pool of workers.
    /// It's called when the pool of workers is empty and a new thread is created.
    getModulePThread().allocateUnusedWorker();
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
