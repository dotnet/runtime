// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import MonoWasmThreads from "consts:monoWasmThreads";
import { PThreadReplacements } from "../../types";
import { afterLoadWasmModuleToWorker } from "../browser";
import { afterThreadInitTLS } from "../worker";


/** @module emscripten-replacements Replacements for individual functions in the emscripten PThreads library.
 * These have a hard dependency on the version of Emscripten that we are using and may need to be kept in sync with
 *    {@linkcode file://./../../../emsdk/upstream/emscripten/src/library_pthread.js}
 */

export function replaceEmscriptenPThreadLibrary(replacements: PThreadReplacements): void {
    if (MonoWasmThreads) {
        // const PThread = replacements.PThread;
        const originalLoadWasmModuleToWorker = replacements.loadWasmModuleToWorker;
        replacements.loadWasmModuleToWorker = (worker: Worker, onFinishedLoading?: (worker: Worker) => void): void => {
            originalLoadWasmModuleToWorker(worker, onFinishedLoading);
            afterLoadWasmModuleToWorker(worker);
        };
        const originalThreadInitTLS = replacements.threadInitTLS;
        replacements.threadInitTLS = (): void => {
            originalThreadInitTLS();
            afterThreadInitTLS();
        };
        const originalAllocateUnusedWorker = replacements.allocateUnusedWorker;
        replacements.allocateUnusedWorker = () => {
            // TODO: replace this with our own implementation based on asset loading
            originalAllocateUnusedWorker();
        };
    }
}
