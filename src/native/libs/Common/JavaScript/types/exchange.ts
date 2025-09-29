// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { registerDllBytes } from "../../../../corehost/browserhost/host/host";
import type { isSharedArrayBuffer } from "../../../../corehost/browserhost/host/memory";
import type { check, error, info, warn } from "../../../../corehost/browserhost/loader/logging";
import type { resolveRunMainPromise, rejectRunMainPromise, getRunMainPromise } from "../../../../corehost/browserhost/loader/run";

export type RuntimeExports = {
}

export type RuntimeExportsTable = [
]

export type LoggerType = {
    info: typeof info,
    warn: typeof warn,
    error: typeof error,
}

export type AssertType = {
    check: typeof check,
}

export type LoaderExports = {
    resolveRunMainPromise:typeof resolveRunMainPromise,
    rejectRunMainPromise:typeof rejectRunMainPromise,
    getRunMainPromise:typeof getRunMainPromise,
}

export type LoaderExportsTable = [
    typeof info,
    typeof warn,
    typeof error,
    typeof check,
    typeof resolveRunMainPromise,
    typeof rejectRunMainPromise,
    typeof getRunMainPromise,
]

export type BrowserHostExports = {
    isSharedArrayBuffer : typeof isSharedArrayBuffer,
    registerDllBytes: typeof registerDllBytes
}

export type BrowserHostExportsTable = [
    typeof registerDllBytes,
    typeof isSharedArrayBuffer,
]

export type InteropJavaScriptExports = {
}

export type InteropJavaScriptExportsTable = [
]

export type NativeBrowserExports = {
}

export type NativeBrowserExportsTable = [
]
