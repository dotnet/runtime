// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type {
    AssertType, LoggerType,
    EmscriptenModuleInternal, InternalExchange, InternalExchangeSubscriber,
    RuntimeAPI, LoaderExports, BrowserUtilsExports, RuntimeExports,
    VoidPtr, JSMarshalerArguments, CSFnHandle, TypedArray,
    MemOffset, CharPtrPtr
} from "../types";

// we want to use the cross-module symbols defined in closure of dotnet.native.js
// which are installed there by libSystem.Native.Browser.Utils.footer.js
// see also `reserved` in `rollup.config.defines.js`

export type EmsAmbientSymbolsType = EmscriptenModuleInternal & {
    dotnetApi: RuntimeAPI;
    dotnetAssert: AssertType;
    dotnetLogger: LoggerType;
    dotnetLoaderExports: LoaderExports;
    dotnetRuntimeExports: RuntimeExports;
    dotnetBrowserUtilsExports: BrowserUtilsExports;

    dotnetUpdateInternals: (internals?: Partial<InternalExchange>, subscriber?: InternalExchangeSubscriber) => void;
    dotnetUpdateInternalsSubscriber: (internals: InternalExchange) => void;

    _GetDotNetRuntimeContractDescriptor: () => void;
    _SystemJS_ExecuteTimerCallback: () => void;
    _SystemJS_ExecuteBackgroundJobCallback: () => void;
    _BrowserHost_CreateHostContract: () => VoidPtr;
    _BrowserHost_InitializeCoreCLR: (propertiesCount: number, propertyKeys: CharPtrPtr, propertyValues: CharPtrPtr) => number;
    _BrowserHost_ExecuteAssembly: (mainAssemblyNamePtr: number, argsLength: number, argsPtr: number) => number;
    _wasm_load_icu_data: (dataPtr: VoidPtr) => number;
    _SystemInteropJS_GetManagedStackTrace: (args: JSMarshalerArguments) => void;
    _SystemInteropJS_CallDelegate: (args: JSMarshalerArguments) => void;
    _SystemInteropJS_CompleteTask: (args: JSMarshalerArguments) => void;
    _SystemInteropJS_ReleaseJSOwnedObjectByGCHandle: (args: JSMarshalerArguments) => void;
    _SystemInteropJS_BindAssemblyExports: (args: JSMarshalerArguments) => void;
    _SystemInteropJS_CallJSExport: (methodHandle: CSFnHandle, args: JSMarshalerArguments) => void;

    FS: {
        createPath: (parent: string, path: string, canRead?: boolean, canWrite?: boolean) => string;
        createDataFile: (parent: string, name: string, data: TypedArray, canRead: boolean, canWrite: boolean, canOwn?: boolean) => string;
    }

    DOTNET: any;
    DOTNET_INTEROP: any;
    BROWSER_HOST: any;

    Module: EmscriptenModuleInternal;
    ENVIRONMENT_IS_NODE: boolean;
    ENVIRONMENT_IS_SHELL: boolean;
    ENVIRONMENT_IS_WEB: boolean;
    ENVIRONMENT_IS_WORKER: boolean;
    ENVIRONMENT_IS_SIDECAR: boolean;
    ABORT: boolean;
    EXITSTATUS: number;

    HEAP8: Int8Array,
    HEAP16: Int16Array;
    HEAP32: Int32Array;
    HEAP64: BigInt64Array;
    HEAPU8: Uint8Array;
    HEAPU16: Uint16Array;
    HEAPU32: Uint32Array;
    HEAPF32: Float32Array;
    HEAPF64: Float64Array;

    ExitStatus: (exitCode: number) => number;
    _emscripten_force_exit: (exitCode: number) => void;
    _exit: (exitCode: number, implicit?: boolean) => void;
    safeSetTimeout: (func: Function, timeout: number) => number;
    exitJS: (status: number, implicit?: boolean | number) => void;
    runtimeKeepalivePop: () => void;
    runtimeKeepalivePush: () => void;

    writeI53ToI64(ptr: MemOffset, value: number): void;
    readI53FromI64(ptr: MemOffset): number;
    readI53FromU64(ptr: MemOffset): number;
}
