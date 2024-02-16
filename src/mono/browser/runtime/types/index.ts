// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { EmscriptenModule, NativePointer } from "./emscripten";

export interface DotnetHostBuilder {
    /**
     * @param config default values for the runtime configuration. It will be merged with the default values.
     * Note that if you provide resources and don't provide custom configSrc URL, the blazor.boot.json will be downloaded and applied by default.
     */
    withConfig(config: MonoConfig): DotnetHostBuilder;
    /**
     * @param configSrc URL to the configuration file. ./blazor.boot.json is a default config file location.
     */
    withConfigSrc(configSrc: string): DotnetHostBuilder;
    /**
     * "command line" arguments for the Main() method.
     * @param args
     */
    withApplicationArguments(...args: string[]): DotnetHostBuilder;
    /**
     * Sets the environment variable for the "process"
     */
    withEnvironmentVariable(name: string, value: string): DotnetHostBuilder;
    /**
     * Sets the environment variables for the "process"
     */
    withEnvironmentVariables(variables: {
        [i: string]: string;
    }): DotnetHostBuilder;
    /**
     * Sets the "current directory" for the "process" on the virtual file system.
     */
    withVirtualWorkingDirectory(vfsPath: string): DotnetHostBuilder;
    /**
     * @param enabled if "true", writes diagnostic messages during runtime startup and execution to the browser console.
     */
    withDiagnosticTracing(enabled: boolean): DotnetHostBuilder;
    /**
     * @param level
     * level > 0 enables debugging and sets the logging level to debug
     * level == 0 disables debugging and enables interpreter optimizations
     * level < 0 enables debugging and disables debug logging.
     */
    withDebugging(level: number): DotnetHostBuilder;
    /**
     * @param mainAssemblyName Sets the name of the assembly with the Main() method. Default is the same as the .csproj name.
     */
    withMainAssembly(mainAssemblyName: string): DotnetHostBuilder;
    /**
     * Supply "command line" arguments for the Main() method from browser query arguments named "arg". Eg. `index.html?arg=A&arg=B&arg=C`.
     * @param args
     */
    withApplicationArgumentsFromQuery(): DotnetHostBuilder;
    /**
     * Sets application environment, such as "Development", "Staging", "Production", etc.
     */
    withApplicationEnvironment(applicationEnvironment?: string): DotnetHostBuilder;
    /**
     * Sets application culture. This is a name specified in the BCP 47 format. See https://tools.ietf.org/html/bcp47
     */
    withApplicationCulture(applicationCulture?: string): DotnetHostBuilder;
    /**
     * Overrides the built-in boot resource loading mechanism so that boot resources can be fetched
     * from a custom source, such as an external CDN.
     */
    withResourceLoader(loadBootResource?: LoadBootResourceCallback): DotnetHostBuilder;
    /**
     * Starts the runtime and returns promise of the API object.
     */
    create(): Promise<RuntimeAPI>;

    /**
     * Runs the Main() method of the application and exits the runtime.
     * You can provide "command line" arguments for the Main() method using
     * - dotnet.withApplicationArguments(["A", "B", "C"])
     * - dotnet.withApplicationArgumentsFromQuery()
     * Note: after the runtime exits, it would reject all further calls to the API.
     * You can use runMain() if you want to keep the runtime alive.
     */
    run(): Promise<number>;
}

