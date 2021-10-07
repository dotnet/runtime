// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-var */
/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/emscripten.d.ts" />
/// <reference path="./types/v8.d.ts" />

import { t_ModuleExtension } from "./exports";
import { MonoConfig, t_RuntimeHelpers } from "./types";

export var Module: t_Module & t_ModuleExtension;
export var MONO: any;
export var BINDING: any;

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function setLegacyModules(mono: any, binding: any, module: t_Module & t_ModuleExtension) {
    Module = module;
    MONO = mono;
    BINDING = binding;
}

let monoConfig: MonoConfig;
let runtime_is_ready = false;

export const runtimeHelpers: t_RuntimeHelpers = <any>{
    namespace: "System.Runtime.InteropServices.JavaScript",
    classname: "Runtime",
    loaded_files: [],
    get mono_wasm_runtime_is_ready() {
        return runtime_is_ready;
    },
    set mono_wasm_runtime_is_ready(value: boolean) {
        runtime_is_ready = value;
        MONO.mono_wasm_runtime_is_ready = value;
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
