//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//!
//! This is generated file, see src/mono/wasm/runtime/rollup.config.js

//! This is not considered public API with backward compatibility guarantees. 

declare interface NativePointer {
    __brandNativePointer: "NativePointer";
}
declare interface VoidPtr extends NativePointer {
    __brand: "VoidPtr";
}
declare interface CharPtr extends NativePointer {
    __brand: "CharPtr";
}
declare interface Int32Ptr extends NativePointer {
    __brand: "Int32Ptr";
}
declare interface EmscriptenModule {
    HEAP8: Int8Array;
    HEAP16: Int16Array;
    HEAP32: Int32Array;
    HEAPU8: Uint8Array;
    HEAPU16: Uint16Array;
    HEAPU32: Uint32Array;
    HEAPF32: Float32Array;
    HEAPF64: Float64Array;
    _malloc(size: number): VoidPtr;
    _free(ptr: VoidPtr): void;
    print(message: string): void;
    printErr(message: string): void;
    ccall<T>(ident: string, returnType?: string | null, argTypes?: string[], args?: any[], opts?: any): T;
    cwrap<T extends Function>(ident: string, returnType: string, argTypes?: string[], opts?: any): T;
    cwrap<T extends Function>(ident: string, ...args: any[]): T;
    setValue(ptr: VoidPtr, value: number, type: string, noSafe?: number | boolean): void;
    setValue(ptr: Int32Ptr, value: number, type: string, noSafe?: number | boolean): void;
    getValue(ptr: number, type: string, noSafe?: number | boolean): number;
    UTF8ToString(ptr: CharPtr, maxBytesToRead?: number): string;
    UTF8ArrayToString(u8Array: Uint8Array, idx?: number, maxBytesToRead?: number): string;
    FS_createPath(parent: string, path: string, canRead?: boolean, canWrite?: boolean): string;
    FS_createDataFile(parent: string, name: string, data: TypedArray, canRead: boolean, canWrite: boolean, canOwn?: boolean): string;
    FS_readFile(filename: string, opts: any): any;
    removeRunDependency(id: string): void;
    addRunDependency(id: string): void;
    stackSave(): VoidPtr;
    stackRestore(stack: VoidPtr): void;
    stackAlloc(size: number): VoidPtr;
    ready: Promise<unknown>;
    instantiateWasm?: (imports: WebAssembly.Imports, successCallback: (instance: WebAssembly.Instance, module: WebAssembly.Module) => void) => any;
    preInit?: (() => any)[] | (() => any);
    preRun?: (() => any)[] | (() => any);
    onRuntimeInitialized?: () => any;
    postRun?: (() => any)[] | (() => any);
    onAbort?: {
        (error: any): void;
    };
}
declare type TypedArray = Int8Array | Uint8Array | Uint8ClampedArray | Int16Array | Uint16Array | Int32Array | Uint32Array | Float32Array | Float64Array;

