// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssetEntry, DotnetModuleConfig, LoadBootResourceCallback, LoadingResource, MonoConfig, RuntimeAPI, SingleAssetBehaviors } from ".";
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
export type PThreadPtr = {
    __brand: "PThreadPtr" // like pthread_t in C
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
export const GCHandleInvalid: GCHandle = <GCHandle><any>-1;
export const VoidPtrNull: VoidPtr = <VoidPtr><any>0;
export const CharPtrNull: CharPtr = <CharPtr><any>0;
export const NativePointerNull: NativePointer = <NativePointer><any>0;
export const PThreadPtrNull: PThreadPtr = <PThreadPtr><any>0;

export function coerceNull<T extends ManagedPointer | NativePointer>(ptr: T | null | undefined): T {
    if ((ptr === null) || (ptr === undefined))
        return (0 as any) as T;
    else
        return ptr as T;
}

// when adding new fields, please consider if it should be impacting the config hash. If not, please drop it in the getCacheKey()
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
    dumpThreadsOnNonZeroExit?: boolean
    logExitCode?: boolean
    forwardConsoleLogsToWS?: boolean,
    asyncFlushOnExit?: boolean
    exitOnUnhandledError?: boolean
    loadAllSatelliteResources?: boolean
    runtimeId?: number

    // related to config hash
    preferredIcuAsset?: string | null,
    resourcesHash?: string,
    GitHash?: string,
    ProductVersion?: string,

    mainThreadingMode?: MainThreadingMode,
    jsThreadBlockingMode?: JSThreadBlockingMode,
    jsThreadInteropMode?: JSThreadInteropMode,
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
    loadingWorkers: PThreadWorker[],
    workerNextNumber: number,

    actual_downloaded_assets_count: number,
    actual_instantiated_assets_count: number,
    expected_downloaded_assets_count: number,
    expected_instantiated_assets_count: number,

    afterConfigLoaded: PromiseAndController<MonoConfig>,
    allDownloadsQueued: PromiseAndController<void>,
    wasmCompilePromise: PromiseAndController<WebAssembly.Module>,
    runtimeModuleLoaded: PromiseAndController<void>,

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
    set_thread_prefix: (prefix: string) => void
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

    isDebuggingSupported(): boolean,
    isChromium: boolean,
    isFirefox: boolean

    // from wasm-feature-detect npm package
    exceptions: () => Promise<boolean>,
    simd: () => Promise<boolean>,
}
export type RuntimeHelpers = {
    emscriptenBuildOptions: EmscriptenBuildOptions,
    gitHash: string,
    config: MonoConfigInternal;
    diagnosticTracing: boolean;

    runtime_interop_module: MonoAssembly;
    runtime_interop_namespace: string;
    runtime_interop_exports_classname: string;
    runtime_interop_exports_class: MonoClass;

    _i52_error_scratch_buffer: Int32Ptr;
    mono_wasm_runtime_is_ready: boolean;
    mono_wasm_bindings_is_ready: boolean;

    enablePerfMeasure: boolean;
    waitForDebugger?: number;
    ExitStatus: ExitStatusError;
    quit: Function,
    nativeExit: (code: number) => void,
    nativeAbort: (reason: any) => void,
    subtle: SubtleCrypto | null,
    updateMemoryViews: () => void
    getMemory(): WebAssembly.Memory,
    getWasmIndirectFunctionTable(): WebAssembly.Table,
    runtimeReady: boolean,
    monoThreadInfo: PThreadInfo,
    proxyGCHandle: GCHandle | undefined,
    managedThreadTID: PThreadPtr,
    currentThreadTID: PThreadPtr,
    isManagedRunningOnCurrentThread: boolean,
    isPendingSynchronousCall: boolean, // true when we are in the middle of a synchronous call from managed code from same thread
    cspPolicy: boolean,

    allAssetsInMemory: PromiseAndController<void>,
    dotnetReady: PromiseAndController<any>,
    afterInstantiateWasm: PromiseAndController<void>,
    beforePreInit: PromiseAndController<void>,
    afterPreInit: PromiseAndController<void>,
    afterPreRun: PromiseAndController<void>,
    beforeOnRuntimeInitialized: PromiseAndController<void>,
    afterMonoStarted: PromiseAndController<GCHandle | undefined>,
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
    dumpThreads: () => void,
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

// these are values from the last re-link with emcc/workload
export type EmscriptenBuildOptions = {
    wasmEnableSIMD: boolean,
    wasmEnableEH: boolean,
    enableAotProfiler: boolean,
    enableBrowserProfiler: boolean,
    runAOTCompilation: boolean,
    wasmEnableThreads: boolean,
    gitHash: string,
};
export type EmscriptenInternals = {
    isPThread: boolean,
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
    DiscardNoWait,

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
    __emscripten_thread_init(pthread_ptr: PThreadPtr, isMainBrowserThread: number, isMainRuntimeThread: number, canBlock: number): void;
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

export type passEmscriptenInternalsType = (internals: EmscriptenInternals, emscriptenBuildOptions: EmscriptenBuildOptions) => void;
export type setGlobalObjectsType = (globalObjects: GlobalObjects) => void;
export type initializeExportsType = (globalObjects: GlobalObjects) => RuntimeAPI;
export type initializeReplacementsType = (replacements: EmscriptenReplacements) => void;
export type afterInitializeType = (module: EmscriptenModuleInternal) => void;
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
    updateInfo = "updateInfo",
    enabledInterop = "notify_enabled_interop",
    monoUnRegistered = "monoUnRegistered",
    pthreadCreated = "pthreadCreated",
    deputyCreated = "createdDeputy",
    deputyFailed = "deputyFailed",
    deputyStarted = "monoStarted",
    preload = "preload",
}

export const enum MainToWorkerMessageType {
    applyConfig = "apply_mono_config",
}

export interface PThreadWorker extends Worker {
    pthread_ptr: PThreadPtr;
    loaded: boolean;
    // this info is updated via async messages from the worker, it could be stale
    info: PThreadInfo;
    thread?: Thread;
}

export interface PThreadInfo {
    pthreadId: PThreadPtr;

    workerNumber: number,
    reuseCount: number,
    updateCount: number,

    threadName: string,
    threadPrefix: string,

    isLoaded?: boolean,
    isRegistered?: boolean,
    isRunning?: boolean,
    isAttached?: boolean,
    isDeputy?: boolean,
    isExternalEventLoop?: boolean,
    isUI?: boolean;
    isBackground?: boolean,
    isDebugger?: boolean,
    isThreadPoolWorker?: boolean,
    isTimer?: boolean,
    isLongRunning?: boolean,
    isThreadPoolGate?: boolean,
    isFinalizer?: boolean,
    isDirtyBecauseOfInterop?: boolean,
}

export interface PThreadLibrary {
    unusedWorkers: PThreadWorker[];
    runningWorkers: PThreadWorker[];
    pthreads: PThreadInfoMap;
    allocateUnusedWorker: () => void;
    loadWasmModuleToWorker: (worker: PThreadWorker) => Promise<PThreadWorker>;
    threadInitTLS: () => void,
    getNewWorker: () => PThreadWorker,
    returnWorkerToPool: (worker: PThreadWorker) => void,
}

export interface PThreadInfoMap {
    [key: number]: PThreadWorker;
}

export interface Thread {
    readonly pthreadPtr: PThreadPtr;
    readonly port: MessagePort;
    postMessageToWorker<T extends MonoThreadMessage>(message: T): void;
}

export interface MonoThreadMessage {
    // Type of message.  Generally a subsystem like "diagnostic_server", or "event_pipe", "debugger", etc.
    type: string;
    // A particular kind of message. For example, "started", "stopped", "stopped_with_error", etc.
    cmd: string;
}

// keep in sync with JSHostImplementation.Types.cs
export const enum MainThreadingMode {
    // Running the managed main thread on UI thread. 
    // Managed GC and similar scenarios could be blocking the UI. 
    // Easy to deadlock. Not recommended for production.
    UIThread = 0,
    // Running the managed main thread on dedicated WebWorker. Marshaling all JavaScript calls to and from the main thread.
    DeputyThread = 1,
}

// keep in sync with JSHostImplementation.Types.cs
export const enum JSThreadBlockingMode {
    // throw PlatformNotSupportedException if blocking .Wait is called on threads with JS interop, like JSWebWorker and Main thread.
    // Avoids deadlocks (typically with pending JS promises on the same thread) by throwing exceptions.
    NoBlockingWait = 0,
    // allow .Wait on all threads. 
    // Could cause deadlocks with blocking .Wait on a pending JS Task/Promise on the same thread or similar Task/Promise chain.
    AllowBlockingWait = 100,
}

// keep in sync with JSHostImplementation.Types.cs
export const enum JSThreadInteropMode {
    // throw PlatformNotSupportedException if synchronous JSImport/JSExport is called on threads with JS interop, like JSWebWorker and Main thread.
    // calling synchronous JSImport on thread pool or new threads is allowed.
    NoSyncJSInterop = 0,
    // allow non-re-entrant synchronous blocking calls to and from JS on JSWebWorker on threads with JS interop, like JSWebWorker and Main thread.
    // calling synchronous JSImport on thread pool or new threads is allowed.
    SimpleSynchronousJSInterop = 1,
}