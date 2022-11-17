// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/v8.d.ts" />
/// <reference path="./types/node.d.ts" />

import { CreateDotnetRuntimeType, DotnetModule, RuntimeAPI, EarlyExports, EarlyImports, ModuleAPI, RuntimeHelpers } from "./types";
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
export const exportedRuntimeAPI: RuntimeAPI = {} as any;
export const moduleExports: ModuleAPI = {} as any;
export let emscriptenEntrypoint: CreateDotnetRuntimeType;
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function set_imports_exports(
    imports: EarlyImports,
    exports: EarlyExports,
): void {
    INTERNAL = exports.internal;
    IMPORTS = exports.marshaled_imports;
    Module = exports.module;

    set_environment(imports);
    ENVIRONMENT_IS_NODE = imports.isNode;
    ENVIRONMENT_IS_SHELL = imports.isShell;
    ENVIRONMENT_IS_WEB = imports.isWeb;
    ENVIRONMENT_IS_WORKER = imports.isWorker;
    ENVIRONMENT_IS_PTHREAD = imports.isPThread;
    runtimeHelpers.quit = imports.quit_;
    runtimeHelpers.ExitStatus = imports.ExitStatus;
    runtimeHelpers.requirePromise = imports.requirePromise;
}

export function set_environment(imports: any) {
    ENVIRONMENT_IS_NODE = imports.isNode;
    ENVIRONMENT_IS_SHELL = imports.isShell;
    ENVIRONMENT_IS_WEB = imports.isWeb;
    ENVIRONMENT_IS_WORKER = imports.isWorker;
    ENVIRONMENT_IS_PTHREAD = imports.isPThread;
}

export function set_emscripten_entrypoint(
    entrypoint: CreateDotnetRuntimeType
): void {
    emscriptenEntrypoint = entrypoint;
}


const initialRuntimeHelpers: Partial<RuntimeHelpers> =
{
    javaScriptExports: {} as any,
    mono_wasm_load_runtime_done: false,
    mono_wasm_bindings_is_ready: false,
    maxParallelDownloads: 16,
    config: {
        environmentVariables: {},
    },
    diagnosticTracing: false,
    enablePerfMeasure: true,
};
export const runtimeHelpers: RuntimeHelpers = initialRuntimeHelpers as any;
