// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import MonoWasmThreads from "consts:monoWasmThreads";
import { PThreadReplacements } from "../../types";
import { afterLoadWasmModuleToWorker } from "../browser";
import { afterThreadInitTLS } from "../worker";
import Internals from "./emscripten-internals";
import { resolve_asset_path } from "../../assets";
import { mono_assert } from "../../types";
import { runtimeHelpers } from "../../imports";

/** @module emscripten-replacements Replacements for individual functions in the emscripten PThreads library.
 * These have a hard dependency on the version of Emscripten that we are using and may need to be kept in sync with
 *    {@linkcode file://./../../../emsdk/upstream/emscripten/src/library_pthread.js}
 */

export function replaceEmscriptenPThreadLibrary(replacements: PThreadReplacements): void {
    if (MonoWasmThreads) {
        const originalLoadWasmModuleToWorker = replacements.loadWasmModuleToWorker;
        replacements.loadWasmModuleToWorker = (worker: Worker): Promise<Worker> => {
            const p = originalLoadWasmModuleToWorker(worker);
            afterLoadWasmModuleToWorker(worker);
            return p;
        };
        const originalThreadInitTLS = replacements.threadInitTLS;
        replacements.threadInitTLS = (): void => {
            originalThreadInitTLS();
            afterThreadInitTLS();
        };
        // const originalAllocateUnusedWorker = replacements.allocateUnusedWorker;
        replacements.allocateUnusedWorker = replacementAllocateUnusedWorker;
    }
}

/// We replace Module["PThreads"].allocateUnusedWorker with this version that knows about assets
function replacementAllocateUnusedWorker(): void {
    if (runtimeHelpers.diagnosticTracing)
        console.debug("MONO_WASM: replacementAllocateUnusedWorker");
    const asset = resolve_asset_path("js-module-threads");
    const uri = asset.resolvedUrl;
    mono_assert(uri !== undefined, "could not resolve the uri for the js-module-threads asset");
    const worker = new Worker(uri);
    Internals.getUnusedWorkerPool().push(worker);
}
