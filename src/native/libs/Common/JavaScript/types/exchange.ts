// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { registerDllBytes } from "../../../../corehost/browserhost/host/host";
import type { isSharedArrayBuffer } from "../../../../corehost/browserhost/host/memory";
import type { check, error, info, warn } from "../../../../corehost/browserhost/loader/logging";
import type { resolveRunMainPromise, rejectRunMainPromise, getRunMainPromise } from "../../../../corehost/browserhost/loader/run";

export type JSEngineType = {
    IS_NODE: boolean,
    IS_SHELL: boolean,
    IS_WEB: boolean,
    IS_WORKER: boolean,
    IS_SIDECAR: boolean,
}

export type RuntimeExports = {
}

export type RuntimeExportsTable = [
]

export type dotnetLoggerType = {
    info: typeof info,
    warn: typeof warn,
    error: typeof error,
}

export type dotnetAssertType = {
    check: typeof check,
}

export type LoaderExports = {
    ENVIRONMENT_IS_NODE: ()=> boolean,
    ENVIRONMENT_IS_SHELL: ()=> boolean,
    ENVIRONMENT_IS_WEB: ()=> boolean,
    ENVIRONMENT_IS_WORKER: ()=> boolean,
    ENVIRONMENT_IS_SIDECAR: ()=> boolean,
    resolveRunMainPromise:typeof resolveRunMainPromise,
    rejectRunMainPromise:typeof rejectRunMainPromise,
    getRunMainPromise:typeof getRunMainPromise,
}

export type LoaderExportsTable = [
    typeof info,
    typeof warn,
    typeof error,
    typeof check,
    ()=> boolean,
    ()=> boolean,
    ()=> boolean,
    ()=> boolean,
    ()=> boolean,
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
