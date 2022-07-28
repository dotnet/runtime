// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import "node/buffer"; // we use the Buffer type to type some of Emscripten's APIs
import { JavaScriptExports } from "./managed-exports";
import { BINDINGType, MONOType } from "./net6-legacy/exports-legacy";
import { CharPtr, EmscriptenModule, ManagedPointer, NativePointer, VoidPtr, Int32Ptr } from "./types/emscripten";

export type GCHandle = {
    __brand: "GCHandle"
}
export type JSHandle = {
    __brand: "JSHandle"
}
export interface MonoObject extends ManagedPointer {
    __brandMonoObject: "MonoObject"
}
export interface MonoString extends MonoObject {
    __brand: "MonoString"
}
export interface MonoClass extends MonoObject {
    __brand: "MonoClass"
}
export interface MonoType extends ManagedPointer {
    __brand: "MonoType"
}
export interface MonoMethod extends ManagedPointer {
    __brand: "MonoMethod"
}
export interface MonoArray extends MonoObject {
    __brand: "MonoArray"
}
export interface MonoAssembly extends MonoObject {
    __brand: "MonoAssembly"
}
// Pointer to a MonoObject* (i.e. the address of a root)
export interface MonoObjectRef extends ManagedPointer {
    __brandMonoObjectRef: "MonoObjectRef"
}
// This exists for signature clarity, we need it to be structurally equivalent
//  so that anything requiring MonoObjectRef will work
// eslint-disable-next-line @typescript-eslint/no-empty-interface
export interface MonoStringRef extends MonoObjectRef {
}
export const MonoMethodNull: MonoMethod = <MonoMethod><any>0;
export const MonoObjectNull: MonoObject = <MonoObject><any>0;
export const MonoArrayNull: MonoArray = <MonoArray><any>0;
export const MonoAssemblyNull: MonoAssembly = <MonoAssembly><any>0;
export const MonoClassNull: MonoClass = <MonoClass><any>0;
export const MonoTypeNull: MonoType = <MonoType><any>0;
export const MonoStringNull: MonoString = <MonoString><any>0;
export const MonoObjectRefNull: MonoObjectRef = <MonoObjectRef><any>0;
export const MonoStringRefNull: MonoStringRef = <MonoStringRef><any>0;
export const JSHandleDisposed: JSHandle = <JSHandle><any>-1;
export const JSHandleNull: JSHandle = <JSHandle><any>0;
export const GCHandleNull: GCHandle = <GCHandle><any>0;
export const VoidPtrNull: VoidPtr = <VoidPtr><any>0;
export const CharPtrNull: CharPtr = <CharPtr><any>0;
export const NativePointerNull: NativePointer = <NativePointer><any>0;

export function coerceNull<T extends ManagedPointer | NativePointer>(ptr: T | null | undefined): T {
    if ((ptr === null) || (ptr === undefined))
        return (0 as any) as T;
    else
        return ptr as T;
}

export type MonoConfig = {
    isError?: false,
    assembly_root?: string, // the subfolder containing managed assemblies and pdbs. This is relative to dotnet.js script.
    assets?: AssetEntry[], // a list of assets to load along with the runtime. each asset is a dictionary-style Object with the following properties:

    /**
     * Either this or enable_debugging needs to be set
     * debug_level > 0 enables debugging and sets the debug log level to debug_level
     * debug_level == 0 disables debugging and enables interpreter optimizations
     * debug_level < 0 enabled debugging and disables debug logging.
     */
    debug_level?: number,
    enable_debugging?: number, // Either this or debug_level needs to be set
    globalization_mode?: GlobalizationMode, // configures the runtime's globalization mode
    diagnostic_tracing?: boolean // enables diagnostic log messages during startup
    remote_sources?: string[], // additional search locations for assets. Sources will be checked in sequential order until the asset is found. The string "./" indicates to load from the application directory (as with the files in assembly_list), and a fully-qualified URL like "https://example.com/" indicates that asset loads can be attempted from a remote server. Sources must end with a "/".
    environment_variables?: {
        [i: string]: string;
    }, // dictionary-style Object containing environment variables
    runtime_options?: string[], // array of runtime options as strings
    aot_profiler_options?: AOTProfilerOptions, // dictionary-style Object. If omitted, aot profiler will not be initialized.
    coverage_profiler_options?: CoverageProfilerOptions, // dictionary-style Object. If omitted, coverage profiler will not be initialized.
    diagnostic_options?: DiagnosticOptions, // dictionary-style Object. If omitted, diagnostics will not be initialized.
    ignore_pdb_load_errors?: boolean,
    wait_for_debugger?: number
};

export type MonoConfigError = {
    isError: true,
    message: string,
    error: any
}

export interface ResourceRequest {
    name: string, // the name of the asset, including extension.
    behavior: AssetBehaviours, // determines how the asset will be handled once loaded
    resolvedUrl?: string;
    hash?: string;
}

