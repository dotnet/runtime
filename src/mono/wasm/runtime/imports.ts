// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/v8.d.ts" />

import { DotnetModule, EarlyExports, EarlyImports, MonoConfig, RuntimeHelpers } from "./types";
import { EmscriptenModule } from "./types/emscripten";

// these are our public API (except internal)
export let Module: EmscriptenModule & DotnetModule;
export let MONO: any;
export let BINDING: any;
export let INTERNAL: any;
export let EXPORTS: any;
export let IMPORTS: any;

// these are imported and re-exported from emscripten internals
export let ENVIRONMENT_IS_NODE: boolean;
export let ENVIRONMENT_IS_SHELL: boolean;
export let ENVIRONMENT_IS_WEB: boolean;
export let ENVIRONMENT_IS_WORKER: boolean;
export let ENVIRONMENT_IS_PTHREAD: boolean;

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function set_imports_exports(
    imports: EarlyImports,
    exports: EarlyExports,
): void {
    MONO = exports.mono;
    BINDING = exports.binding;
    INTERNAL = exports.internal;
    Module = exports.module;

    EXPORTS = exports.marshaled_exports; // [JSExport]
    IMPORTS = exports.marshaled_imports; // [JSImport]

    ENVIRONMENT_IS_NODE = imports.isNode;
    ENVIRONMENT_IS_SHELL = imports.isShell;
    ENVIRONMENT_IS_WEB = imports.isWeb;
    ENVIRONMENT_IS_WORKER = imports.isWorker;
    ENVIRONMENT_IS_PTHREAD = imports.isPThread;
    runtimeHelpers.quit = imports.quit_;
    runtimeHelpers.ExitStatus = imports.ExitStatus;
    runtimeHelpers.requirePromise = imports.requirePromise;
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
