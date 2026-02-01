// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetModuleConfig, RuntimeAPI, AssetEntry, LoaderConfig } from "./public-api";
import type { EmscriptenModule, InstantiateWasmCallBack, ManagedPointer, NativePointer, VoidPtr } from "./emscripten";
import { InteropJavaScriptExportsTable, LoaderExportsTable, BrowserHostExportsTable, RuntimeExportsTable, NativeBrowserExportsTable, BrowserUtilsExportsTable, DiagnosticsExportsTable } from "./exchange";

export type GCHandle = {
    __brand: "GCHandle"
}
export type JSHandle = {
    __brand: "JSHandle"
}
export type JSFnHandle = {
    __brand: "JSFnHandle"
}
export interface JSMarshalerArguments extends NativePointer {
    __brand: "JSMarshalerArguments"
}
export type CSFnHandle = {
    __brand: "CSFnHandle"
}

export type MemOffset = number | VoidPtr | NativePointer | ManagedPointer;
export type NumberOrPointer = number | VoidPtr | NativePointer | ManagedPointer;

// how we extended emscripten Module
export type DotnetModule = EmscriptenModule & DotnetModuleConfig;
export type DotnetModuleInternal = EmscriptenModule & DotnetModuleConfig & EmscriptenModuleInternal;

// these are values from the last re-link with emcc/workload
export type EmscriptenBuildOptions = {
    gitHash: string,
};

export type EmscriptenInternals = {
    isPThread: boolean,
    quit_: Function,
    ExitStatus: new (status: number) => any,
    gitHash: string,
    getMemory(): WebAssembly.Memory,
    getWasmIndirectFunctionTable(): WebAssembly.Table,
    updateMemoryViews: () => void,
};

export type EmscriptenModuleInternal = EmscriptenModule & DotnetModuleConfig & {
    runtimeKeepalivePush(): void;
    runtimeKeepalivePop(): void;
    instantiateWasm?: InstantiateWasmCallBack;
    onAbort?: (reason: any, extraJson?: string) => void;
    onExit?: (code: number) => void;
}

export interface AssetEntryInternal extends AssetEntry {
    integrity?: string
    cache?: RequestCache
    useCredentials?: boolean
}

export type LoaderConfigInternal = LoaderConfig & {
    runtimeOptions?: string[], // array of runtime options as strings
    appendElementOnExit?: boolean
    logExitCode?: boolean
    exitOnUnhandledError?: boolean
    loadAllSatelliteResources?: boolean
    forwardConsole?: boolean,
    asyncFlushOnExit?: boolean
    interopCleanupOnExit?: boolean
};


/// A Promise<T> with a controller attached
export interface ControllablePromise<T = any> extends Promise<T> {
    __brand: "ControllablePromise"
}

/// Just a pair of a promise and its controller
export interface PromiseCompletionSource<T> {
    readonly promise: ControllablePromise<T>;
    isDone: boolean;
    resolve: (value: T | PromiseLike<T>) => void;
    reject: (reason?: any) => void;
    propagateFrom: (other: Promise<T>) => void;
}

export type InternalExchangeSubscriber = (internals: InternalExchange) => void;

export type InternalExchange = [
    RuntimeAPI, //0
    InternalExchangeSubscriber[], //1
    LoaderConfigInternal, //2
    LoaderExportsTable, //3
    RuntimeExportsTable, //4
    BrowserHostExportsTable, //5
    InteropJavaScriptExportsTable, //6
    NativeBrowserExportsTable, //7
    BrowserUtilsExportsTable, //8
    DiagnosticsExportsTable, //9
]
export const enum InternalExchangeIndex {
    RuntimeAPI = 0,
    InternalUpdatesCallbacks = 1,
    LoaderConfig = 2,
    LoaderExportsTable = 3,
    RuntimeExportsTable = 4,
    BrowserHostExportsTable = 5,
    InteropJavaScriptExportsTable = 6,
    NativeBrowserExportsTable = 7,
    BrowserUtilsExportsTable = 8,
    DiagnosticsExportsTable = 9,
}

export type JsModuleExports = {
    dotnetInitializeModule<T>(internals: InternalExchange): Promise<T>;
};

export type OnExitListener = (exitCode: number, reason: any, silent: boolean) => boolean;
