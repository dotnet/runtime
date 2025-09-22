// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { registerDllBytes } from "../../../../corehost/browserhost/host/host";
import type { isSharedArrayBuffer } from "../../../../corehost/browserhost/host/memory";
import type { check, error, info, warn } from "../../../../corehost/browserhost/loader/logging";
import type { browserHostResolveMain, browserHostRejectMain, getRunMainPromise } from "../../../../corehost/browserhost/loader/run";

export type EnvironmentType = {
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

export type LoggerType = {
    info: typeof info,
    warn: typeof warn,
    error: typeof error,
}

export type AssertType = {
    check: typeof check,
}

export type LoaderExports = {
    ENVIRONMENT_IS_NODE: ()=> boolean,
    ENVIRONMENT_IS_SHELL: ()=> boolean,
    ENVIRONMENT_IS_WEB: ()=> boolean,
    ENVIRONMENT_IS_WORKER: ()=> boolean,
    ENVIRONMENT_IS_SIDECAR: ()=> boolean,
    browserHostResolveMain:typeof browserHostResolveMain,
    browserHostRejectMain:typeof browserHostRejectMain,
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
    typeof browserHostResolveMain,
    typeof browserHostRejectMain,
    typeof getRunMainPromise,
]

export type HostNativeExports = {
    isSharedArrayBuffer : typeof isSharedArrayBuffer,
    registerDllBytes: typeof registerDllBytes
}

export type HostNativeExportsTable = [
    typeof registerDllBytes,
    typeof isSharedArrayBuffer,
]

export type InteropJavaScriptNativeExports = {
}

export type InteropJavaScriptNativeExportsTable = [
]

export type NativeBrowserExports = {
}

export type NativeBrowserExportsTable = [
]
