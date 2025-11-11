// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { installVfsFile, registerDllBytes } from "../../../../corehost/browserhost/host/host";
import type { check, error, info, warn, debug } from "../../../../corehost/browserhost/loader/logging";
import type { createPromiseCompletionSource, getPromiseCompletionSource, isControllablePromise } from "../../../../corehost/browserhost/loader/promise-completion-source";
import type { resolveRunMainPromise, rejectRunMainPromise, getRunMainPromise } from "../../../../corehost/browserhost/loader/run";
import type { zeroRegion } from "../../../System.Native.Browser/utils/memory";
import type { stringToUTF16, stringToUTF16Ptr, stringToUTF8Ptr, utf16ToString } from "../../../System.Native.Browser/utils/strings";
import type { bindJSImportST, invokeJSFunction, invokeJSImportST } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/invoke-js";
import type { releaseCSOwnedObject } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/gc-handles";
import type { resolveOrRejectPromise } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/marshal-to-js";
import type { cancelPromise } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/cancelable-promise";

export type RuntimeExports = {
    bindJSImportST: typeof bindJSImportST,
    invokeJSImportST: typeof invokeJSImportST,
    releaseCSOwnedObject: typeof releaseCSOwnedObject,
    resolveOrRejectPromise: typeof resolveOrRejectPromise,
    cancelPromise: typeof cancelPromise,
    invokeJSFunction: typeof invokeJSFunction,
}

export type RuntimeExportsTable = [
    typeof bindJSImportST,
    typeof invokeJSImportST,
    typeof releaseCSOwnedObject,
    typeof resolveOrRejectPromise,
    typeof cancelPromise,
    typeof invokeJSFunction,
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
}

export type BrowserHostExportsTable = [
    typeof registerDllBytes,
    typeof installVfsFile,
]

export type InteropJavaScriptExports = {
    SystemInteropJS_GetManagedStackTrace: typeof _SystemInteropJS_GetManagedStackTrace,
}

export type InteropJavaScriptExportsTable = [
    typeof _SystemInteropJS_GetManagedStackTrace,
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
}

export type BrowserUtilsExportsTable = [
    typeof utf16ToString,
    typeof stringToUTF16,
    typeof stringToUTF16Ptr,
    typeof stringToUTF8Ptr,
    typeof zeroRegion,
]
