// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/v8.d.ts" />

import { DotnetModule, EarlyExports, EarlyImports, RuntimeHelpers } from "./types";
import { EmscriptenModule } from "./types/emscripten";

// these are our public API (except internal)
export let Module: EmscriptenModule & DotnetModule;
export let INTERNAL: any;
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
    INTERNAL = exports.internal;
    Module = exports.module;

    ENVIRONMENT_IS_NODE = imports.isNode;
    ENVIRONMENT_IS_SHELL = imports.isShell;
    ENVIRONMENT_IS_WEB = imports.isWeb;
    ENVIRONMENT_IS_WORKER = imports.isWorker;
    ENVIRONMENT_IS_PTHREAD = imports.isPThread;
    runtimeHelpers.quit = imports.quit_;
    runtimeHelpers.ExitStatus = imports.ExitStatus;
    runtimeHelpers.requirePromise = imports.requirePromise;
}

export const runtimeHelpers: RuntimeHelpers = <any>{
    javaScriptExports: {},
    mono_wasm_load_runtime_done: false,
    mono_wasm_bindings_is_ready: false,
    max_parallel_downloads: 16,
    config: {},
    diagnosticTracing: false,
    fetch: null
};
