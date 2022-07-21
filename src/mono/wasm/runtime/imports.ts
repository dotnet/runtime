// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/v8.d.ts" />

import { fetch_like } from "./polyfills";
import { afterLoadWasmModuleToWorker } from "./pthreads/browser";
import { afterThreadInitTLS } from "./pthreads/worker";
import { DotnetModule, DotnetModuleConfigImports, EarlyExports, EarlyImports, EarlyReplacements, MonoConfig, RuntimeHelpers } from "./types";
import { EmscriptenModule } from "./types/emscripten";
import MonoWasmThreads from "consts:monoWasmThreads";
import { afterUpdateGlobalBufferAndViews } from "./memory";

// these are our public API (except internal)
export let Module: EmscriptenModule & DotnetModule;
export let MONO: any;
export let BINDING: any;
export let INTERNAL: any;
export let EXPORTS: any;
export let IMPORTS: any;

// these are imported and re-exported from emscripten internals
export let ENVIRONMENT_IS_ESM: boolean;
export let ENVIRONMENT_IS_NODE: boolean;
export let ENVIRONMENT_IS_SHELL: boolean;
export let ENVIRONMENT_IS_WEB: boolean;
export let ENVIRONMENT_IS_WORKER: boolean;
export let ENVIRONMENT_IS_PTHREAD: boolean;

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function setImportsAndExports(
    imports: EarlyImports,
    exports: EarlyExports,
    replacements: EarlyReplacements,
): void {
    MONO = exports.mono;
    BINDING = exports.binding;
    INTERNAL = exports.internal;
    Module = exports.module;
    const anyModule = Module as any;

    EXPORTS = exports.marshaled_exports; // [JSExport]
    IMPORTS = exports.marshaled_imports; // [JSImport]

    ENVIRONMENT_IS_ESM = imports.isESM;
    ENVIRONMENT_IS_NODE = imports.isNode;
    ENVIRONMENT_IS_SHELL = imports.isShell;
    ENVIRONMENT_IS_WEB = imports.isWeb;
    ENVIRONMENT_IS_WORKER = imports.isWorker;
    ENVIRONMENT_IS_PTHREAD = imports.isPThread;
    runtimeHelpers.quit = imports.quit_;
    runtimeHelpers.ExitStatus = imports.ExitStatus;
    runtimeHelpers.requirePromise = imports.requirePromise;

    // require replacement
    Module.imports = Module.imports || <DotnetModuleConfigImports>{};
    const requireWrapper = (wrappedRequire: Function) => (name: string) => {
        const resolved = (<any>Module.imports)[name];
        if (resolved) {
            return resolved;
        }
        return wrappedRequire(name);
    };
    if (Module.imports.require) {
        runtimeHelpers.requirePromise = replacements.requirePromise = Promise.resolve(requireWrapper(Module.imports.require));
    }
    else if (replacements.require) {
        runtimeHelpers.requirePromise = replacements.requirePromise = Promise.resolve(requireWrapper(replacements.require));
    } else if (replacements.requirePromise) {
        runtimeHelpers.requirePromise = replacements.requirePromise.then(require => requireWrapper(require));
    } else {
        runtimeHelpers.requirePromise = replacements.requirePromise = Promise.resolve(requireWrapper((name: string) => {
            throw new Error(`Please provide Module.imports.${name} or Module.imports.require`);
        }));
    }

    // script location
    runtimeHelpers.scriptDirectory = replacements.scriptDirectory = detectScriptDirectory(replacements);
    anyModule.mainScriptUrlOrBlob = replacements.scriptUrl;// this is needed by worker threads
    console.trace(`MONO_WASM: starting script ${replacements.scriptUrl}`);
    console.trace(`MONO_WASM: starting in ${runtimeHelpers.scriptDirectory}`);
    if (anyModule.__locateFile === anyModule.locateFile) {
        // it's our early version from dotnet.es6.pre.js, we could replace it
        anyModule.locateFile = runtimeHelpers.locateFile = (path) => runtimeHelpers.scriptDirectory + path;
    } else {
        // we use what was given to us
        runtimeHelpers.locateFile = anyModule.locateFile;
    }

    // fetch poly
    if (Module.imports.fetch) {
        replacements.fetch = runtimeHelpers.fetch_like = Module.imports.fetch;
    }
    else {
        replacements.fetch = runtimeHelpers.fetch_like = fetch_like;
    }

    // misc
    replacements.noExitRuntime = ENVIRONMENT_IS_WEB;

    // threads
    if (MonoWasmThreads) {
        if (replacements.pthreadReplacements) {
            const originalLoadWasmModuleToWorker = replacements.pthreadReplacements.loadWasmModuleToWorker;
            replacements.pthreadReplacements.loadWasmModuleToWorker = (worker: Worker, onFinishedLoading: Function): void => {
                originalLoadWasmModuleToWorker(worker, onFinishedLoading);
                afterLoadWasmModuleToWorker(worker);
            };
            const originalThreadInitTLS = replacements.pthreadReplacements.threadInitTLS;
            replacements.pthreadReplacements.threadInitTLS = (): void => {
                originalThreadInitTLS();
                afterThreadInitTLS();
            };
        }
    }

    // memory
    const originalUpdateGlobalBufferAndViews = replacements.updateGlobalBufferAndViews;
    replacements.updateGlobalBufferAndViews = (buffer: ArrayBufferLike) => {
        originalUpdateGlobalBufferAndViews(buffer);
        afterUpdateGlobalBufferAndViews(buffer);
    };
}

