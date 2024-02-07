// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssetEntry, DotnetModuleConfig, LoadBootResourceCallback, LoadingResource, MonoConfig, RuntimeAPI, SingleAssetBehaviors } from ".";
import type { PThreadLibrary } from "../pthreads/shared/emscripten-internals";
import type { CharPtr, EmscriptenModule, ManagedPointer, NativePointer, VoidPtr, Int32Ptr } from "./emscripten";

export type GCHandle = {
    __brand: "GCHandle"
}
export type JSHandle = {
    __brand: "JSHandle"
}
export type JSFnHandle = {
    __brand: "JSFnHandle"
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

// when adding new fields, please consider if it should be impacting the snapshot hash. If not, please drop it in the snapshot getCacheKey()
export type MonoConfigInternal = MonoConfig & {
    linkerEnabled?: boolean,
    assets?: AssetEntryInternal[],
    runtimeOptions?: string[], // array of runtime options as strings
    aotProfilerOptions?: AOTProfilerOptions, // dictionary-style Object. If omitted, aot profiler will not be initialized.
    browserProfilerOptions?: BrowserProfilerOptions, // dictionary-style Object. If omitted, browser profiler will not be initialized.
    waitForDebugger?: number,
    appendElementOnExit?: boolean
    assertAfterExit?: boolean // default true for shell/nodeJS
    interopCleanupOnExit?: boolean
    logExitCode?: boolean
    forwardConsoleLogsToWS?: boolean,
    asyncFlushOnExit?: boolean
    exitOnUnhandledError?: boolean
    exitAfterSnapshot?: number
    loadAllSatelliteResources?: boolean
    runtimeId?: number

    // related to config hash
    preferredIcuAsset?: string | null,
    resourcesHash?: string,
    GitHash?: string,
    ProductVersion?: string,
};

export type RunArguments = {
    applicationArguments?: string[],
    virtualWorkingDirectory?: string[],
    environmentVariables?: { [name: string]: string },
    runtimeOptions?: string[],
    diagnosticTracing?: boolean,
}

export interface AssetEntryInternal extends AssetEntry {
    // this could have multiple values in time, because of re-try download logic
    pendingDownloadInternal?: LoadingResource
    noCache?: boolean
    useCredentials?: boolean
}

export type LoaderHelpers = {
    gitHash: string,
    config: MonoConfigInternal;
    diagnosticTracing: boolean;

    maxParallelDownloads: number;
    enableDownloadRetry: boolean;
    assertAfterExit: boolean;

    exitCode: number | undefined;
    exitReason: any;

    loadedFiles: string[],
    _loaded_files: { url: string, file: string }[];
    loadedAssemblies: string[],
    scriptDirectory: string
    scriptUrl: string
    modulesUniqueQuery?: string
    preferredIcuAsset?: string | null,
    invariantMode: boolean,

    actual_downloaded_assets_count: number,
    actual_instantiated_assets_count: number,
    expected_downloaded_assets_count: number,
    expected_instantiated_assets_count: number,

    afterConfigLoaded: PromiseAndController<MonoConfig>,
    allDownloadsQueued: PromiseAndController<void>,
    wasmCompilePromise: PromiseAndController<WebAssembly.Module>,
    runtimeModuleLoaded: PromiseAndController<void>,
    memorySnapshotSkippedOrDone: PromiseAndController<void>,

    is_exited: () => boolean,
    is_runtime_running: () => boolean,
    assert_runtime_running: () => void,
    mono_exit: (exit_code: number, reason?: any) => void,
    createPromiseController: <T>(afterResolve?: () => void, afterReject?: () => void) => PromiseAndController<T>,
    getPromiseController: <T>(promise: ControllablePromise<T>) => PromiseController<T>,
    assertIsControllablePromise: <T>(promise: Promise<T>) => asserts promise is ControllablePromise<T>,
    mono_download_assets: () => Promise<void>,
    resolve_single_asset_path: (behavior: SingleAssetBehaviors) => AssetEntryInternal,
    setup_proxy_console: (id: string, console: Console, origin: string) => void
    mono_set_thread_name: (tid: string) => void
    fetch_like: (url: string, init?: RequestInit) => Promise<Response>;
    locateFile: (path: string, prefix?: string) => string,
    out(message: string): void;
    err(message: string): void;

    retrieve_asset_download(asset: AssetEntry): Promise<ArrayBuffer>;
    onDownloadResourceProgress?: (resourcesLoaded: number, totalResources: number) => void;
    logDownloadStatsToConsole: () => void;
    installUnhandledErrorHandler: () => void;
    purgeUnusedCacheEntriesAsync: () => Promise<void>;

    loadBootResource?: LoadBootResourceCallback;
    invokeLibraryInitializers: (functionName: string, args: any[]) => Promise<void>,
    libraryInitializers?: { scriptName: string, exports: any }[];

    isChromium: boolean,
    isFirefox: boolean

    // from wasm-feature-detect npm package
    exceptions: () => Promise<boolean>,
    simd: () => Promise<boolean>,
}
export type RuntimeHelpers = {
    gitHash: string,
    moduleGitHash: string,
    config: MonoConfigInternal;
    diagnosticTracing: boolean;

    runtime_interop_module: MonoAssembly;
    runtime_interop_namespace: string;
    runtime_interop_exports_classname: string;
    runtime_interop_exports_class: MonoClass;

    _i52_error_scratch_buffer: Int32Ptr;
    mono_wasm_runtime_is_ready: boolean;
    mono_wasm_bindings_is_ready: boolean;

    loadedMemorySnapshotSize?: number,
    enablePerfMeasure: boolean;
    waitForDebugger?: number;
    ExitStatus: ExitStatusError;
    quit: Function,
    nativeExit: (code: number) => void,
    nativeAbort: (reason: any) => void,
    javaScriptExports: JavaScriptExports,
    storeMemorySnapshotPending: boolean,
    memorySnapshotCacheKey: string,
    subtle: SubtleCrypto | null,
    updateMemoryViews: () => void
    getMemory(): WebAssembly.Memory,
    getWasmIndirectFunctionTable(): WebAssembly.Table,
    runtimeReady: boolean,
    proxy_context_gc_handle: GCHandle,
    cspPolicy: boolean,

    allAssetsInMemory: PromiseAndController<void>,
    dotnetReady: PromiseAndController<any>,
    afterInstantiateWasm: PromiseAndController<void>,
    beforePreInit: PromiseAndController<void>,
    afterPreInit: PromiseAndController<void>,
    afterPreRun: PromiseAndController<void>,
    beforeOnRuntimeInitialized: PromiseAndController<void>,
    afterOnRuntimeInitialized: PromiseAndController<void>,
    afterPostRun: PromiseAndController<void>,

    featureWasmEh: boolean,
    featureWasmSimd: boolean,

    //core
    stringify_as_error_with_stack?: (error: any) => string,
    instantiate_asset: (asset: AssetEntry, url: string, bytes: Uint8Array) => void,
    instantiate_symbols_asset: (pendingAsset: AssetEntryInternal) => Promise<void>,
    instantiate_segmentation_rules_asset: (pendingAsset: AssetEntryInternal) => Promise<void>,
    jiterpreter_dump_stats?: (x: boolean) => string,
    forceDisposeProxies: (disposeMethods: boolean, verbose: boolean) => void,
}

export type AOTProfilerOptions = {
    writeAt?: string, // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::StopProfile'
    sendTo?: string // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::DumpAotProfileData' (DumpAotProfileData stores the data into INTERNAL.aotProfileData.)
}

export type BrowserProfilerOptions = {
}

// how we extended emscripten Module
export type DotnetModule = EmscriptenModule & DotnetModuleConfig;
export type DotnetModuleInternal = EmscriptenModule & DotnetModuleConfig & EmscriptenModuleInternal;

// Evaluates whether a value is nullish (same definition used as the ?? operator,
//  https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/Nullish_coalescing_operator)
export function is_nullish<T>(value: T | null | undefined): value is null | undefined {
    return (value === undefined) || (value === null);
}

export type EmscriptenInternals = {
    isPThread: boolean,
    linkerWasmEnableSIMD: boolean,
    linkerWasmEnableEH: boolean,
    linkerEnableAotProfiler: boolean,
    linkerEnableBrowserProfiler: boolean,
    linkerRunAOTCompilation: boolean,
    quit_: Function,
    ExitStatus: ExitStatusError,
    gitHash: string,
    getMemory(): WebAssembly.Memory,
    getWasmIndirectFunctionTable(): WebAssembly.Table,
    updateMemoryViews: () => void,
};
export type GlobalObjects = {
    mono: any,
    binding: any,
    internal: any,
    module: DotnetModuleInternal,
    loaderHelpers: LoaderHelpers,
    runtimeHelpers: RuntimeHelpers,
    api: RuntimeAPI,
};
export type EmscriptenReplacements = {
    fetch: any,
    require: any,
    modulePThread: PThreadLibrary | undefined | null
    scriptDirectory: string;
    ENVIRONMENT_IS_WORKER: boolean;
}
export interface ExitStatusError {
    new(status: number): any;
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

    // the marshaled signature is: void CompleteTask<T>(GCHandle holder, Exception? exceptionResult, T? result)
    complete_task(holder_gc_handle: GCHandle, isCanceling: boolean, error?: any, data?: any, res_converter?: MarshalerToCs): void;

    // the marshaled signature is: TRes? CallDelegate<T1,T2,T3TRes>(GCHandle callback, T1? arg1, T2? arg2, T3? arg3)
    call_delegate(callback_gc_handle: GCHandle, arg1_js: any, arg2_js: any, arg3_js: any,
        res_converter?: MarshalerToJs, arg1_converter?: MarshalerToCs, arg2_converter?: MarshalerToCs, arg3_converter?: MarshalerToCs): any;

    // the marshaled signature is: Task<int>? CallEntrypoint(MonoMethod* entrypointPtr, string[] args)
    call_entry_point(entry_point: MonoMethod, args?: string[]): Promise<number>;

    // the marshaled signature is: void InstallMainSynchronizationContext()
    install_main_synchronization_context(): void;

    // the marshaled signature is: string GetManagedStackTrace(GCHandle exception)
    get_managed_stack_trace(exception_gc_handle: GCHandle): string | null

    // the marshaled signature is: void LoadSatelliteAssembly(byte[] dll)
    load_satellite_assembly(dll: Uint8Array): void;

    // the marshaled signature is: void LoadLazyAssembly(byte[] dll, byte[] pdb)
    load_lazy_assembly(dll: Uint8Array, pdb: Uint8Array | null): void;
}

export type MarshalerToJs = (arg: JSMarshalerArgument, element_type?: MarshalerType, res_converter?: MarshalerToJs, arg1_converter?: MarshalerToCs, arg2_converter?: MarshalerToCs, arg3_converter?: MarshalerToCs) => any;
export type MarshalerToCs = (arg: JSMarshalerArgument, value: any, element_type?: MarshalerType, res_converter?: MarshalerToCs, arg1_converter?: MarshalerToJs, arg2_converter?: MarshalerToJs, arg3_converter?: MarshalerToJs) => void;
export type BoundMarshalerToJs = (args: JSMarshalerArguments) => any;
export type BoundMarshalerToCs = (args: JSMarshalerArguments, value: any) => void;
// please keep in sync with src\libraries\System.Runtime.InteropServices.JavaScript\src\System\Runtime\InteropServices\JavaScript\MarshalerType.cs
export enum MarshalerType {
    None = 0,
    Void = 1,
    Discard,
    Boolean,
    Byte,
    Char,
    Int16,
    Int32,
    Int52,
    BigInt64,
    Double,
    Single,
    IntPtr,
    JSObject,
    Object,
    String,
    Exception,
    DateTime,
    DateTimeOffset,

    Nullable,
    Task,
    Array,
    ArraySegment,
    Span,
    Action,
    Function,

    // only on runtime
    JSException,
    TaskResolved,
    TaskRejected,
    TaskPreCreated,
}

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

export declare interface EmscriptenModuleInternal {
    HEAP8: Int8Array,
    HEAP16: Int16Array;
    HEAP32: Int32Array;
    HEAP64: BigInt64Array;
    HEAPU8: Uint8Array;
    HEAPU16: Uint16Array;
    HEAPU32: Uint32Array;
    HEAPF32: Float32Array;
    HEAPF64: Float64Array;

    __locateFile?: (path: string, prefix?: string) => string;
    locateFile?: (path: string, prefix?: string) => string;
    mainScriptUrlOrBlob?: string;
    ENVIRONMENT_IS_PTHREAD?: boolean;
    FS: any;
    wasmModule: WebAssembly.Instance | null;
    ready: Promise<unknown>;
    asm: any;
    getWasmTableEntry(index: number): any;
    removeRunDependency(id: string): void;
    addRunDependency(id: string): void;
    onConfigLoaded?: (config: MonoConfig, api: RuntimeAPI) => void | Promise<void>;
    safeSetTimeout(func: Function, timeout: number): number;
    runtimeKeepalivePush(): void;
    runtimeKeepalivePop(): void;
    maybeExit(): void;
}

/// A PromiseController encapsulates a Promise together with easy access to its resolve and reject functions.
/// It's a bit like a TaskCompletionSource in .NET
export interface PromiseController<T = any> {
    isDone: boolean;
    readonly promise: Promise<T>;
    resolve: (value: T | PromiseLike<T>) => void;
    reject: (reason?: any) => void;
}


/// A Promise<T> with a controller attached
export interface ControllablePromise<T = any> extends Promise<T> {
    __brand: "ControllablePromise"
}

/// Just a pair of a promise and its controller
export interface PromiseAndController<T> {
    promise: ControllablePromise<T>;
    promise_control: PromiseController<T>;
}

export type passEmscriptenInternalsType = (internals: EmscriptenInternals) => void;
export type setGlobalObjectsType = (globalObjects: GlobalObjects) => void;
export type initializeExportsType = (globalObjects: GlobalObjects) => RuntimeAPI;
export type initializeReplacementsType = (replacements: EmscriptenReplacements) => void;
export type configureEmscriptenStartupType = (module: DotnetModuleInternal) => void;
export type configureRuntimeStartupType = () => Promise<void>;
export type configureWorkerStartupType = (module: DotnetModuleInternal) => Promise<void>


export type RuntimeModuleExportsInternal = {
    setRuntimeGlobals: setGlobalObjectsType,
    initializeExports: initializeExportsType,
    initializeReplacements: initializeReplacementsType,
    configureRuntimeStartup: configureRuntimeStartupType,
    configureEmscriptenStartup: configureEmscriptenStartupType,
    configureWorkerStartup: configureWorkerStartupType,
    passEmscriptenInternals: passEmscriptenInternalsType,
}

export type NativeModuleExportsInternal = {
    default: (unificator: Function) => EmscriptenModuleInternal
}

export type WeakRefInternal<T extends object> = WeakRef<T> & {
    dispose?: () => void
}

/// a symbol that we use as a key on messages on the global worker-to-main channel to identify our own messages
/// we can't use an actual JS Symbol because those don't transfer between workers.
export const monoMessageSymbol = "__mono_message__";

export const enum WorkerToMainMessageType {
    monoRegistered = "monoRegistered",
    monoAttached = "monoAttached",
    enabledInterop = "notify_enabled_interop",
    monoUnRegistered = "monoUnRegistered",
    pthreadCreated = "pthreadCreated",
    preload = "preload",
}

export const enum MainToWorkerMessageType {
    applyConfig = "apply_mono_config",
}
