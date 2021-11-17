// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/emscripten.d.ts" />
/// <reference path="./types/v8.d.ts" />

import { EmscriptenModuleMono, MonoConfig, RuntimeHelpers } from "./types";

// these are our public API (except internal)
export let Module: EmscriptenModule & EmscriptenModuleMono;
export let MONO: any;
export let BINDING: any;
export let INTERNAL: any;

// these are imported and re-exported from emscripten internals
export let ENVIRONMENT_IS_GLOBAL: boolean;
export let ENVIRONMENT_IS_NODE: boolean;
export let ENVIRONMENT_IS_SHELL: boolean;
export let ENVIRONMENT_IS_WEB: boolean;
export let locateFile: Function;

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function setImportsAndExports(
    imports: { isGlobal: boolean, isNode: boolean, isShell: boolean, isWeb: boolean, locateFile: Function },
    exports: { mono: any, binding: any, internal: any, module: any },
) {
    MONO = exports.mono;
    BINDING = exports.binding;
    INTERNAL = exports.internal;
    Module = exports.module;
    ENVIRONMENT_IS_GLOBAL = imports.isGlobal;
    ENVIRONMENT_IS_NODE = imports.isNode;
    ENVIRONMENT_IS_SHELL = imports.isShell;
    ENVIRONMENT_IS_WEB = imports.isWeb;
    locateFile = imports.locateFile;
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
};
