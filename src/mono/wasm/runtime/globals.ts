// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/triple-slash-reference */
/// <reference path="./types/v8.d.ts" />
/// <reference path="./types/node.d.ts" />

import { RuntimeAPI } from "./types/index";
import type { GlobalObjects, EmscriptenInternals, RuntimeHelpers, LoaderHelpers, DotnetModuleInternal, PromiseAndController } from "./types/internal";

// these are our public API (except internal)
export let Module: DotnetModuleInternal;
export let INTERNAL: any;

export const ENVIRONMENT_IS_NODE = typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
export const ENVIRONMENT_IS_WEB = typeof window == "object";
export const ENVIRONMENT_IS_WORKER = typeof importScripts == "function";
export const ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE && !ENVIRONMENT_IS_WORKER;
// these are imported and re-exported from emscripten internals
export let ENVIRONMENT_IS_PTHREAD: boolean;
export let exportedRuntimeAPI: RuntimeAPI = null as any;
export let runtimeHelpers: RuntimeHelpers = null as any;
export let loaderHelpers: LoaderHelpers = null as any;
// this is when we link with workload tools. The consts:WasmEnableLegacyJsInterop is when we compile with rollup.
export let disableLegacyJsInterop = false;
export let _runtimeModuleLoaded = false; // please keep it in place also as rollup guard

export function passEmscriptenInternals(internals: EmscriptenInternals): void {
    ENVIRONMENT_IS_PTHREAD = internals.isPThread;
    disableLegacyJsInterop = internals.disableLegacyJsInterop;
    runtimeHelpers.quit = internals.quit_;
    runtimeHelpers.ExitStatus = internals.ExitStatus;
}

// NOTE: this is called AFTER the config is loaded
export function setRuntimeGlobals(globalObjects: GlobalObjects) {
    if (_runtimeModuleLoaded) {
        throw new Error("Runtime module already loaded");
    }
    _runtimeModuleLoaded = true;
    Module = globalObjects.module;
    INTERNAL = globalObjects.internal;
    runtimeHelpers = globalObjects.runtimeHelpers;
    loaderHelpers = globalObjects.loaderHelpers;
    exportedRuntimeAPI = globalObjects.api;

    Object.assign(runtimeHelpers, {
        allAssetsInMemory: createPromiseController<void>(),
        dotnetReady: createPromiseController<any>(),
        memorySnapshotSkippedOrDone: createPromiseController<void>(),
        afterInstantiateWasm: createPromiseController<void>(),
        beforePreInit: createPromiseController<void>(),
        afterPreInit: createPromiseController<void>(),
        afterPreRun: createPromiseController<void>(),
        beforeOnRuntimeInitialized: createPromiseController<void>(),
        afterOnRuntimeInitialized: createPromiseController<void>(),
        afterPostRun: createPromiseController<void>(),
    });

    Object.assign(globalObjects.module.config!, {}) as any;
    Object.assign(globalObjects.api, {
        Module: globalObjects.module, ...globalObjects.module
    });
    Object.assign(globalObjects.api, {
        INTERNAL: globalObjects.internal,
    });
}

export function createPromiseController<T>(afterResolve?: () => void, afterReject?: () => void): PromiseAndController<T> {
    return loaderHelpers.createPromiseController<T>(afterResolve, afterReject);
}