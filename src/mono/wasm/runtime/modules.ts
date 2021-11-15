// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/emscripten.d.ts" />
/// <reference path="./types/v8.d.ts" />

import { EmscriptenModuleMono, MonoConfig, RuntimeHelpers } from "./types";

export let Module: EmscriptenModule & EmscriptenModuleMono;
export let MONO: any;
export let BINDING: any;
export let INTERNAL: any;
export let ENVIRONMENT_IS_GLOBAL: boolean;
export let ENVIRONMENT_IS_NODE: boolean;
export let ENVIRONMENT_IS_SHELL: boolean;
export let ENVIRONMENT_IS_WEB: boolean;
export let locateFile: Function;

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function setAPI(mono: any, binding: any, internal: any, module: EmscriptenModule & EmscriptenModuleMono, isGlobal: boolean, isNode: boolean, isShell: boolean, isWeb: boolean, glocateFile: Function) {
    MONO = mono;
    BINDING = binding;
    INTERNAL = internal;
    Module = module;
    ENVIRONMENT_IS_GLOBAL = isGlobal;
    ENVIRONMENT_IS_NODE = isNode;
    ENVIRONMENT_IS_SHELL = isShell;
    ENVIRONMENT_IS_WEB = isWeb;
    locateFile = glocateFile;
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