declare type MonoConfig = {
    assemblyRootFolder?: string;
    assets?: AssetEntry[];
    /**
     * debugLevel > 0 enables debugging and sets the debug log level to debugLevel
     * debugLevel == 0 disables debugging and enables interpreter optimizations
     * debugLevel < 0 enabled debugging and disables debug logging.
     */
    debugLevel?: number;
    globalizationMode?: GlobalizationMode;
    diagnosticTracing?: boolean;
    remoteSources?: string[];
    environmentVariables?: {
        [i: string]: string;
    };
    runtimeOptions?: string[];
    aotProfilerOptions?: AOTProfilerOptions;
    coverageProfilerOptions?: CoverageProfilerOptions;
    diagnosticOptions?: DiagnosticOptions;
    ignorePdbLoadErrors?: boolean;
    waitForDebugger?: number;
};
interface ResourceRequest {
    name: string;
    behavior: AssetBehaviours;
    resolvedUrl?: string;
    hash?: string;
}
interface AssetEntry extends ResourceRequest {
    virtualPath?: string;
    culture?: string;
    loadRemote?: boolean;
    isOptional?: boolean;
    buffer?: ArrayBuffer;
    pending?: LoadingResource;
}
declare type AssetBehaviours = "resource" | "assembly" | "pdb" | "heap" | "icu" | "vfs" | "dotnetwasm";
declare type GlobalizationMode = "icu" | // load ICU globalization data from any runtime assets with behavior "icu".
"invariant" | //  operate in invariant globalization mode.
"auto";
declare type AOTProfilerOptions = {
    writeAt?: string;
    sendTo?: string;
};
declare type CoverageProfilerOptions = {
    writeAt?: string;
    sendTo?: string;
};
declare type DiagnosticOptions = {
    sessions?: EventPipeSessionOptions[];
    server?: DiagnosticServerOptions;
};
interface EventPipeSessionOptions {
    collectRundownEvents?: boolean;
    providers: string;
}
declare type DiagnosticServerOptions = {
    connectUrl: string;
    suspend: string | boolean;
};
declare type DotnetModuleConfig = {
    disableDotnet6Compatibility?: boolean;
    config?: MonoConfig;
    configSrc?: string;
    onConfigLoaded?: (config: MonoConfig) => void | Promise<void>;
    onDotnetReady?: () => void | Promise<void>;
    imports?: DotnetModuleConfigImports;
    exports?: string[];
    downloadResource?: (request: ResourceRequest) => LoadingResource;
} & Partial<EmscriptenModule>;
declare type DotnetModuleConfigImports = {
    require?: (name: string) => any;
    fetch?: (url: string) => Promise<Response>;
    fs?: {
        promises?: {
            readFile?: (path: string) => Promise<string | Buffer>;
        };
        readFileSync?: (path: string, options: any | undefined) => string;
    };
    crypto?: {
        randomBytes?: (size: number) => Buffer;
    };
    ws?: WebSocket & {
        Server: any;
    };
    path?: {
        normalize?: (path: string) => string;
        dirname?: (path: string) => string;
    };
    url?: any;
};
interface LoadingResource {
    name: string;
    url: string;
    response: Promise<Response>;
}
declare type EventPipeSessionID = bigint;

declare const eventLevel: {
    readonly LogAlways: 0;
    readonly Critical: 1;
    readonly Error: 2;
    readonly Warning: 3;
    readonly Informational: 4;
    readonly Verbose: 5;
};
declare type EventLevel = typeof eventLevel;
declare type UnnamedProviderConfiguration = Partial<{
    keywordMask: string | 0;
    level: number;
    args: string;
}>;
interface ProviderConfiguration extends UnnamedProviderConfiguration {
    name: string;
}
declare class SessionOptionsBuilder {
    private _rundown?;
    private _providers;
    constructor();
    static get Empty(): SessionOptionsBuilder;
    static get DefaultProviders(): SessionOptionsBuilder;
    setRundownEnabled(enabled: boolean): SessionOptionsBuilder;
    addProvider(provider: ProviderConfiguration): SessionOptionsBuilder;
    addRuntimeProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder;
    addRuntimePrivateProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder;
    addSampleProfilerProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder;
    build(): EventPipeSessionOptions;
}

interface EventPipeSession {
    get sessionID(): EventPipeSessionID;
    start(): void;
    stop(): void;
    getTraceBlob(): Blob;
}

interface Diagnostics {
    eventLevel: EventLevel;
    SessionOptionsBuilder: typeof SessionOptionsBuilder;
    createEventPipeSession(options?: EventPipeSessionOptions): EventPipeSession | null;
    getStartupSessions(): (EventPipeSession | null)[];
}

