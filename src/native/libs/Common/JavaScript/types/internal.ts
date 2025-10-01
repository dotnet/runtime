// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetModuleConfig, RuntimeAPI, AssetEntry, LoaderConfig, LoadingResource } from "./public-api";
import type { EmscriptenModule, ManagedPointer, NativePointer, VoidPtr } from "./emscripten";
import { InteropJavaScriptExportsTable as InteropJavaScriptExportsTable, LoaderExportsTable, BrowserHostExportsTable, RuntimeExportsTable, NativeBrowserExportsTable, BrowserUtilsExportsTable } from "./exchange";

export type GCHandle = {
    __brand: "GCHandle"
}
export type JSHandle = {
    __brand: "JSHandle"
}
export type JSFnHandle = {
    __brand: "JSFnHandle"
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

export declare interface EmscriptenModuleInternal extends EmscriptenModule {
    HEAP8: Int8Array,
    HEAP16: Int16Array;
    HEAP32: Int32Array;
    HEAP64: BigInt64Array;
    HEAPU8: Uint8Array;
    HEAPU16: Uint16Array;
    HEAPU32: Uint32Array;
    HEAPF32: Float32Array;
    HEAPF64: Float64Array;

    locateFile?: (path: string, prefix?: string) => string;
    mainScriptUrlOrBlob?: string;
    ENVIRONMENT_IS_PTHREAD?: boolean;
    FS: any;
    wasmModule: WebAssembly.Instance | null;
    ready: Promise<unknown>;
    wasmExports: any;
    getWasmTableEntry(index: number): any;
    removeRunDependency(id: string): void;
    addRunDependency(id: string): void;
    safeSetTimeout(func: Function, timeout: number): number;
    runtimeKeepalivePush(): void;
    runtimeKeepalivePop(): void;
    maybeExit(): void;
    print(message: string): void;
    printErr(message: string): void;
    abort(reason: any): void;
    _emscripten_force_exit(exit_code: number): void;
}

export interface AssetEntryInternal extends AssetEntry {
    // this could have multiple values in time, because of re-try download logic
    pendingDownloadInternal?: LoadingResource
    noCache?: boolean
    useCredentials?: boolean
    isCore?: boolean
}

export type LoaderConfigInternal = LoaderConfig & {
    linkerEnabled?: boolean,
    runtimeOptions?: string[], // array of runtime options as strings
    appendElementOnExit?: boolean
    logExitCode?: boolean
    exitOnUnhandledError?: boolean
    loadAllSatelliteResources?: boolean
    resourcesHash?: string,
};


/// A Promise<T> with a controller attached
export interface ControllablePromise<T = any> extends Promise<T> {
    __brand: "ControllablePromise"
}

/// Just a pair of a promise and its controller
export interface PromiseController<T> {
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
}

export type JsModuleExports = {
    dotnetInitializeModule<T>(internals: InternalExchange): Promise<T>;
};

