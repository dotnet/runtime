// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { check, error, info, warn, debug, fastCheck } from "../../../../corehost/browserhost/loader/logging";
import type { resolveRunMainPromise, rejectRunMainPromise, getRunMainPromise, abortStartup } from "../../../../corehost/browserhost/loader/run";
import type { addOnExitListener, isExited, isRuntimeRunning, quitNow } from "../../../../corehost/browserhost/loader/exit";

import type { installVfsFile, registerDllBytes, loadIcuData, initializeCoreCLR, registerPdbBytes } from "../../../../corehost/browserhost/host/host";
import type { createPromiseCompletionSource, getPromiseCompletionSource, isControllablePromise } from "../../../../corehost/browserhost/loader/promise-completion-source";

import type { isSharedArrayBuffer, zeroRegion } from "../../../System.Native.Browser/utils/memory";
import type { stringToUTF16, stringToUTF16Ptr, stringToUTF8Ptr, utf16ToString } from "../../../System.Native.Browser/utils/strings";
import type { abortPosix, abortTimers, getExitStatus } from "../../../System.Native.Browser/utils/host";

import type { symbolicateStackTrace } from "../../../System.Native.Browser/diagnostics/symbolicate";

export type RuntimeExports = {
}

export type RuntimeExportsTable = [
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
]

export type BrowserHostExports = {
    registerDllBytes: typeof registerDllBytes
    installVfsFile: typeof installVfsFile
    loadIcuData: typeof loadIcuData
    initializeCoreCLR: typeof initializeCoreCLR
    registerPdbBytes: typeof registerPdbBytes
}

export type BrowserHostExportsTable = [
    typeof registerDllBytes,
    typeof installVfsFile,
    typeof loadIcuData,
    typeof initializeCoreCLR,
    typeof registerPdbBytes,
]

export type InteropJavaScriptExports = {
}

export type InteropJavaScriptExportsTable = [
]

export type NativeBrowserExports = {
}

export type NativeBrowserExportsTable = [
]

export type BrowserUtilsExports = {
    utf16ToString: typeof utf16ToString,
    stringToUTF16: typeof stringToUTF16,
    stringToUTF16Ptr: typeof stringToUTF16Ptr,
    stringToUTF8Ptr: typeof stringToUTF8Ptr,
    zeroRegion: typeof zeroRegion,
    isSharedArrayBuffer: typeof isSharedArrayBuffer
    abortTimers: typeof abortTimers,
    abortPosix: typeof abortPosix,
    getExitStatus: typeof getExitStatus,
}

export type BrowserUtilsExportsTable = [
    typeof utf16ToString,
    typeof stringToUTF16,
    typeof stringToUTF16Ptr,
    typeof stringToUTF8Ptr,
    typeof zeroRegion,
    typeof isSharedArrayBuffer,
    typeof abortTimers,
    typeof abortPosix,
    typeof getExitStatus,
]

export type DiagnosticsExportsTable = [
    typeof symbolicateStackTrace,
]

export type DiagnosticsExports = {
    symbolicateStackTrace: typeof symbolicateStackTrace,
}