// when adding new fields, please consider if it should be impacting the config hash. If not, please drop it in the getCacheKey()
export type MonoConfig = {
    /**
     * Additional search locations for assets.
     */
    remoteSources?: string[], // Sources will be checked in sequential order until the asset is found. The string "./" indicates to load from the application directory (as with the files in assembly_list), and a fully-qualified URL like "https://example.com/" indicates that asset loads can be attempted from a remote server. Sources must end with a "/".
    /**
     * It will not fail the startup is .pdb files can't be downloaded
     */
    ignorePdbLoadErrors?: boolean,
    /**
     * We are throttling parallel downloads in order to avoid net::ERR_INSUFFICIENT_RESOURCES on chrome. The default value is 16.
     */
    maxParallelDownloads?: number,
    /**
     * We are making up to 2 more delayed attempts to download same asset. Default true.
     */
    enableDownloadRetry?: boolean,
    /**
     * Name of the assembly with main entrypoint
     */
    mainAssemblyName?: string,
    /**
     * Configures the runtime's globalization mode
     */
    globalizationMode?: GlobalizationMode,
    /**
     * debugLevel > 0 enables debugging and sets the debug log level to debugLevel
     * debugLevel == 0 disables debugging and enables interpreter optimizations
     * debugLevel < 0 enables debugging and disables debug logging.
     */
    debugLevel?: number,

    /**
     * Gets a value that determines whether to enable caching of the 'resources' inside a CacheStorage instance within the browser.
     */
    cacheBootResources?: boolean,
    /**
     * Delay of the purge of the cached resources in milliseconds. Default is 10000 (10 seconds).
     */
    cachedResourcesPurgeDelay?: number,
    /**
     * Configures use of the `integrity` directive for fetching assets
     */
    disableIntegrityCheck?: boolean,
    /**
     * Configures use of the `no-cache` directive for fetching assets
     */
    disableNoCacheFetch?: boolean,
    /**
    * Enables diagnostic log messages during startup
    */
    diagnosticTracing?: boolean
    /**
     * Dictionary-style Object containing environment variables
     */
    environmentVariables?: {
        [i: string]: string;
    },
    /**
     * initial number of workers to add to the emscripten pthread pool
     */
    pthreadPoolSize?: number,
    /**
     * If true, a list of the methods optimized by the interpreter will be saved and used for faster startup
     *  on future runs of the application
     */
    interpreterPgo?: boolean,
    /**
     * Configures how long to wait before saving the interpreter PGO list. If your application takes
     *  a while to start you should adjust this value.
     */
    interpreterPgoSaveDelay?: number,
    /**
     * application environment
     */
    applicationEnvironment?: string,

    /**
     * Gets the application culture. This is a name specified in the BCP 47 format. See https://tools.ietf.org/html/bcp47
     */
    applicationCulture?: string,

    /**
     * definition of assets to load along with the runtime.
     */
    resources?: ResourceGroups;

    /**
     * appsettings files to load to VFS
     */
    appsettings?: string[];

    /**
     * config extensions declared in MSBuild items @(WasmBootConfigExtension)
     */
    extensions?: { [name: string]: any };

    /**
     * This is initial working directory for the runtime on the virtual file system. Default is "/".
     */
    virtualWorkingDirectory?: string;

    /**
     * This is the arguments to the Main() method of the program when called with dotnet.run() Default is [].
     * Note: RuntimeAPI.runMain() and RuntimeAPI.runMainAndExit() will replace this value, if they provide it.
     */
    applicationArguments?: string[];
};

export type ResourceExtensions = { [extensionName: string]: ResourceList };

export interface ResourceGroups {
    hash?: string;
    assembly?: ResourceList; // nullable only temporarily
    lazyAssembly?: ResourceList; // nullable only temporarily
    pdb?: ResourceList;

    jsModuleWorker?: ResourceList;
    jsModuleNative: ResourceList;
    jsModuleRuntime: ResourceList;
    wasmSymbols?: ResourceList;
    wasmNative: ResourceList;
    icu?: ResourceList;

    satelliteResources?: { [cultureName: string]: ResourceList };

    modulesAfterConfigLoaded?: ResourceList,
    modulesAfterRuntimeReady?: ResourceList

    extensions?: ResourceExtensions
    vfs?: { [virtualPath: string]: ResourceList };
}

/**
 * A "key" is name of the file, a "value" is optional hash for integrity check.
 */
export type ResourceList = { [name: string]: string | null | "" };

/**
 * Overrides the built-in boot resource loading mechanism so that boot resources can be fetched
 * from a custom source, such as an external CDN.
 * @param type The type of the resource to be loaded.
 * @param name The name of the resource to be loaded.
 * @param defaultUri The URI from which the framework would fetch the resource by default. The URI may be relative or absolute.
 * @param integrity The integrity string representing the expected content in the response.
 * @param behavior The detailed behavior/type of the resource to be loaded.
 * @returns A URI string or a Response promise to override the loading process, or null/undefined to allow the default loading behavior.
 * When returned string is not qualified with `./` or absolute URL, it will be resolved against the application base URI.
 */
export type LoadBootResourceCallback = (type: WebAssemblyBootResourceType, name: string, defaultUri: string, integrity: string, behavior: AssetBehaviors) => string | Promise<Response> | null | undefined;

export interface LoadingResource {
    name: string;
    url: string;
    response: Promise<Response>;
}

// Types of assets that can be in the _framework/blazor.boot.json file (taken from /src/tasks/WasmAppBuilder/WasmAppBuilder.cs)
export interface AssetEntry {
    /**
     * the name of the asset, including extension.
     */
    name: string,
    /**
     * determines how the asset will be handled once loaded
     */
    behavior: AssetBehaviors,
    /**
     * this should be absolute url to the asset
     */
    resolvedUrl?: string;
    /**
     * the integrity hash of the asset (if any)
     */
    hash?: string | null | "";
    /**
     * If specified, overrides the path of the asset in the virtual filesystem and similar data structures once downloaded.
     */
    virtualPath?: string,
    /**
     * Culture code
     */
    culture?: string,
    /**
     * If true, an attempt will be made to load the asset from each location in MonoConfig.remoteSources.
     */
    loadRemote?: boolean,
    /**
     * If true, the runtime startup would not fail if the asset download was not successful.
     */
    isOptional?: boolean
    /**
     * If provided, runtime doesn't have to fetch the data.
     * Runtime would set the buffer to null after instantiation to free the memory.
     */
    buffer?: ArrayBuffer | Promise<ArrayBuffer>,

