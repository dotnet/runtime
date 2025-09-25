// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Common symbols shared between multiple JS modules.
 * IMPORTANT: Anything you add into this folder could be duplicated into multiple JS bundles!
 * Please keep it small and register it into emscripten as dependency.
 */

import type { DotnetModuleInternal, InternalExchange, RuntimeExports, LoaderExports, RuntimeAPI, LoggerType, AssertType, JSEngineType, BrowserHostExports, InteropJavaScriptExports, LoaderExportsTable, RuntimeExportsTable, BrowserHostExportsTable, InteropJavaScriptExportsTable, NativeBrowserExports, NativeBrowserExportsTable } from "../types";
import { InternalExchangeIndex } from "../types";

export let Module: DotnetModuleInternal;
export let netPublicApi: RuntimeAPI;
export let Logger: LoggerType = {} as any;
export let Assert: AssertType = {} as any;
export let netJSEngine: JSEngineType = {}as any;
export let netLoaderExports: LoaderExports = {} as any;
export let netRuntimeExports: RuntimeExports = {} as any;
export let netBrowserHostExports: BrowserHostExports = {} as any;
export let netInteropJSExports: InteropJavaScriptExports = {} as any;
export let netNativeBrowserExports: NativeBrowserExports = {} as any;
export let netInternals: InternalExchange;

export function getInternals(): InternalExchange {
    return netInternals;
}

export function netSetInternals(internal: InternalExchange) {
    netInternals = internal;
    netPublicApi = netInternals[InternalExchangeIndex.RuntimeAPI];
    Module = netPublicApi.Module as any;
    if (netInternals[InternalExchangeIndex.InternalUpdatesCallbacks] === undefined) {
        netInternals[InternalExchangeIndex.InternalUpdatesCallbacks] = [];
    }
}

export function netUpdateAllInternals() {
    for (const updateImpl of netInternals[InternalExchangeIndex.InternalUpdatesCallbacks]) {
        updateImpl();
    }
}

export function netUpdateModuleInternals() {
    if (Object.keys(netLoaderExports).length === 0 && netInternals[InternalExchangeIndex.LoaderExportsTable]) {
        netLoaderExports = {} as LoaderExports;
        Logger = {} as LoggerType;
        Assert = {} as AssertType;
        netJSEngine = {} as JSEngineType;
        expandLE(netInternals[InternalExchangeIndex.LoaderExportsTable], Logger, Assert, netJSEngine, netLoaderExports);
    }
    if (Object.keys(netRuntimeExports).length === 0 && netInternals[InternalExchangeIndex.RuntimeExportsTable]) {
        netRuntimeExports = {} as RuntimeExports;
        expandRE(netInternals[InternalExchangeIndex.RuntimeExportsTable], netRuntimeExports);
    }
    if (Object.keys(netBrowserHostExports).length === 0 && netInternals[InternalExchangeIndex.BrowserHostExportsTable]) {
        netBrowserHostExports = {} as BrowserHostExports;
        expandBHE(netInternals[InternalExchangeIndex.BrowserHostExportsTable], netBrowserHostExports);
    }
    if (Object.keys(netInteropJSExports).length === 0 && netInternals[InternalExchangeIndex.InteropJavaScriptExportsTable]) {
        netInteropJSExports = {} as InteropJavaScriptExports;
        expandIJSE(netInternals[InternalExchangeIndex.InteropJavaScriptExportsTable], netInteropJSExports);
    }
    if (Object.keys(netNativeBrowserExports).length === 0 && netInternals[InternalExchangeIndex.NativeBrowserExportsTable]) {
        netNativeBrowserExports = {} as NativeBrowserExports;
        expandNBE(netInternals[InternalExchangeIndex.NativeBrowserExportsTable], netNativeBrowserExports);
    }

    function expandRE(table:RuntimeExportsTable, runtime:RuntimeExports):void {
        Object.assign(runtime, {
        });
    }

    function expandLE(table:LoaderExportsTable, logger:LoggerType, assert:AssertType, jsEngine:JSEngineType, netLoaderExports:LoaderExports):void {
        const loggerLocal :LoggerType = {
            info: table[0],
            warn: table[1],
            error: table[2],
        };
        const assertLocal :AssertType = {
            check: table[3],
        };
        const loaderExportsLocal :LoaderExports = {
            ENVIRONMENT_IS_NODE: table[4],
            ENVIRONMENT_IS_SHELL: table[5],
            ENVIRONMENT_IS_WEB: table[6],
            ENVIRONMENT_IS_WORKER: table[7],
            ENVIRONMENT_IS_SIDECAR: table[8],
            resolveRunMainPromise: table[9],
            rejectRunMainPromise: table[10],
            getRunMainPromise: table[11],
        };
        const jsEngineLocal :JSEngineType = {
            IS_NODE: loaderExportsLocal.ENVIRONMENT_IS_NODE(),
            IS_SHELL: loaderExportsLocal.ENVIRONMENT_IS_SHELL(),
            IS_WEB: loaderExportsLocal.ENVIRONMENT_IS_WEB(),
            IS_WORKER: loaderExportsLocal.ENVIRONMENT_IS_WORKER(),
            IS_SIDECAR: loaderExportsLocal.ENVIRONMENT_IS_SIDECAR(),
        };
        Object.assign(netLoaderExports, loaderExportsLocal);
        Object.assign(logger, loggerLocal);
        Object.assign(assert, assertLocal);
        Object.assign(jsEngine, jsEngineLocal);
    }

    function expandBHE(table:BrowserHostExportsTable, native:BrowserHostExports):void {
        const nativeLocal :BrowserHostExports = {
            registerDllBytes: table[0],
            isSharedArrayBuffer: table[1],
        };
        Object.assign(native, nativeLocal);
    }

    function expandIJSE(table:InteropJavaScriptExportsTable, interop:InteropJavaScriptExports):void {
        const interopLocal :InteropJavaScriptExports = {
        };
        Object.assign(interop, interopLocal);
    }

    function expandNBE(table:NativeBrowserExportsTable, interop:NativeBrowserExports):void {
        const interopLocal :NativeBrowserExports = {
        };
        Object.assign(interop, interopLocal);
    }
}

/**
 * Functions below allow our JS modules to exchange internal interfaces by passing tables of functions in known order instead of using string symbols.
 * IMPORTANT: If you need to add more functions, make sure that you add them at the end of the table, so that the order of existing functions does not change.
 */

export function netTabulateLE(logger:LoggerType, assert:AssertType, netLoaderExports:LoaderExports):LoaderExportsTable {
    return [
        logger.info,
        logger.warn,
        logger.error,
        assert.check,
        netLoaderExports.ENVIRONMENT_IS_NODE,
        netLoaderExports.ENVIRONMENT_IS_SHELL,
        netLoaderExports.ENVIRONMENT_IS_WEB,
        netLoaderExports.ENVIRONMENT_IS_WORKER,
        netLoaderExports.ENVIRONMENT_IS_SIDECAR,
        netLoaderExports.resolveRunMainPromise,
        netLoaderExports.rejectRunMainPromise,
        netLoaderExports.getRunMainPromise,
    ];
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function netTabulateRE(map:RuntimeExports):RuntimeExportsTable {
    return [
    ];
}

export function netTabulateBHE(map:BrowserHostExports):BrowserHostExportsTable {
    return [
        map.registerDllBytes,
        map.isSharedArrayBuffer,
    ];
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function netTabulateIJSE(map:InteropJavaScriptExports):InteropJavaScriptExportsTable {
    return [
    ];
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function netTabulateNBE(map:NativeBrowserExports):NativeBrowserExportsTable {
    return [
    ];
}

