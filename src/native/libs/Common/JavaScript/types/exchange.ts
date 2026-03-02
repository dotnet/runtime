// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { check, error, info, warn, debug, fastCheck, normalizeException } from "../loader/logging";
import type { resolveRunMainPromise, rejectRunMainPromise, getRunMainPromise, abortStartup } from "../loader/run";
import type { addOnExitListener, isExited, isRuntimeRunning, quitNow } from "../loader/exit";

import type { initializeCoreCLR } from "../host/host";
import type { instantiateWasm, installVfsFile, registerDllBytes, loadIcuData, registerPdbBytes, instantiateWebcilModule } from "../host/assets";
import type { createPromiseCompletionSource, getPromiseCompletionSource, isControllablePromise } from "../loader/promise-completion-source";
import type { fetchSatelliteAssemblies, fetchLazyAssembly } from "../loader/assets";

import type { isSharedArrayBuffer, zeroRegion } from "../../../System.Native.Browser/utils/memory";
import type { stringToUTF16, stringToUTF16Ptr, stringToUTF8, stringToUTF8Ptr, utf16ToString } from "../../../System.Native.Browser/utils/strings";
import type { abortPosix, abortBackgroundTimers, getExitStatus, runBackgroundTimers } from "../../../System.Native.Browser/utils/host";
import type { bindJSImportST, invokeJSFunction, invokeJSImportST } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/invoke-js";
import type { forceDisposeProxies, releaseCSOwnedObject } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/gc-handles";
import type { resolveOrRejectPromise } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/marshal-to-js";
import type { cancelPromise } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/cancelable-promise";
import type { abortInteropTimers } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/scheduling";

import type { installNativeSymbols, symbolicateStackTrace } from "../../../System.Native.Browser/diagnostics/symbolicate";
import type { EmsAmbientSymbolsType } from "../types";


type getWasmMemoryType = () => WebAssembly.Memory;
type getWasmTableType = () => WebAssembly.Table;

export type RuntimeExports = {
    bindJSImportST: typeof bindJSImportST,
    invokeJSImportST: typeof invokeJSImportST,
    releaseCSOwnedObject: typeof releaseCSOwnedObject,
    resolveOrRejectPromise: typeof resolveOrRejectPromise,
    cancelPromise: typeof cancelPromise,
    invokeJSFunction: typeof invokeJSFunction,
    forceDisposeProxies: typeof forceDisposeProxies,
    abortInteropTimers: typeof abortInteropTimers,
}

export type RuntimeExportsTable = [
    typeof bindJSImportST,
    typeof invokeJSImportST,
    typeof releaseCSOwnedObject,
    typeof resolveOrRejectPromise,
    typeof cancelPromise,
    typeof invokeJSFunction,
    typeof forceDisposeProxies,
    typeof abortInteropTimers,
]

export type LoggerType = {
    debug: typeof debug,
    info: typeof info,
    warn: typeof warn,
    error: typeof error,
}

export type AssertType = {
    check: typeof check,
    fastCheck: typeof fastCheck,
}

export type LoaderExports = {
    resolveRunMainPromise: typeof resolveRunMainPromise,
    rejectRunMainPromise: typeof rejectRunMainPromise,
    getRunMainPromise: typeof getRunMainPromise,
    createPromiseCompletionSource: typeof createPromiseCompletionSource,
    isControllablePromise: typeof isControllablePromise,
    getPromiseCompletionSource: typeof getPromiseCompletionSource,
    isExited: typeof isExited,
    isRuntimeRunning: typeof isRuntimeRunning,
    addOnExitListener: typeof addOnExitListener,
    abortStartup: typeof abortStartup,
    quitNow: typeof quitNow,
    normalizeException: typeof normalizeException,
    fetchSatelliteAssemblies: typeof fetchSatelliteAssemblies,
    fetchLazyAssembly: typeof fetchLazyAssembly,
}