    /**
     * If provided, runtime doesn't have to import it's JavaScript modules.
     * This will not work for multi-threaded runtime.
     */
    moduleExports?: any | Promise<any>,

    /**
     * It's metadata + fetch-like Promise<Response>
     * If provided, the runtime doesn't have to initiate the download. It would just await the response.
     */
    pendingDownload?: LoadingResource
}

export type SingleAssetBehaviors =
    /**
     * The binary of the .NET runtime.
     */
    | "dotnetwasm"
    /**
     * The javascript module for loader.
     */
    | "js-module-dotnet"
    /**
     * The javascript module for threads.
     */
    | "js-module-threads"
    /**
     * The javascript module for runtime.
     */
    | "js-module-runtime"
    /**
     * The javascript module for emscripten.
     */
    | "js-module-native"
    /**
     * Typically blazor.boot.json
     */
    | "manifest"
    /**
     * The debugging symbols
     */
    | "symbols"
    /**
     * Load segmentation rules file for Hybrid Globalization.
     */
    | "segmentation-rules";

export type AssetBehaviors = SingleAssetBehaviors |
    /**
     * Load asset as a managed resource assembly.
     */
    "resource"
    /**
     * Load asset as a managed assembly.
     */
    | "assembly"
    /**
     * Load asset as a managed debugging information.
     */
    | "pdb"
    /**
     * Store asset into the native heap.
     */
    | "heap"
    /**
     * Load asset as an ICU data archive.
     */
    | "icu"
    /**
     * Load asset into the virtual filesystem (for fopen, File.Open, etc).
     */
    | "vfs"
    /**
     * The javascript module that came from nuget package .
     */
    | "js-module-library-initializer"

export const enum GlobalizationMode {
    /**
     * Load sharded ICU data.
     */
    Sharded = "sharded",
    /**
     * Load all ICU data.
     */
    All = "all",
    /**
     * Operate in invariant globalization mode.
     */
    Invariant = "invariant",
    /**
     * Use user defined icu file.
     */
    Custom = "custom",
    /**
     * Operate in hybrid globalization mode with small ICU files, using native platform functions.
     */
    Hybrid = "hybrid"
}

export type DotnetModuleConfig = {
    config?: MonoConfig,
    configSrc?: string,
    onConfigLoaded?: (config: MonoConfig) => void | Promise<void>;
    onDotnetReady?: () => void | Promise<void>;
    onDownloadResourceProgress?: (resourcesLoaded: number, totalResources: number) => void;

    imports?: any;
    exports?: string[];
} & Partial<EmscriptenModule>

