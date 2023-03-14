// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/v8.d.ts" />
/// <reference path="./types/node.d.ts" />

import { RuntimeAPI } from "./loader/types";
import type { DotnetModule, EarlyExports, EarlyImports, RuntimeHelpers } from "./types";
import type { EmscriptenModule, EmscriptenModuleInternal } from "./types/emscripten";

// these are our public API (except internal)
export let Module: EmscriptenModule & DotnetModule & EmscriptenModuleInternal;
export let INTERNAL: any;

// these are imported and re-exported from emscripten internals
export let ENVIRONMENT_IS_NODE: boolean;
export let ENVIRONMENT_IS_SHELL: boolean;
export let ENVIRONMENT_IS_WEB: boolean;
export let ENVIRONMENT_IS_WORKER: boolean;
export let ENVIRONMENT_IS_PTHREAD: boolean;
export let exportedRuntimeAPI: RuntimeAPI = {} as any;
export let runtimeHelpers: RuntimeHelpers;

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function set_imports_exports(
    imports: EarlyImports,
    exports: EarlyExports,
): void {
    INTERNAL = exports.internal;
    Module = exports.module;
    runtimeHelpers = exports.helpers;
    exportedRuntimeAPI = exports.api;

    ENVIRONMENT_IS_NODE = imports.isNode;
    ENVIRONMENT_IS_SHELL = imports.isShell;
    ENVIRONMENT_IS_WEB = imports.isWeb;
    ENVIRONMENT_IS_WORKER = imports.isWorker;
    ENVIRONMENT_IS_PTHREAD = imports.isPThread;
    Object.assign(exports.module.config, {
        environmentVariables: {},
    });
    Object.assign(runtimeHelpers, {
        javaScriptExports: {} as any,
        mono_wasm_bindings_is_ready: false,
        maxParallelDownloads: 16,
        enableDownloadRetry: true,
        config: exports.module.config,
        diagnosticTracing: false,
        enablePerfMeasure: true,
        loadedFiles: []
    } as Partial<RuntimeHelpers>);
    runtimeHelpers.quit = imports.quit_;
    runtimeHelpers.ExitStatus = imports.ExitStatus;
}


