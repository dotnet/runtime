// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Common symbols shared between multiple JS modules.
 * IMPORTANT: Anything you add into this folder could be duplicated into multiple JS bundles!
 * Please keep it small and register it into emscripten as dependency.
 */

import type { DotnetModuleInternal, InternalExchange, RuntimeExports, LoaderExports, RuntimeAPI, dotnetLoggerType, dotnetAssertType, JSEngineType, BrowserHostExports, InteropJavaScriptExports, LoaderExportsTable, RuntimeExportsTable, BrowserHostExportsTable, InteropJavaScriptExportsTable, NativeBrowserExports, NativeBrowserExportsTable } from "../types";
import { InternalExchangeIndex } from "../types";

export let Module: DotnetModuleInternal;
export let dotnetApi: RuntimeAPI;
export let dotnetLogger: dotnetLoggerType = {} as any;
export let dotnetAssert: dotnetAssertType = {} as any;
export let dotnetJSEngine: JSEngineType = {}as any;
export let dotnetLoaderExports: LoaderExports = {} as any;
export let dotnetRuntimeExports: RuntimeExports = {} as any;
export let dotnetBrowserHostExports: BrowserHostExports = {} as any;
export let dotnetInteropJSExports: InteropJavaScriptExports = {} as any;
export let dotnetNativeBrowserExports: NativeBrowserExports = {} as any;
export let dotnetInternals: InternalExchange;

export function dotnetGetInternals(): InternalExchange {
    return dotnetInternals;
}

export function dotnetSetInternals(internal: InternalExchange) {
    dotnetInternals = internal;
    dotnetApi = dotnetInternals[InternalExchangeIndex.RuntimeAPI];
    Module = dotnetApi.Module as any;
    if (dotnetInternals[InternalExchangeIndex.InternalUpdatesCallbacks] === undefined) {
        dotnetInternals[InternalExchangeIndex.InternalUpdatesCallbacks] = [];
    }
}

export function dotnetUpdateAllInternals() {
    for (const updateImpl of dotnetInternals[InternalExchangeIndex.InternalUpdatesCallbacks]) {
        updateImpl();
    }
}

export function dotnetUpdateModuleInternals() {
    /**
     * Functions below allow our JS modules to exchange internal interfaces by passing tables of functions in known order instead of using string symbols.
     * IMPORTANT: If you need to add more functions, make sure that you add them at the end of the table, so that the order of existing functions does not change.
     */

    if (Object.keys(dotnetLoaderExports).length === 0 && dotnetInternals[InternalExchangeIndex.LoaderExportsTable]) {
        dotnetLoaderExports = {} as LoaderExports;
        dotnetLogger = {} as dotnetLoggerType;
        dotnetAssert = {} as dotnetAssertType;
        dotnetJSEngine = {} as JSEngineType;
        expandLoaderExports(dotnetInternals[InternalExchangeIndex.LoaderExportsTable], dotnetLogger, dotnetAssert, dotnetJSEngine, dotnetLoaderExports);
    }
    if (Object.keys(dotnetRuntimeExports).length === 0 && dotnetInternals[InternalExchangeIndex.RuntimeExportsTable]) {
        dotnetRuntimeExports = {} as RuntimeExports;
        expandRuntimeExports(dotnetInternals[InternalExchangeIndex.RuntimeExportsTable], dotnetRuntimeExports);
    }
    if (Object.keys(dotnetBrowserHostExports).length === 0 && dotnetInternals[InternalExchangeIndex.BrowserHostExportsTable]) {
        dotnetBrowserHostExports = {} as BrowserHostExports;
        expandBrowserHostExports(dotnetInternals[InternalExchangeIndex.BrowserHostExportsTable], dotnetBrowserHostExports);
    }
    if (Object.keys(dotnetInteropJSExports).length === 0 && dotnetInternals[InternalExchangeIndex.InteropJavaScriptExportsTable]) {
        dotnetInteropJSExports = {} as InteropJavaScriptExports;
        expandInteropJavaScriptExports(dotnetInternals[InternalExchangeIndex.InteropJavaScriptExportsTable], dotnetInteropJSExports);
    }
    if (Object.keys(dotnetNativeBrowserExports).length === 0 && dotnetInternals[InternalExchangeIndex.NativeBrowserExportsTable]) {
        dotnetNativeBrowserExports = {} as NativeBrowserExports;
        expandNativeBrowserExports(dotnetInternals[InternalExchangeIndex.NativeBrowserExportsTable], dotnetNativeBrowserExports);
    }

    // keep in sync with tabulateRuntimeExports()
    function expandRuntimeExports(table:RuntimeExportsTable, runtime:RuntimeExports):void {
        Object.assign(runtime, {
        });
    }

    // keep in sync with tabulateLoaderExports()
    function expandLoaderExports(table:LoaderExportsTable, logger:dotnetLoggerType, assert:dotnetAssertType, jsEngine:JSEngineType, dotnetLoaderExports:LoaderExports):void {
        const loggerLocal :dotnetLoggerType = {
            info: table[0],
            warn: table[1],
            error: table[2],
        };
        const assertLocal :dotnetAssertType = {
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
        Object.assign(dotnetLoaderExports, loaderExportsLocal);
        Object.assign(logger, loggerLocal);
        Object.assign(assert, assertLocal);
        Object.assign(jsEngine, jsEngineLocal);
    }

    // keep in sync with tabulateBrowserHostExports()
    function expandBrowserHostExports(table:BrowserHostExportsTable, native:BrowserHostExports):void {
        const nativeLocal :BrowserHostExports = {
            registerDllBytes: table[0],
            isSharedArrayBuffer: table[1],
        };
        Object.assign(native, nativeLocal);
    }

    // keep in sync with tabulateInteropJavaScriptExports()
    function expandInteropJavaScriptExports(table:InteropJavaScriptExportsTable, interop:InteropJavaScriptExports):void {
        const interopLocal :InteropJavaScriptExports = {
        };
        Object.assign(interop, interopLocal);
    }

    // keep in sync with tabulateNativeBrowserExports()
    function expandNativeBrowserExports(table:NativeBrowserExportsTable, interop:NativeBrowserExports):void {
        const interopLocal :NativeBrowserExports = {
        };
        Object.assign(interop, interopLocal);
    }
}
