// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    assemblyRootFolder?: string, // the subfolder containing managed assemblies and pdbs. This is relative to dotnet.js script.
    assets?: AssetEntry[], // a list of assets to load along with the runtime. each asset is a dictionary-style Object with the following properties:

    /**
     * debugLevel > 0 enables debugging and sets the debug log level to debugLevel
     * debugLevel == 0 disables debugging and enables interpreter optimizations
     * debugLevel < 0 enabled debugging and disables debug logging.
     */
    debugLevel?: number,
    maxParallelDownloads?: number, // we are throttling parallel downloads in order to avoid net::ERR_INSUFFICIENT_RESOURCES on chrome
    globalizationMode?: GlobalizationMode, // configures the runtime's globalization mode
    diagnosticTracing?: boolean // enables diagnostic log messages during startup
    remoteSources?: string[], // additional search locations for assets. Sources will be checked in sequential order until the asset is found. The string "./" indicates to load from the application directory (as with the files in assembly_list), and a fully-qualified URL like "https://example.com/" indicates that asset loads can be attempted from a remote server. Sources must end with a "/".
    environmentVariables?: {
        [i: string]: string;
    }, // dictionary-style Object containing environment variables
    runtimeOptions?: string[], // array of runtime options as strings
    aotProfilerOptions?: AOTProfilerOptions, // dictionary-style Object. If omitted, aot profiler will not be initialized.
    coverageProfilerOptions?: CoverageProfilerOptions, // dictionary-style Object. If omitted, coverage profiler will not be initialized.
    ignorePdbLoadErrors?: boolean,
    waitForDebugger?: number
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
    virtualPath?: string, // if specified, overrides the path of the asset in the virtual filesystem and similar data structures once loaded.
    culture?: string,
    loadRemote?: boolean, // if true, an attempt will be made to load the asset from each location in @args.remoteSources.
    isOptional?: boolean // if true, any failure to load this asset will be ignored.
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
    runtime_interop_module: MonoAssembly;
    runtime_interop_namespace: string;
    runtime_interop_exports_classname: string;
    runtime_interop_exports_class: MonoClass;

    _i52_error_scratch_buffer: Int32Ptr;
    mono_wasm_load_runtime_done: boolean;
    mono_wasm_runtime_is_ready: boolean;
    mono_wasm_bindings_is_ready: boolean;

    loaded_files: string[];
    maxParallelDownloads: number;
    config: MonoConfig;
    diagnosticTracing: boolean;
    waitForDebugger?: number;
    fetch_like: (url: string, init?: RequestInit) => Promise<Response>;
    scriptDirectory?: string
    requirePromise: Promise<Function>
    ExitStatus: ExitStatusError;
    quit: Function,
    locateFile: (path: string, prefix?: string) => string,
    javaScriptExports: JavaScriptExports,
}

export type GlobalizationMode =
    "icu" | // load ICU globalization data from any runtime assets with behavior "icu".
    "invariant" | //  operate in invariant globalization mode.
    "auto" // (default): if "icu" behavior assets are present, use ICU, otherwise invariant.


export type AOTProfilerOptions = {
    writeAt?: string, // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::StopProfile'
    sendTo?: string // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::DumpAotProfileData' (DumpAotProfileData stores the data into INTERNAL.aotProfileData.)
}

export type CoverageProfilerOptions = {
    writeAt?: string, // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::StopProfile'
    sendTo?: string // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::DumpCoverageProfileData' (DumpCoverageProfileData stores the data into INTERNAL.coverage_profile_data.)
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
    connectUrl: string, // websocket URL to connect to.
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

    imports?: any;
    exports?: string[];
    downloadResource?: (request: ResourceRequest) => LoadingResource | undefined
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
    marshaled_imports: any,
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

// in all the exported internals methods, we use the same data structures for stack frame as normal full blow interop
// see src\libraries\System.Runtime.InteropServices.JavaScript\src\System\Runtime\InteropServices\JavaScript\Interop\JavaScriptExports.cs
export interface JavaScriptExports {
    // the marshaled signature is: void ReleaseJSOwnedObjectByGCHandle(GCHandle gcHandle)
    release_js_owned_object_by_gc_handle(gc_handle: GCHandle): void;

    // the marshaled signature is: GCHandle CreateTaskCallback()
    create_task_callback(): GCHandle;

    // the marshaled signature is: void CompleteTask<T>(GCHandle holder, Exception? exceptionResult, T? result)
    complete_task(holder_gc_handle: GCHandle, error?: any, data?: any, res_converter?: MarshalerToCs): void;

    // the marshaled signature is: TRes? CallDelegate<T1,T2,T3TRes>(GCHandle callback, T1? arg1, T2? arg2, T3? arg3)
    call_delegate(callback_gc_handle: GCHandle, arg1_js: any, arg2_js: any, arg3_js: any,
        res_converter?: MarshalerToJs, arg1_converter?: MarshalerToCs, arg2_converter?: MarshalerToCs, arg3_converter?: MarshalerToCs): any;

    // the marshaled signature is: Task<int>? CallEntrypoint(MonoMethod* entrypointPtr, string[] args)
    call_entry_point(entry_point: MonoMethod, args?: string[]): Promise<number>;
}

export type MarshalerToJs = (arg: JSMarshalerArgument, sig?: JSMarshalerType, res_converter?: MarshalerToJs, arg1_converter?: MarshalerToCs, arg2_converter?: MarshalerToCs) => any;
export type MarshalerToCs = (arg: JSMarshalerArgument, value: any, sig?: JSMarshalerType, res_converter?: MarshalerToCs, arg1_converter?: MarshalerToJs, arg2_converter?: MarshalerToJs) => void;

export interface JSMarshalerArguments extends NativePointer {
    __brand: "JSMarshalerArguments"
}

export interface JSFunctionSignature extends NativePointer {
    __brand: "JSFunctionSignatures"
}

export interface JSMarshalerType extends NativePointer {
    __brand: "JSMarshalerType"
}

export interface JSMarshalerArgument extends NativePointer {
    __brand: "JSMarshalerArgument"
}

export type MemOffset = number | VoidPtr | NativePointer | ManagedPointer;
export type NumberOrPointer = number | VoidPtr | NativePointer | ManagedPointer;

export interface WasmRoot<T extends MonoObject> {
    get_address(): MonoObjectRef;
    get_address_32(): number;
    get address(): MonoObjectRef;
    get(): T;
    set(value: T): T;
    get value(): T;
    set value(value: T);
    copy_from_address(source: MonoObjectRef): void;
    copy_to_address(destination: MonoObjectRef): void;
    copy_from(source: WasmRoot<T>): void;
    copy_to(destination: WasmRoot<T>): void;
    valueOf(): T;
    clear(): void;
    release(): void;
    toString(): string;
}

export interface WasmRootBuffer {
    get_address(index: number): MonoObjectRef
    get_address_32(index: number): number
    get(index: number): ManagedPointer
    set(index: number, value: ManagedPointer): ManagedPointer
    copy_value_from_address(index: number, sourceAddress: MonoObjectRef): void
    clear(): void;
    release(): void;
    toString(): string;
}
