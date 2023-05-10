// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/v8.d.ts" />
/// <reference path="./types/node.d.ts" />

import { RuntimeAPI } from "./types-api";
import type { DotnetModule, GlobalObjects, EmscriptenInternals, EmscriptenModuleInternal, RuntimeHelpers } from "./types";
import type { EmscriptenModule } from "./types/emscripten";

// these are our public API (except internal)
export let Module: EmscriptenModule & DotnetModule & EmscriptenModuleInternal;
export let INTERNAL: any;

// these are imported and re-exported from emscripten internals
export const ENVIRONMENT_IS_NODE = typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
export const ENVIRONMENT_IS_WEB = typeof window == "object";
export let ENVIRONMENT_IS_SHELL: boolean;
export let ENVIRONMENT_IS_WORKER: boolean;
export let ENVIRONMENT_IS_PTHREAD: boolean;
export let exportedRuntimeAPI: RuntimeAPI = null as any;
export let runtimeHelpers: RuntimeHelpers = null as any;
// this is when we link with workload tools. The consts:WasmEnableLegacyJsInterop is when we compile with rollup.
export let disableLegacyJsInterop = false;
export let earlyExports: GlobalObjects;

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function passEmscriptenInternals(
    internals: EmscriptenInternals,
): void {
    ENVIRONMENT_IS_SHELL = internals.isShell;
    ENVIRONMENT_IS_WORKER = internals.isWorker;
    ENVIRONMENT_IS_PTHREAD = internals.isPThread;
    disableLegacyJsInterop = internals.disableLegacyJsInterop;
    runtimeHelpers.quit = internals.quit_;
    runtimeHelpers.ExitStatus = internals.ExitStatus;
}

export function setGlobalObjects(
    globalObjects: GlobalObjects,
) {
    earlyExports = globalObjects;
    Module = globalObjects.module;
    INTERNAL = globalObjects.internal;
    runtimeHelpers = globalObjects.helpers;
    exportedRuntimeAPI = globalObjects.api;

    Object.assign(globalObjects.module, {
        disableDotnet6Compatibility: true,
        config: { environmentVariables: {} }
    });
    Object.assign(globalObjects.module.config!, {}) as any;
    Object.assign(earlyExports.api, {
        Module: globalObjects.module, ...globalObjects.module
    });
    Object.assign(earlyExports.api, {
        INTERNAL: earlyExports.internal,
    });
    Object.assign(runtimeHelpers, {
        javaScriptExports: {} as any,
        mono_wasm_bindings_is_ready: false,
        maxParallelDownloads: 16,
        enableDownloadRetry: true,
        config: globalObjects.module.config,
        diagnosticTracing: false,
        enablePerfMeasure: true,
        loadedFiles: []
    } as Partial<RuntimeHelpers>);
}
