// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { installVfsFile, registerDllBytes, loadIcuData } from "../../../../corehost/browserhost/host/host";
import type { check, error, info, warn, debug } from "../../../../corehost/browserhost/loader/logging";
import type { createPromiseCompletionSource, getPromiseCompletionSource, isControllablePromise } from "../../../../corehost/browserhost/loader/promise-completion-source";
import type { resolveRunMainPromise, rejectRunMainPromise, getRunMainPromise } from "../../../../corehost/browserhost/loader/run";
import type { isSharedArrayBuffer, zeroRegion } from "../../../System.Native.Browser/utils/memory";
import type { stringToUTF16, stringToUTF16Ptr, stringToUTF8Ptr, utf16ToString } from "../../../System.Native.Browser/utils/strings";

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
}

export type LoaderExports = {
    resolveRunMainPromise: typeof resolveRunMainPromise,
    rejectRunMainPromise: typeof rejectRunMainPromise,
    getRunMainPromise: typeof getRunMainPromise,
    createPromiseCompletionSource: typeof createPromiseCompletionSource,
    isControllablePromise: typeof isControllablePromise,
    getPromiseCompletionSource: typeof getPromiseCompletionSource,
}

export type LoaderExportsTable = [
    typeof debug,
    typeof info,
    typeof warn,
    typeof error,
    typeof check,
    typeof resolveRunMainPromise,
    typeof rejectRunMainPromise,
    typeof getRunMainPromise,
    typeof createPromiseCompletionSource,
    typeof isControllablePromise,
    typeof getPromiseCompletionSource,
]

export type BrowserHostExports = {
    registerDllBytes: typeof registerDllBytes
    installVfsFile: typeof installVfsFile
    loadIcuData: typeof loadIcuData
}

export type BrowserHostExportsTable = [
    typeof registerDllBytes,
    typeof installVfsFile,
    typeof loadIcuData,
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
}

export type BrowserUtilsExportsTable = [
    typeof utf16ToString,
    typeof stringToUTF16,
    typeof stringToUTF16Ptr,
    typeof stringToUTF8Ptr,
    typeof zeroRegion,
    typeof isSharedArrayBuffer,
]