interface APIType {
    runMain: (mainAssemblyName: string, args: string[]) => Promise<number>;
    runMainAndExit: (mainAssemblyName: string, args: string[]) => Promise<void>;
    setEnvironmentVariable: (name: string, value: string) => void;
    getAssemblyExports(assemblyName: string): Promise<any>;
    getConfig: () => MonoConfig;
    memory: {
        setB32: (offset: NativePointer, value: number | boolean) => void;
        setU8: (offset: NativePointer, value: number) => void;
        setU16: (offset: NativePointer, value: number) => void;
        setU32: (offset: NativePointer, value: NativePointer | number) => void;
        setI8: (offset: NativePointer, value: number) => void;
        setI16: (offset: NativePointer, value: number) => void;
        setI32: (offset: NativePointer, value: number) => void;
        setI52: (offset: NativePointer, value: number) => void;
        setU52: (offset: NativePointer, value: number) => void;
        setI64Big: (offset: NativePointer, value: bigint) => void;
        setF32: (offset: NativePointer, value: number) => void;
        setF64: (offset: NativePointer, value: number) => void;
        getB32: (offset: NativePointer) => boolean;
        getU8: (offset: NativePointer) => number;
        getU16: (offset: NativePointer) => number;
        getU32: (offset: NativePointer) => number;
        getI8: (offset: NativePointer) => number;
        getI16: (offset: NativePointer) => number;
        getI32: (offset: NativePointer) => number;
        getI52: (offset: NativePointer) => number;
        getU52: (offset: NativePointer) => number;
        getI64Big: (offset: NativePointer) => bigint;
        getF32: (offset: NativePointer) => number;
        getF64: (offset: NativePointer) => number;
    };
    diagnostics: Diagnostics;
}
interface DotnetPublicAPI {
    API: APIType;
    /**
     * @deprecated Please use API object instead. See also MONOType in dotnet-legacy.d.ts
     */
    MONO: any;
    /**
     * @deprecated Please use API object instead. See also BINDINGType in dotnet-legacy.d.ts
     */
    BINDING: any;
    INTERNAL: any;
    EXPORTS: any;
    IMPORTS: any;
    Module: EmscriptenModule;
    RuntimeId: number;
    RuntimeBuildInfo: {
        ProductVersion: string;
        Configuration: string;
    };
}

interface IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
}
declare class ManagedObject implements IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
    toString(): string;
}
declare class ManagedError extends Error implements IDisposable {
    constructor(message: string);
    get stack(): string | undefined;
    dispose(): void;
    get isDisposed(): boolean;
    toString(): string;
}
declare const enum MemoryViewType {
    Byte = 0,
    Int32 = 1,
    Double = 2
}
interface IMemoryView {
    /**
     * copies elements from provided source to the wasm memory.
     * target has to have the elements of the same type as the underlying C# array.
     * same as TypedArray.set()
     */
    set(source: TypedArray, targetOffset?: number): void;
    /**
     * copies elements from wasm memory to provided target.
     * target has to have the elements of the same type as the underlying C# array.
     */
    copyTo(target: TypedArray, sourceOffset?: number): void;
    /**
     * same as TypedArray.slice()
     */
    slice(start?: number, end?: number): TypedArray;
    get length(): number;
    get byteLength(): number;
}

declare function createDotnetRuntime(moduleFactory: DotnetModuleConfig | ((api: DotnetPublicAPI) => DotnetModuleConfig)): Promise<DotnetPublicAPI>;
declare type CreateDotnetRuntimeType = typeof createDotnetRuntime;
declare global {
    function getDotnetRuntime(runtimeId: number): DotnetPublicAPI | undefined;
}

/**
 * Span class is JS wrapper for System.Span<T>. This view doesn't own the memory, nor pin the underlying array.
 * It's ideal to be used on call from C# with the buffer pinned there or with unmanaged memory.
 * It is disposed at the end of the call to JS.
 */
declare class Span implements IMemoryView, IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
    set(source: TypedArray, targetOffset?: number | undefined): void;
    copyTo(target: TypedArray, sourceOffset?: number | undefined): void;
    slice(start?: number | undefined, end?: number | undefined): TypedArray;
    get length(): number;
    get byteLength(): number;
}
/**
 * ArraySegment class is JS wrapper for System.ArraySegment<T>.
 * This wrapper would also pin the underlying array and hold GCHandleType.Pinned until this JS instance is collected.
 * User could dispose it manually.
 */
declare class ArraySegment implements IMemoryView, IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
    set(source: TypedArray, targetOffset?: number | undefined): void;
    copyTo(target: TypedArray, sourceOffset?: number | undefined): void;
    slice(start?: number | undefined, end?: number | undefined): TypedArray;
    get length(): number;
    get byteLength(): number;
}

export { APIType, ArraySegment, AssetBehaviours, AssetEntry, CreateDotnetRuntimeType, DotnetModuleConfig, DotnetPublicAPI, EmscriptenModule, IMemoryView, LoadingResource, ManagedError, ManagedObject, MemoryViewType, MonoConfig, NativePointer, ResourceRequest, Span, createDotnetRuntime as default };