export type LoaderExportsTable = [
    typeof debug,
    typeof info,
    typeof warn,
    typeof error,
    typeof check,
    typeof fastCheck,
    typeof resolveRunMainPromise,
    typeof rejectRunMainPromise,
    typeof getRunMainPromise,
    typeof createPromiseCompletionSource,
    typeof isControllablePromise,
    typeof getPromiseCompletionSource,
    typeof isExited,
    typeof isRuntimeRunning,
    typeof addOnExitListener,
    typeof abortStartup,
    typeof quitNow,
    typeof normalizeException,
    typeof fetchSatelliteAssemblies,
    typeof fetchLazyAssembly,
]

export type BrowserHostExports = {
    registerDllBytes: typeof registerDllBytes
    installVfsFile: typeof installVfsFile
    loadIcuData: typeof loadIcuData
    initializeCoreCLR: typeof initializeCoreCLR
    registerPdbBytes: typeof registerPdbBytes
    instantiateWasm: typeof instantiateWasm
    instantiateWebcilModule: typeof instantiateWebcilModule
}

export type BrowserHostExportsTable = [
    typeof registerDllBytes,
    typeof installVfsFile,
    typeof loadIcuData,
    typeof initializeCoreCLR,
    typeof registerPdbBytes,
    typeof instantiateWasm,
    typeof instantiateWebcilModule,
]

export type InteropJavaScriptExports = {
    SystemInteropJS_GetManagedStackTrace: EmsAmbientSymbolsType["_SystemInteropJS_GetManagedStackTrace"],
    SystemInteropJS_CallDelegate: EmsAmbientSymbolsType["_SystemInteropJS_CallDelegate"],
    SystemInteropJS_CompleteTask: EmsAmbientSymbolsType["_SystemInteropJS_CompleteTask"],
    SystemInteropJS_ReleaseJSOwnedObjectByGCHandle: EmsAmbientSymbolsType["_SystemInteropJS_ReleaseJSOwnedObjectByGCHandle"],
    SystemInteropJS_BindAssemblyExports: EmsAmbientSymbolsType["_SystemInteropJS_BindAssemblyExports"],
    SystemInteropJS_CallJSExport: EmsAmbientSymbolsType["_SystemInteropJS_CallJSExport"],
}

export type InteropJavaScriptExportsTable = [
    EmsAmbientSymbolsType["_SystemInteropJS_GetManagedStackTrace"],
    EmsAmbientSymbolsType["_SystemInteropJS_CallDelegate"],
    EmsAmbientSymbolsType["_SystemInteropJS_CompleteTask"],
    EmsAmbientSymbolsType["_SystemInteropJS_ReleaseJSOwnedObjectByGCHandle"],
    EmsAmbientSymbolsType["_SystemInteropJS_BindAssemblyExports"],
    EmsAmbientSymbolsType["_SystemInteropJS_CallJSExport"],
]

export type NativeBrowserExports = {
    getWasmMemory: getWasmMemoryType,
    getWasmTable: getWasmTableType,
}

export type NativeBrowserExportsTable = [
    getWasmMemoryType,
    getWasmTableType,
]

export type BrowserUtilsExports = {
    utf16ToString: typeof utf16ToString,
    stringToUTF16: typeof stringToUTF16,
    stringToUTF16Ptr: typeof stringToUTF16Ptr,
    stringToUTF8Ptr: typeof stringToUTF8Ptr,
    stringToUTF8: typeof stringToUTF8,
    zeroRegion: typeof zeroRegion,
    isSharedArrayBuffer: typeof isSharedArrayBuffer
    abortBackgroundTimers: typeof abortBackgroundTimers,
    abortPosix: typeof abortPosix,
    getExitStatus: typeof getExitStatus,
    runBackgroundTimers: typeof runBackgroundTimers,
}

export type BrowserUtilsExportsTable = [
    typeof utf16ToString,
    typeof stringToUTF16,
    typeof stringToUTF16Ptr,
    typeof stringToUTF8Ptr,
    typeof stringToUTF8,
    typeof zeroRegion,
    typeof isSharedArrayBuffer,
    typeof abortBackgroundTimers,
    typeof abortPosix,
    typeof getExitStatus,
    typeof runBackgroundTimers,
]

export type DiagnosticsExportsTable = [
    typeof symbolicateStackTrace,
    typeof installNativeSymbols,
]

export type DiagnosticsExports = {
    symbolicateStackTrace: typeof symbolicateStackTrace,
    installNativeSymbols: typeof installNativeSymbols,
}
