// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Common symbols shared between multiple JS modules.
 * IMPORTANT: Anything you add into this folder could be duplicated into multiple JS bundles!
 * Please keep it small and register it into emscripten as dependency.
 *
 * The cross-module API consists of a passed table `internals` which has indexed slots for exports of various JS modules or emscripten JS sub-modules.
 * The slots denoted by compile-time integer constant `InternalExchangeIndex` and has sub-tables described by their TS type. For example `LoaderExportsTable`.
 * The JS modules are loaded gradually, so the internals are populated over time, during runtime startup.
 * Each JS module could subscribe to updates and set it's own internal symbols to functions that other modules exported.
 * This subscriber is implemented by `dotnetUpdateInternalsSubscriber()` function below.
 * Note that copy of this function is in each JS module and has visibility to the current module's internal closure.
 * After each update, the providers should call `dotnetUpdateInternals()` function to notify all subscribers.
 *
 * This design allows
 *  - each JS module to be efficiently minified without exposing symbolic names
 *  - each JS module to use exported symbols in ergonomic way
 */

import type { DotnetModuleInternal, InternalExchange, RuntimeExports, LoaderExports, RuntimeAPI, LoggerType, AssertType, BrowserHostExports, InteropJavaScriptExports, LoaderExportsTable, RuntimeExportsTable, BrowserHostExportsTable, InteropJavaScriptExportsTable, NativeBrowserExports, NativeBrowserExportsTable, InternalExchangeSubscriber } from "../types";
import { InternalExchangeIndex } from "../types";

export let Module: DotnetModuleInternal;
export let dotnetApi: RuntimeAPI;
export let dotnetLogger: LoggerType = {} as any;
export let dotnetAssert: AssertType = {} as any;
export let dotnetLoaderExports: LoaderExports = {} as any;
export let dotnetRuntimeExports: RuntimeExports = {} as any;
export let dotnetBrowserHostExports: BrowserHostExports = {} as any;
export let dotnetInteropJSExports: InteropJavaScriptExports = {} as any;
export let dotnetNativeBrowserExports: NativeBrowserExports = {} as any;
export let dotnetInternals: InternalExchange = {} as any;

export function dotnetGetInternals(): InternalExchange {
    return dotnetInternals;
}

// this should be called when we want to dispatch new internal functions to other JS modules
// subscriber parameter is the callback function with visibility to the current module's internal closure
export function dotnetUpdateInternals(internals?: InternalExchange, subscriber?: InternalExchangeSubscriber) {
    if (dotnetInternals === undefined) {
        dotnetInternals = internals!;
    }
    if (dotnetApi === undefined) {
        dotnetApi = dotnetInternals[InternalExchangeIndex.RuntimeAPI];
    }
    if (Module === undefined && dotnetApi) {
        Module = dotnetApi.Module as any;
    }
    if (dotnetInternals[InternalExchangeIndex.InternalUpdatesCallbacks] === undefined) {
        dotnetInternals[InternalExchangeIndex.InternalUpdatesCallbacks] = [];
    }
    const updates = dotnetInternals[InternalExchangeIndex.InternalUpdatesCallbacks];
    if (subscriber && !updates.includes(subscriber)) {
        updates.push(subscriber);
    }
    for (const subscriber of dotnetInternals[InternalExchangeIndex.InternalUpdatesCallbacks]) {
        subscriber(dotnetInternals);
    }
}

export function dotnetUpdateInternalsSubscriber() {
    /**
     * Functions below allow our JS modules to exchange internal interfaces by passing tables of functions in known order instead of using string symbols.
     * IMPORTANT: If you need to add more functions, make sure that you add them at the end of the table, so that the order of existing functions does not change.
     */

    if (Object.keys(dotnetLoaderExports).length === 0 && dotnetInternals[InternalExchangeIndex.LoaderExportsTable]) {
        dotnetLoaderExports = {} as LoaderExports;
        dotnetLogger = {} as LoggerType;
        dotnetAssert = {} as AssertType;
        loaderExportsFromTable(dotnetInternals[InternalExchangeIndex.LoaderExportsTable], dotnetLogger, dotnetAssert, dotnetLoaderExports);
    }
    if (Object.keys(dotnetRuntimeExports).length === 0 && dotnetInternals[InternalExchangeIndex.RuntimeExportsTable]) {
        dotnetRuntimeExports = {} as RuntimeExports;
        runtimeExportsFromTable(dotnetInternals[InternalExchangeIndex.RuntimeExportsTable], dotnetRuntimeExports);
    }
    if (Object.keys(dotnetBrowserHostExports).length === 0 && dotnetInternals[InternalExchangeIndex.BrowserHostExportsTable]) {
        dotnetBrowserHostExports = {} as BrowserHostExports;
        browserHostExportsFromTable(dotnetInternals[InternalExchangeIndex.BrowserHostExportsTable], dotnetBrowserHostExports);
    }
    if (Object.keys(dotnetInteropJSExports).length === 0 && dotnetInternals[InternalExchangeIndex.InteropJavaScriptExportsTable]) {
        dotnetInteropJSExports = {} as InteropJavaScriptExports;
        interopJavaScriptExportsFromTable(dotnetInternals[InternalExchangeIndex.InteropJavaScriptExportsTable], dotnetInteropJSExports);
    }
    if (Object.keys(dotnetNativeBrowserExports).length === 0 && dotnetInternals[InternalExchangeIndex.NativeBrowserExportsTable]) {
        dotnetNativeBrowserExports = {} as NativeBrowserExports;
        nativeBrowserExportsFromTable(dotnetInternals[InternalExchangeIndex.NativeBrowserExportsTable], dotnetNativeBrowserExports);
    }

    // keep in sync with runtimeExportsToTable()
    function runtimeExportsFromTable(table:RuntimeExportsTable, runtime:RuntimeExports):void {
        Object.assign(runtime, {
        });
    }

    // keep in sync with loaderExportsToTable()
    function loaderExportsFromTable(table:LoaderExportsTable, logger:LoggerType, assert:AssertType, dotnetLoaderExports:LoaderExports):void {
        const loggerLocal :LoggerType = {
            info: table[0],
            warn: table[1],
            error: table[2],
        };
        const assertLocal :AssertType = {
            check: table[3],
        };
        const loaderExportsLocal :LoaderExports = {
            resolveRunMainPromise: table[4],
            rejectRunMainPromise: table[5],
            getRunMainPromise: table[6],
        };
        Object.assign(dotnetLoaderExports, loaderExportsLocal);
        Object.assign(logger, loggerLocal);
        Object.assign(assert, assertLocal);
    }

    // keep in sync with browserHostExportsToTable()
    function browserHostExportsFromTable(table:BrowserHostExportsTable, native:BrowserHostExports):void {
        const nativeLocal :BrowserHostExports = {
            registerDllBytes: table[0],
            isSharedArrayBuffer: table[1],
        };
        Object.assign(native, nativeLocal);
    }

    // keep in sync with interopJavaScriptExportsToTable()
    function interopJavaScriptExportsFromTable(table:InteropJavaScriptExportsTable, interop:InteropJavaScriptExports):void {
        const interopLocal :InteropJavaScriptExports = {
        };
        Object.assign(interop, interopLocal);
    }

    // keep in sync with nativeBrowserExportsToTable()
    function nativeBrowserExportsFromTable(table:NativeBrowserExportsTable, interop:NativeBrowserExports):void {
        const interopLocal :NativeBrowserExports = {
        };
        Object.assign(interop, interopLocal);
    }
}
