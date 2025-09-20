// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { isSharedArrayBuffer } from "../../../System.Native.Browser/memory";
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
    browserHostResolveMain:typeof browserHostResolveMain,
    browserHostRejectMain:typeof browserHostRejectMain,
    getRunMainPromise:typeof getRunMainPromise,
}

export type LoaderExportsTable = [
    typeof info,
    typeof warn,
    typeof error,
    typeof check,
    typeof browserHostResolveMain,
    typeof browserHostRejectMain,
    typeof getRunMainPromise,
]

type registerDllBytesType = (bytes: Uint8Array, asset: { name: string })=>void;

export type NativeExports = {
    isSharedArrayBuffer : typeof isSharedArrayBuffer,
    registerDllBytes: registerDllBytesType
}

export type NativeExportsTable = [
    registerDllBytesType,
    typeof isSharedArrayBuffer,
]

export type InteropExports = {
}

export type InteropExportsTable = [
]