// Types of assets that can be in the mono-config.js/mono-config.json file (taken from /src/tasks/WasmAppBuilder/WasmAppBuilder.cs)
export interface AssetEntry extends ResourceRequest {
    virtual_path?: string, // if specified, overrides the path of the asset in the virtual filesystem and similar data structures once loaded.
    culture?: string,
    load_remote?: boolean, // if true, an attempt will be made to load the asset from each location in @args.remote_sources.
    is_optional?: boolean // if true, any failure to load this asset will be ignored.
    buffer?: ArrayBuffer // if provided, we don't have to fetch it
    pending?: LoadingResource // if provided, we don't have to start fetching it
}

export type AssetBehaviours =
    "resource" // load asset as a managed resource assembly
    | "assembly" // load asset as a managed assembly 
    | "pdb" // load asset as a managed debugging information
    | "heap" // store asset into the native heap
    | "icu" // load asset as an ICU data archive
    | "vfs" // load asset into the virtual filesystem (for fopen, File.Open, etc)
    | "dotnetwasm"; // the binary of the dotnet runtime

export type RuntimeHelpers = {
    get_call_sig_ref: MonoMethod;
    complete_task_method: MonoMethod;
    create_task_method: MonoMethod;
    call_delegate: MonoMethod;
    runtime_interop_module: MonoAssembly;
    runtime_interop_namespace: string;
    runtime_interop_exports_classname: string;
    runtime_interop_exports_class: MonoClass;
    runtime_legacy_exports_classname: string;
    runtime_legacy_exports_class: MonoClass;

    _box_buffer_size: number;
    _unbox_buffer_size: number;

    _box_buffer: VoidPtr;
    _unbox_buffer: VoidPtr;
    _i52_error_scratch_buffer: Int32Ptr;
    _box_root: any;
    // A WasmRoot that is guaranteed to contain 0
    _null_root: any;
    _class_int32: MonoClass;
    _class_uint32: MonoClass;
    _class_double: MonoClass;
    _class_boolean: MonoClass;
    mono_wasm_load_runtime_done: boolean;
    mono_wasm_runtime_is_ready: boolean;
    mono_wasm_bindings_is_ready: boolean;

    loaded_files: string[];
    config: MonoConfig;
    diagnostic_tracing: boolean;
    enable_debugging: number;
    wait_for_debugger?: number;
    fetch_like: (url: string, init?: RequestInit) => Promise<Response>;
    scriptDirectory?: string
    requirePromise: Promise<Function>
    ExitStatus: ExitStatusError;
    quit: Function,
    locateFile: (path: string, prefix?: string) => string,
    javaScriptExports: JavaScriptExports,
}

export const wasm_type_symbol = Symbol.for("wasm type");

export type GlobalizationMode =
    "icu" | // load ICU globalization data from any runtime assets with behavior "icu".
    "invariant" | //  operate in invariant globalization mode.
    "auto" // (default): if "icu" behavior assets are present, use ICU, otherwise invariant.


export type AOTProfilerOptions = {
    write_at?: string, // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::StopProfile'
    send_to?: string // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::DumpAotProfileData' (DumpAotProfileData stores the data into INTERNAL.aot_profile_data.)
}

export type CoverageProfilerOptions = {
    write_at?: string, // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::StopProfile'
    send_to?: string // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::DumpCoverageProfileData' (DumpCoverageProfileData stores the data into INTERNAL.coverage_profile_data.)
}

/// Options to configure EventPipe sessions that will be created and started at runtime startup
export type DiagnosticOptions = {
    /// An array of sessions to start at runtime startup
    sessions?: EventPipeSessionOptions[],
    /// If true, the diagnostic server will be started.  If "wait", the runtime will wait at startup until a diagnsotic session connects to the server
    server?: DiagnosticServerOptions,
}

/// Options to configure the event pipe session
/// The recommended method is to MONO.diagnostics.SesisonOptionsBuilder to create an instance of this type
export interface EventPipeSessionOptions {
    /// Whether to collect additional details (such as method and type names) at EventPipeSession.stop() time (default: true)
    /// This is required for some use cases, and may allow some tools to better understand the events.
    collectRundownEvents?: boolean;
    /// The providers that will be used by this session.
    /// See https://docs.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables
    providers: string;
}

/// Options to configure the diagnostic server
export type DiagnosticServerOptions = {
    connect_url: string, // websocket URL to connect to.
    suspend: string | boolean, // if true, the server will suspend the app when it starts until a diagnostic tool tells the runtime to resume.
}
// how we extended emscripten Module
export type DotnetModule = EmscriptenModule & DotnetModuleConfig;

export type DotnetModuleConfig = {
    disableDotnet6Compatibility?: boolean,

    config?: MonoConfig,
    configSrc?: string,
    onConfigLoaded?: (config: MonoConfig) => void | Promise<void>;
    onDotnetReady?: () => void | Promise<void>;

    imports?: DotnetModuleConfigImports;
    exports?: string[];
    downloadResource?: (request: ResourceRequest) => LoadingResource
} & Partial<EmscriptenModule>

