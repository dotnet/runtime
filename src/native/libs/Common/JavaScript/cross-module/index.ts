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

import type { DotnetModuleInternal, InternalExchange, RuntimeExports, LoaderExports, RuntimeAPI, LoggerType, AssertType, BrowserHostExports, InteropJavaScriptExports, LoaderExportsTable, RuntimeExportsTable, BrowserHostExportsTable, InteropJavaScriptExportsTable, NativeBrowserExports, NativeBrowserExportsTable, InternalExchangeSubscriber, BrowserUtilsExports, BrowserUtilsExportsTable, VoidPtr, CharPtr, NativePointer, DiagnosticsExports, DiagnosticsExportsTable } from "../types";
import { InternalExchangeIndex } from "../types";

export let dotnetInternals: InternalExchange;
export let Module: DotnetModuleInternal;
export let dotnetApi: RuntimeAPI;
export const dotnetLogger: LoggerType = {} as LoggerType;
export const dotnetAssert: AssertType = {} as AssertType;
export const dotnetLoaderExports: LoaderExports = {} as any;
export const dotnetRuntimeExports: RuntimeExports = {} as any;
export const dotnetBrowserHostExports: BrowserHostExports = {} as any;
export const dotnetInteropJSExports: InteropJavaScriptExports = {} as any;
export const dotnetNativeBrowserExports: NativeBrowserExports = {} as any;
export const dotnetBrowserUtilsExports: BrowserUtilsExports = {} as any;
export const dotnetDiagnosticsExports: DiagnosticsExports = {} as any;

export const VoidPtrNull: VoidPtr = <VoidPtr><any>0;
export const CharPtrNull: CharPtr = <CharPtr><any>0;
export const NativePointerNull: NativePointer = <NativePointer><any>0;

export function dotnetGetInternals(): InternalExchange {
    return dotnetInternals;
}