function normalizeFileUrl(filename: string) {
    // unix vs windows
    // remove query string
    return filename.replace(/\\/g, "/").replace(/[?#].*/, "");
}

function normalizeDirectoryUrl(dir: string) {
    return dir.slice(0, dir.lastIndexOf("/")) + "/";
}

export function detectScriptDirectory(replacements: EarlyReplacements): string {
    if (ENVIRONMENT_IS_WORKER) {
        // Check worker, not web, since window could be polyfilled
        replacements.scriptUrl = self.location.href;
    }
    // when ENVIRONMENT_IS_ESM we have scriptUrl from import.meta.url from dotnet.es6.lib.js
    if (!ENVIRONMENT_IS_ESM) {
        if (ENVIRONMENT_IS_WEB) {
            if (
                (typeof (globalThis.document) === "object") &&
                (typeof (globalThis.document.createElement) === "function")
            ) {
                // blazor injects a module preload link element for dotnet.[version].[sha].js
                const blazorDotNetJS = Array.from(document.head.getElementsByTagName("link")).filter(elt => elt.rel !== undefined && elt.rel == "modulepreload" && elt.href !== undefined && elt.href.indexOf("dotnet") != -1 && elt.href.indexOf(".js") != -1);
                if (blazorDotNetJS.length == 1) {
                    replacements.scriptUrl = blazorDotNetJS[0].href;
                } else {
                    const temp = globalThis.document.createElement("a");
                    temp.href = "dotnet.js";
                    replacements.scriptUrl = temp.href;
                }
            }
        }
        if (ENVIRONMENT_IS_NODE) {
            if (typeof __filename !== "undefined") {
                // unix vs windows
                replacements.scriptUrl = __filename;
            }
        }
    }
    if (!replacements.scriptUrl) {
        // probably V8 shell in non ES6
        replacements.scriptUrl = "./dotnet.js";
    }
    replacements.scriptUrl = normalizeFileUrl(replacements.scriptUrl);
    return normalizeDirectoryUrl(replacements.scriptUrl);
}

let monoConfig: MonoConfig = {} as any;
let runtime_is_ready = false;

export const runtimeHelpers: RuntimeHelpers = <any>{
    namespace: "System.Runtime.InteropServices.JavaScript",
    classname: "Runtime",
    mono_wasm_load_runtime_done: false,
    mono_wasm_bindings_is_ready: false,
    get mono_wasm_runtime_is_ready() {
        return runtime_is_ready;
    },
    set mono_wasm_runtime_is_ready(value: boolean) {
        runtime_is_ready = value;
        INTERNAL.mono_wasm_runtime_is_ready = value;
    },
    get config() {
        return monoConfig;
    },
    set config(value: MonoConfig) {
        monoConfig = value;
        MONO.config = value;
        Module.config = value;
    },
    diagnostic_tracing: false,
    enable_debugging: false,
    fetch: null
};