export type DotnetModuleConfigImports = {
    require?: (name: string) => any;
    fetch?: (url: string) => Promise<Response>;
    fs?: {
        promises?: {
            readFile?: (path: string) => Promise<string | Buffer>,
        }
        readFileSync?: (path: string, options: any | undefined) => string,
    };
    crypto?: {
        randomBytes?: (size: number) => Buffer
    };
    ws?: WebSocket & { Server: any };
    path?: {
        normalize?: (path: string) => string,
        dirname?: (path: string) => string,
    };
    url?: any;
}

export interface LoadingResource {
    name: string;
    url: string;
    response: Promise<Response>;
}


// see src\mono\wasm\runtime\rollup.config.js
// inline this, because the lambda could allocate closure on hot path otherwise
export function mono_assert(condition: unknown, messageFactory: string | (() => string)): asserts condition {
    if (!condition) {
        const message = typeof messageFactory === "string"
            ? messageFactory
            : messageFactory();
        throw new Error(`Assert failed: ${message}`);
    }
}

// see src/mono/wasm/driver.c MARSHAL_TYPE_xxx and Runtime.cs MarshalType
export const enum MarshalType {
    NULL = 0,
    INT = 1,
    FP64 = 2,
    STRING = 3,
    VT = 4,
    DELEGATE = 5,
    TASK = 6,
    OBJECT = 7,
    BOOL = 8,
    ENUM = 9,
    URI = 22,
    SAFEHANDLE = 23,
    ARRAY_BYTE = 10,
    ARRAY_UBYTE = 11,
    ARRAY_UBYTE_C = 12,
    ARRAY_SHORT = 13,
    ARRAY_USHORT = 14,
    ARRAY_INT = 15,
    ARRAY_UINT = 16,
    ARRAY_FLOAT = 17,
    ARRAY_DOUBLE = 18,
    FP32 = 24,
    UINT32 = 25,
    INT64 = 26,
    UINT64 = 27,
    CHAR = 28,
    STRING_INTERNED = 29,
    VOID = 30,
    ENUM64 = 31,
    POINTER = 32,
    SPAN_BYTE = 33,
}

// see src/mono/wasm/driver.c MARSHAL_ERROR_xxx and Runtime.cs
export const enum MarshalError {
    BUFFER_TOO_SMALL = 512,
    NULL_CLASS_POINTER = 513,
    NULL_TYPE_POINTER = 514,
    UNSUPPORTED_TYPE = 515,
    FIRST = BUFFER_TOO_SMALL
}

// Evaluates whether a value is nullish (same definition used as the ?? operator,
//  https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/Nullish_coalescing_operator)
export function is_nullish<T>(value: T | null | undefined): value is null | undefined {
    return (value === undefined) || (value === null);
}

export type EarlyImports = {
    isGlobal: boolean,
    isNode: boolean,
    isWorker: boolean,
    isShell: boolean,
    isWeb: boolean,
    isPThread: boolean,
    quit_: Function,
    ExitStatus: ExitStatusError,
    requirePromise: Promise<Function>
};
export type EarlyExports = {
    mono: any,
    binding: any,
    internal: any,
    module: any,
    marshaled_exports: any,
    marshaled_imports: any
};
export type EarlyReplacements = {
    fetch: any,
    require: any,
    requirePromise: Promise<Function>,
    noExitRuntime: boolean,
    updateGlobalBufferAndViews: Function,
    pthreadReplacements: PThreadReplacements | undefined | null
    scriptDirectory: string;
    scriptUrl: string
}
export interface ExitStatusError {
    new(status: number): any;
}
export type PThreadReplacements = {
    loadWasmModuleToWorker: Function,
    threadInitTLS: Function
}

/// Always throws. Used to handle unreachable switch branches when TypeScript refines the type of a variable
/// to 'never' after you handle all the cases it knows about.
export function assertNever(x: never): never {
    throw new Error("Unexpected value: " + x);
}

/// returns true if the given value is not Thenable
///
/// Useful if some function returns a value or a promise of a value.
export function notThenable<T>(x: T | PromiseLike<T>): x is T {
    return typeof x !== "object" || typeof ((<PromiseLike<T>>x).then) !== "function";
}

/// An identifier for an EventPipe session. The id is unique during the lifetime of the runtime.
/// Primarily intended for debugging purposes.
export type EventPipeSessionID = bigint;

// this represents visibility in the javascript
// like https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoTypes.ts
export interface DotnetPublicAPI {
    MONO: MONOType,
    BINDING: BINDINGType,
    INTERNAL: any,
    EXPORTS: any,
    IMPORTS: any,
    Module: EmscriptenModule,
    RuntimeId: number,
    RuntimeBuildInfo: {
        ProductVersion: string,
        Configuration: string,
    }
}