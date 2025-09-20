// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Common symbols shared between multiple JS modules.
 * IMPORTANT: Anything you add into this folder could be duplicated into multiple JS bundles!
 * Please keep it small and register it into emscripten as dependency.
 */

import type { DotnetModuleInternal, InternalApis, RuntimeExports, LoaderExports, RuntimeAPI, LoggerType, AssertType, EnvironmentType, NativeExports, InteropExports, LoaderExportsTable, RuntimeExportsTable, NativeExportsTable, InteropExportsTable } from "../types";

export let JSEngine: EnvironmentType;
export let Module: DotnetModuleInternal;
export let runtimeApi: RuntimeAPI;
export let Logger: LoggerType = {} as any;
export let Assert: AssertType = {} as any;
export let loaderExports: LoaderExports = {} as any;
export let runtimeExports: RuntimeExports = {} as any;
export let nativeExports: NativeExports = {} as any;
export let interopExports: InteropExports = {} as any;
export let dotnetInternals: InternalApis;

export function getInternals(): InternalApis {
    return dotnetInternals;
}

export function setInternals(internal: Partial<InternalApis>) {
    dotnetInternals = internal as InternalApis;
    runtimeApi = dotnetInternals.runtimeApi;
    Module = dotnetInternals.runtimeApi.Module as any;
}

export function updateInternals() {
    if (dotnetInternals.updates === undefined) {
        dotnetInternals.updates = [];
    }
    for (const updateImpl of dotnetInternals.updates) {
        updateImpl();
    }
}

export function updateInternalsImpl() {
    if (!JSEngine) {
        const ENVIRONMENT_IS_NODE = typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
        const ENVIRONMENT_IS_WEB_WORKER = typeof importScripts == "function";
        const ENVIRONMENT_IS_SIDECAR = ENVIRONMENT_IS_WEB_WORKER && typeof dotnetSidecar !== "undefined"; // sidecar is emscripten main running in a web worker
        const ENVIRONMENT_IS_WORKER = ENVIRONMENT_IS_WEB_WORKER && !ENVIRONMENT_IS_SIDECAR; // we redefine what ENVIRONMENT_IS_WORKER, we replace it in emscripten internals, so that sidecar works
        const ENVIRONMENT_IS_WEB = typeof window == "object" || (ENVIRONMENT_IS_WEB_WORKER && !ENVIRONMENT_IS_NODE);
        const ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE;
        JSEngine = {
            IS_NODE: ENVIRONMENT_IS_NODE,
            IS_SHELL: ENVIRONMENT_IS_SHELL,
            IS_WEB: ENVIRONMENT_IS_WEB,
            IS_WORKER: ENVIRONMENT_IS_WORKER,
            IS_SIDECAR: ENVIRONMENT_IS_SIDECAR,
        };
    }
    if (Object.keys(loaderExports).length === 0 && dotnetInternals.loaderExportsTable) {
        loaderExports = {} as LoaderExports;
        Logger = {} as LoggerType;
        Assert = {} as AssertType;
        loaderExportsFromTable(dotnetInternals.loaderExportsTable, Logger, Assert, loaderExports);
    }
    if (Object.keys(runtimeExports).length === 0 && dotnetInternals.runtimeExportsTable) {
        runtimeExports = {} as RuntimeExports;
        runtimeExportsFromTable(dotnetInternals.runtimeExportsTable, runtimeExports);
    }
    if (Object.keys(nativeExports).length === 0 && dotnetInternals.nativeExportsTable) {
        nativeExports = {} as NativeExports;
        nativeExportsFromTable(dotnetInternals.nativeExportsTable, nativeExports);
    }
    if (Object.keys(interopExports).length === 0 && dotnetInternals.interopExportsTable) {
        interopExports = {} as InteropExports;
        interopExportsFromTable(dotnetInternals.interopExportsTable, interopExports);
    }
}

/**
 * Functions below allow our JS modules to exchange internal interfaces by passing tables of functions in known order instead of using string symbols.
 * IMPORTANT: If you need to add more functions, make sure that you add them at the end of the table, so that the order of existing functions does not change.
 */

export function loaderExportsToTable(logger:LoggerType, assert:AssertType, loaderExports:LoaderExports):LoaderExportsTable {
    return [
        logger.info,
        logger.warn,
        logger.error,
        assert.check,
        loaderExports.browserHostResolveMain,
        loaderExports.browserHostRejectMain,
        loaderExports.getRunMainPromise,
    ];
}

export function loaderExportsFromTable(table:LoaderExportsTable, logger:LoggerType, assert:AssertType, loaderExports:LoaderExports):void {
    const loggerLocal :LoggerType = {
        info: table[0],
        warn: table[1],
        error: table[2],
    };
    const assertLocal :AssertType = {
        check: table[3],
    };
    const loaderExportsLocal :LoaderExports = {
        browserHostResolveMain: table[4],
        browserHostRejectMain: table[5],
        getRunMainPromise: table[6],
    };
    Object.assign(logger, loggerLocal);
    Object.assign(assert, assertLocal);
    Object.assign(loaderExports, loaderExportsLocal);
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function runtimeExportsToTable(map:RuntimeExports):RuntimeExportsTable {
    return [
    ];
}

export function runtimeExportsFromTable(table:RuntimeExportsTable, runtime:RuntimeExports):void {
    Object.assign(runtime, {
    });
}

export function nativeExportsToTable(map:NativeExports):NativeExportsTable {
    return [
        map.registerDllBytes,
        map.isSharedArrayBuffer,
    ];
}

export function nativeExportsFromTable(table:NativeExportsTable, native:NativeExports):void {
    const nativeLocal :NativeExports = {
        registerDllBytes: table[0],
        isSharedArrayBuffer: table[1],
    };
    Object.assign(native, nativeLocal);
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function interopExportsToTable(map:InteropExports):InteropExportsTable {
    return [
    ];
}

export function interopExportsFromTable(table:InteropExportsTable, interop:InteropExports):void {
    const interopLocal :InteropExports = {
    };
    Object.assign(interop, interopLocal);
}