// this should be called when we want to dispatch new internal functions to other JS modules
// subscriber parameter is the callback function with visibility to the current module's internal closure
export function dotnetUpdateInternals(internals: InternalExchange, subscriber?: InternalExchangeSubscriber) {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");
    if (!Array.isArray(internals[InternalExchangeIndex.InternalUpdatesCallbacks])) throw new Error("Expected internal updates to be an array");
    if (dotnetInternals === undefined) {
        dotnetInternals = internals;
    } else if (dotnetInternals !== internals) {
        throw new Error("Cannot replace internals");
    }
    if (dotnetApi === undefined) {
        dotnetApi = dotnetInternals[InternalExchangeIndex.RuntimeAPI];
    }
    if (typeof dotnetApi !== "object") throw new Error("Expected internals to have RuntimeAPI");
    if (Module === undefined) {
        Module = dotnetApi.Module as any;
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
        loaderExportsFromTable(dotnetInternals[InternalExchangeIndex.LoaderExportsTable], dotnetLogger, dotnetAssert, dotnetLoaderExports);
    }
    if (Object.keys(dotnetRuntimeExports).length === 0 && dotnetInternals[InternalExchangeIndex.RuntimeExportsTable]) {
        runtimeExportsFromTable(dotnetInternals[InternalExchangeIndex.RuntimeExportsTable], dotnetRuntimeExports);
    }
    if (Object.keys(dotnetBrowserHostExports).length === 0 && dotnetInternals[InternalExchangeIndex.BrowserHostExportsTable]) {
        browserHostExportsFromTable(dotnetInternals[InternalExchangeIndex.BrowserHostExportsTable], dotnetBrowserHostExports);
    }
    if (Object.keys(dotnetBrowserUtilsExports).length === 0 && dotnetInternals[InternalExchangeIndex.BrowserUtilsExportsTable]) {
        nativeHelperExportsFromTable(dotnetInternals[InternalExchangeIndex.BrowserUtilsExportsTable], dotnetBrowserUtilsExports);
    }
    if (Object.keys(dotnetInteropJSExports).length === 0 && dotnetInternals[InternalExchangeIndex.InteropJavaScriptExportsTable]) {
        interopJavaScriptExportsFromTable(dotnetInternals[InternalExchangeIndex.InteropJavaScriptExportsTable], dotnetInteropJSExports);
    }
    if (Object.keys(dotnetNativeBrowserExports).length === 0 && dotnetInternals[InternalExchangeIndex.NativeBrowserExportsTable]) {
        nativeBrowserExportsFromTable(dotnetInternals[InternalExchangeIndex.NativeBrowserExportsTable], dotnetNativeBrowserExports);
    }
    if (Object.keys(dotnetDiagnosticsExports).length === 0 && dotnetInternals[InternalExchangeIndex.DiagnosticsExportsTable]) {
        diagnosticsExportsFromTable(dotnetInternals[InternalExchangeIndex.DiagnosticsExportsTable], dotnetDiagnosticsExports);
    }

    // keep in sync with runtimeExportsToTable()
    function runtimeExportsFromTable(table: RuntimeExportsTable, runtime: RuntimeExports): void {
        const runtimerLocal: RuntimeExports = {
            bindJSImportST: table[0],
            invokeJSImportST: table[1],
            releaseCSOwnedObject: table[2],
            resolveOrRejectPromise: table[3],
            cancelPromise: table[4],
            invokeJSFunction: table[5],
            forceDisposeProxies: table[6],
            abortInteropTimers: table[7],
        };
        Object.assign(runtime, runtimerLocal);
    }

    // keep in sync with loaderExportsToTable()
    function loaderExportsFromTable(table: LoaderExportsTable, logger: LoggerType, assert: AssertType, dotnetLoaderExports: LoaderExports): void {
        const loggerLocal: LoggerType = {
            debug: table[0],
            info: table[1],
            warn: table[2],
            error: table[3],
        };
        const assertLocal: AssertType = {
            check: table[4],
            fastCheck: table[5],
        };
        const loaderExportsLocal: LoaderExports = {
            resolveRunMainPromise: table[6],
            rejectRunMainPromise: table[7],
            getRunMainPromise: table[8],
            createPromiseCompletionSource: table[9],
            isControllablePromise: table[10],
            getPromiseCompletionSource: table[11],
            isExited: table[12],
            isRuntimeRunning: table[13],
            addOnExitListener: table[14],
            abortStartup: table[15],
            quitNow: table[16],
        };
        Object.assign(dotnetLoaderExports, loaderExportsLocal);
        Object.assign(logger, loggerLocal);
        Object.assign(assert, assertLocal);
    }

    // keep in sync with browserHostExportsToTable()
    function browserHostExportsFromTable(table: BrowserHostExportsTable, native: BrowserHostExports): void {
        const nativeLocal: BrowserHostExports = {
            registerDllBytes: table[0],
            installVfsFile: table[1],
            loadIcuData: table[2],
            initializeCoreCLR: table[3],
            registerPdbBytes: table[4],
        };
        Object.assign(native, nativeLocal);
    }

    // keep in sync with interopJavaScriptExportsToTable()
    function interopJavaScriptExportsFromTable(table: InteropJavaScriptExportsTable, interop: InteropJavaScriptExports): void {
        const interopLocal: InteropJavaScriptExports = {
            SystemInteropJS_GetManagedStackTrace: table[0],
            SystemInteropJS_CallDelegate: table[1],
            SystemInteropJS_CompleteTask: table[2],
            SystemInteropJS_ReleaseJSOwnedObjectByGCHandle: table[3],
            SystemInteropJS_BindAssemblyExports: table[4],
            SystemInteropJS_CallJSExport: table[5],
        };
        Object.assign(interop, interopLocal);
    }

    // keep in sync with nativeBrowserExportsToTable()
    function nativeBrowserExportsFromTable(table: NativeBrowserExportsTable, interop: NativeBrowserExports): void {
        const interopLocal: NativeBrowserExports = {
        };
        Object.assign(interop, interopLocal);
    }

    // keep in sync with nativeBrowserExportsToTable()
    function diagnosticsExportsFromTable(table: DiagnosticsExportsTable, interop: DiagnosticsExports): void {
        const interopLocal: DiagnosticsExports = {
            symbolicateStackTrace: table[0],
        };
        Object.assign(interop, interopLocal);
    }

    // keep in sync with nativeHelperExportsToTable()
    function nativeHelperExportsFromTable(table: BrowserUtilsExportsTable, interop: BrowserUtilsExports): void {
        const interopLocal: BrowserUtilsExports = {
            utf16ToString: table[0],
            stringToUTF16: table[1],
            stringToUTF16Ptr: table[2],
            stringToUTF8Ptr: table[3],
            stringToUTF8: table[4],
            zeroRegion: table[5],
            isSharedArrayBuffer: table[6],
            abortBackgroundTimers: table[7],
            abortPosix: table[8],
            getExitStatus: table[9],
            runBackgroundTimers: table[10],
        };
        Object.assign(interop, interopLocal);
    }
}
