// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import type { EmscriptenReplacements } from "./types/internal";
import { ENVIRONMENT_IS_SHELL, ENVIRONMENT_IS_WORKER, Module, loaderHelpers } from "./globals";
import { replaceEmscriptenTLSInit } from "./pthreads";
import { replaceEmscriptenPThreadUI } from "./pthreads";

const dummyPerformance = {
    now: function () {
        return Date.now();
    }
};

export function initializeReplacements (replacements: EmscriptenReplacements): void {
    // performance.now() is used by emscripten and doesn't work in V8
    if (ENVIRONMENT_IS_SHELL && typeof globalThis.performance === "undefined") {
        globalThis.performance = dummyPerformance as any;
    }
    if (typeof globalThis.crypto === "undefined") {
        globalThis.crypto = {} as any;
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


