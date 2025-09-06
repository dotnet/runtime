// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import type { EmscriptenReplacements } from "./types/internal";
import type { TypedArray } from "./types/emscripten";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_WORKER, INTERNAL, Module, loaderHelpers, runtimeHelpers } from "./globals";
import { replaceEmscriptenTLSInit } from "./pthreads";
import { replaceEmscriptenPThreadUI } from "./pthreads";

const dummyPerformance = {
    now: function () {
        return Date.now();
    }
};

export function initializeReplacements (replacements: EmscriptenReplacements): void {
    // performance.now() is used by emscripten and doesn't work in JSC
    if (typeof globalThis.performance === "undefined") {
        globalThis.performance = dummyPerformance as any;
    }

    // script location
    replacements.scriptDirectory = loaderHelpers.scriptDirectory;
    if (Module.locateFile === Module.__locateFile) {
        Module.locateFile = loaderHelpers.locateFile;
    }

    // prefer fetch_like over global fetch for assets
    replacements.fetch = loaderHelpers.fetch_like;

    // misc
    replacements.ENVIRONMENT_IS_WORKER = ENVIRONMENT_IS_WORKER;

    // threads
    if (WasmEnableThreads && replacements.modulePThread) {
        if (ENVIRONMENT_IS_WORKER) {
            replaceEmscriptenTLSInit(replacements.modulePThread);
        } else {
            replaceEmscriptenPThreadUI(replacements.modulePThread);
        }
    }
}

export async function init_polyfills_async (): Promise<void> {
    if (ENVIRONMENT_IS_NODE) {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore:
        globalThis.performance = await import(/*! webpackIgnore: true */"perf_hooks");
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore:
        INTERNAL.process = await import(/*! webpackIgnore: true */"process");

        if (!globalThis.crypto) {
            globalThis.crypto = <any>{};
        }
        if (!globalThis.crypto.getRandomValues) {
            let nodeCrypto: any = undefined;
            try {
                // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                // @ts-ignore:
                nodeCrypto = await import(/*! webpackIgnore: true */"node:crypto");
            } catch (err: any) {
                // Noop, error throwing polyfill provided bellow
            }

            if (!nodeCrypto) {
                globalThis.crypto.getRandomValues = () => {
                    throw new Error("Using node without crypto support. To enable current operation, either provide polyfill for 'globalThis.crypto.getRandomValues' or enable 'node:crypto' module.");
                };
            } else if (nodeCrypto.webcrypto) {
                globalThis.crypto = nodeCrypto.webcrypto;
            } else if (nodeCrypto.randomBytes) {
                globalThis.crypto.getRandomValues = (buffer: TypedArray) => {
                    if (buffer) {
                        buffer.set(nodeCrypto.randomBytes(buffer.length));
                    }
                };
            }
        }
    }
    runtimeHelpers.subtle = globalThis.crypto?.subtle;
}


