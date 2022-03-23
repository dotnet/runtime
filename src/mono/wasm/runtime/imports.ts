// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/v8.d.ts" />

import { DotnetModule, MonoConfig, RuntimeHelpers } from "./types";
import { EmscriptenModule } from "./types/emscripten";

// these are our public API (except internal)
export let Module: EmscriptenModule & DotnetModule;
export let MONO: any;
export let BINDING: any;
export let INTERNAL: any;

// these are imported and re-exported from emscripten internals
export let ENVIRONMENT_IS_ESM: boolean;
export let ENVIRONMENT_IS_NODE: boolean;
export let ENVIRONMENT_IS_SHELL: boolean;
export let ENVIRONMENT_IS_WEB: boolean;
export let locateFile: Function;
export let quit: Function;
export let ExitStatus: ExitStatusError;
export let requirePromise: Promise<Function>;
export let readFile: Function;

export interface ExitStatusError {
    new(status: number): any;
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function setImportsAndExports(
    imports: { isESM: boolean, isNode: boolean, isShell: boolean, isWeb: boolean, locateFile: Function, ExitStatus: ExitStatusError, quit_: Function, requirePromise: Promise<Function> },
    exports: { mono: any, binding: any, internal: any, module: any },
): void {
    MONO = exports.mono;
    BINDING = exports.binding;
    INTERNAL = exports.internal;
    Module = exports.module;
    ENVIRONMENT_IS_ESM = imports.isESM;
    ENVIRONMENT_IS_NODE = imports.isNode;
    ENVIRONMENT_IS_SHELL = imports.isShell;
    ENVIRONMENT_IS_WEB = imports.isWeb;
    locateFile = imports.locateFile;
    quit = imports.quit_;
    ExitStatus = imports.ExitStatus;
    requirePromise = imports.requirePromise;
}

let monoConfig: MonoConfig;
let runtime_is_ready = false;

export const runtimeHelpers: RuntimeHelpers = <any>{
    namespace: "System.Runtime.InteropServices.JavaScript",
    classname: "Runtime",
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
    fetch: null
};