export type APIType = {
    /**
     * Runs the Main() method of the application.
     * Note: this will keep the .NET runtime alive and the APIs will be available for further calls.
     * @param mainAssemblyName name of the assembly with the Main() method. Optional. Default is the same as the .csproj name.
     * @param args command line arguments for the Main() method. Optional.
     * @returns exit code of the Main() method.
     */
    runMain: (mainAssemblyName?: string, args?: string[]) => Promise<number>;
    /**
     * Runs the Main() method of the application and exits the runtime.
     * Note: after the runtime exits, it would reject all further calls to the API.
     * @param mainAssemblyName name of the assembly with the Main() method. Optional. Default is the same as the .csproj name.
     * @param args command line arguments for the Main() method. Optional.
     * @returns exit code of the Main() method.
     */
    runMainAndExit: (mainAssemblyName?: string, args?: string[]) => Promise<number>;
    /**
     * Sets the environment variable for the "process"
     * @param name
     * @param value
     */
    setEnvironmentVariable: (name: string, value: string) => void;
    /**
     * Returns the [JSExport] methods of the assembly with the given name
     * @param assemblyName
     */
    getAssemblyExports(assemblyName: string): Promise<any>;
    /**
     * Provides functions which could be imported by the managed code using [JSImport]
     * @param moduleName maps to the second parameter of [JSImport]
     * @param moduleImports object with functions which could be imported by the managed code. The keys map to the first parameter of [JSImport]
     */
    setModuleImports(moduleName: string, moduleImports: any): void;
    /**
     * Returns the configuration object used to start the runtime.
     */
    getConfig: () => MonoConfig;
    /**
     * Executes scripts which were loaded during runtime bootstrap.
     * You can register the scripts using MonoConfig.resources.modulesAfterConfigLoaded and MonoConfig.resources.modulesAfterRuntimeReady.
     */
    invokeLibraryInitializers: (functionName: string, args: any[]) => Promise<void>;
    /**
     * Writes to the WASM linear memory
     */
    setHeapB32: (offset: NativePointer, value: number | boolean) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapU8: (offset: NativePointer, value: number) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapU16: (offset: NativePointer, value: number) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapU32: (offset: NativePointer, value: NativePointer | number) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapI8: (offset: NativePointer, value: number) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapI16: (offset: NativePointer, value: number) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapI32: (offset: NativePointer, value: number) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapI52: (offset: NativePointer, value: number) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapU52: (offset: NativePointer, value: number) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapI64Big: (offset: NativePointer, value: bigint) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapF32: (offset: NativePointer, value: number) => void;
    /**
     * Writes to the WASM linear memory
     */
    setHeapF64: (offset: NativePointer, value: number) => void;
    /**
     * Reads from the WASM linear memory
     */
    getHeapB32: (offset: NativePointer) => boolean;
    /**
     * Reads from the WASM linear memory
     */
    getHeapU8: (offset: NativePointer) => number;
    /**
     * Reads from the WASM linear memory
     */
    getHeapU16: (offset: NativePointer) => number;
    /**
     * Reads from the WASM linear memory
     */
    getHeapU32: (offset: NativePointer) => number;
    /**
     * Reads from the WASM linear memory
     */
    getHeapI8: (offset: NativePointer) => number;
    /**
     * Reads from the WASM linear memory
     */
    getHeapI16: (offset: NativePointer) => number;
    /**
     * Reads from the WASM linear memory
     */
    getHeapI32: (offset: NativePointer) => number;
    /**
     * Reads from the WASM linear memory
     */
    getHeapI52: (offset: NativePointer) => number;
    /**
     * Reads from the WASM linear memory
     */
    getHeapU52: (offset: NativePointer) => number;
    /**
     * Reads from the WASM linear memory
     */
    getHeapI64Big: (offset: NativePointer) => bigint;
    /**
     * Reads from the WASM linear memory
     */
    getHeapF32: (offset: NativePointer) => number;
    /**
     * Reads from the WASM linear memory
     */
    getHeapF64: (offset: NativePointer) => number;
    /**
     * Returns a short term view of the WASM linear memory. Don't store the reference, don't use it after await.
     */
    localHeapViewI8: () => Int8Array;
    /**
     * Returns a short term view of the WASM linear memory. Don't store the reference, don't use it after await.
     */
    localHeapViewI16: () => Int16Array;
    /**
     * Returns a short term view of the WASM linear memory. Don't store the reference, don't use it after await.
     */
    localHeapViewI32: () => Int32Array;
    /**
     * Returns a short term view of the WASM linear memory. Don't store the reference, don't use it after await.
     */
    localHeapViewI64Big: () => BigInt64Array;
    /**
     * Returns a short term view of the WASM linear memory. Don't store the reference, don't use it after await.
     */
    localHeapViewU8: () => Uint8Array;
    /**
     * Returns a short term view of the WASM linear memory. Don't store the reference, don't use it after await.
     */
    localHeapViewU16: () => Uint16Array;
    /**
     * Returns a short term view of the WASM linear memory. Don't store the reference, don't use it after await.
     */
    localHeapViewU32: () => Uint32Array;
    /**
     * Returns a short term view of the WASM linear memory. Don't store the reference, don't use it after await.
     */
    localHeapViewF32: () => Float32Array;
    /**
     * Returns a short term view of the WASM linear memory. Don't store the reference, don't use it after await.
     */
    localHeapViewF64: () => Float64Array;
}

export type RuntimeAPI = {
    INTERNAL: any,
    Module: EmscriptenModule,
    runtimeId: number,
    runtimeBuildInfo: {
        productVersion: string,
        gitHash: string,
        buildConfiguration: string,
        wasmEnableThreads: boolean,
        wasmEnableSIMD: boolean,
        wasmEnableExceptionHandling: boolean,
    }
} & APIType

export type ModuleAPI = {
    /**
     * The builder for the .NET runtime.
     */
    dotnet: DotnetHostBuilder;
    /**
     * Terminates the runtime "process" and reject all further calls to the API.
     */
    exit: (code: number, reason?: any) => void;
}

export type CreateDotnetRuntimeType = (moduleFactory: DotnetModuleConfig | ((api: RuntimeAPI) => DotnetModuleConfig)) => Promise<RuntimeAPI>;

// This type doesn't have to align with anything in BootConfig.
// Instead, this represents the public API through which certain aspects
// of boot resource loading can be customized.
export type WebAssemblyBootResourceType = "assembly" | "pdb" | "dotnetjs" | "dotnetwasm" | "globalization" | "manifest" | "configuration";
