// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { bind_runtime_method } from "./method-binding";

export type TypedArray = Int8Array | Uint8Array | Uint8ClampedArray | Int16Array | Uint16Array | Int32Array | Uint32Array | Float32Array | Float64Array;

export interface ManagedPointer {
    __brandManagedPointer: "ManagedPointer"
}

export interface NativePointer {
    __brandNativePointer: "NativePointer"
}

export interface VoidPtr extends NativePointer {
    __brand: "VoidPtr"
}

export interface CharPtr extends NativePointer {
    __brand: "CharPtr"
}

export interface Int32Ptr extends NativePointer {
    __brand: "Int32Ptr"
}

export interface CharPtrPtr extends NativePointer {
    __brand: "CharPtrPtr"
}

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
export const MonoMethodNull: MonoMethod = <MonoMethod><any>0;
export const MonoObjectNull: MonoObject = <MonoObject><any>0;
export const MonoArrayNull: MonoArray = <MonoArray><any>0;
export const MonoAssemblyNull: MonoAssembly = <MonoAssembly><any>0;
export const MonoClassNull: MonoClass = <MonoClass><any>0;
export const MonoTypeNull: MonoType = <MonoType><any>0;
export const MonoStringNull: MonoString = <MonoString><any>0;
export const JSHandleDisposed: JSHandle = <JSHandle><any>-1;
export const JSHandleNull: JSHandle = <JSHandle><any>0;
export const VoidPtrNull: VoidPtr = <VoidPtr><any>0;
export const CharPtrNull: CharPtr = <CharPtr><any>0;

export function coerceNull<T extends ManagedPointer | NativePointer>(ptr: T | null | undefined): T {
    return (<any>ptr | <any>0) as any;
}

export type MonoConfig = {
    isError: false,
    assembly_root: string, // the subfolder containing managed assemblies and pdbs
    assets: AllAssetEntryTypes[], // a list of assets to load along with the runtime. each asset is a dictionary-style Object with the following properties:
    debug_level?: number, // Either this or the next one needs to be set
    enable_debugging?: number, // Either this or the previous one needs to be set
    fetch_file_cb?: Request, // a function (string) invoked to fetch a given file. If no callback is provided a default implementation appropriate for the current environment will be selected (readFileSync in node, fetch elsewhere). If no default implementation is available this call will fail.
    globalization_mode: GlobalizationMode, // configures the runtime's globalization mode
    diagnostic_tracing?: boolean // enables diagnostic log messages during startup
    remote_sources?: string[], // additional search locations for assets. Sources will be checked in sequential order until the asset is found. The string "./" indicates to load from the application directory (as with the files in assembly_list), and a fully-qualified URL like "https://example.com/" indicates that asset loads can be attempted from a remote server. Sources must end with a "/".
    environment_variables?: {
        [i: string]: string;
    }, // dictionary-style Object containing environment variables
    runtime_options?: string[], // array of runtime options as strings
    aot_profiler_options?: AOTProfilerOptions, // dictionary-style Object. If omitted, aot profiler will not be initialized.
    coverage_profiler_options?: CoverageProfilerOptions, // dictionary-style Object. If omitted, coverage profiler will not be initialized.
    ignore_pdb_load_errors?: boolean
};

export type MonoConfigError = {
    isError: true,
    message: string,
    error: any
}

export type AllAssetEntryTypes = AssetEntry | AssemblyEntry | SatelliteAssemblyEntry | VfsEntry | IcuData;

// Types of assets that can be in the mono-config.js/mono-config.json file (taken from /src/tasks/WasmAppBuilder/WasmAppBuilder.cs)
export type AssetEntry = {
    name: string, // the name of the asset, including extension.
    behavior: AssetBehaviours, // determines how the asset will be handled once loaded
    virtual_path?: string, // if specified, overrides the path of the asset in the virtual filesystem and similar data structures once loaded.
    culture?: string,
    load_remote?: boolean, // if true, an attempt will be made to load the asset from each location in @args.remote_sources.
    is_optional?: boolean // if true, any failure to load this asset will be ignored.
}

export interface AssemblyEntry extends AssetEntry {
    name: "assembly"
}

export interface SatelliteAssemblyEntry extends AssetEntry {
    name: "resource",
    culture: string
}

export interface VfsEntry extends AssetEntry {
    name: "vfs",
    virtual_path: string
}

export interface IcuData extends AssetEntry {
    name: "icu",
    load_remote: boolean
}

// Note that since these are annoated as `declare const enum` they are replaces by tsc with their raw value during compilation
export const enum AssetBehaviours {
    Resource = "resource", // load asset as a managed resource assembly
    Assembly = "assembly", // load asset as a managed assembly (or debugging information)
    Heap = "heap", // store asset into the native heap
    ICU = "icu", // load asset as an ICU data archive
    VFS = "vfs", // load asset into the virtual filesystem (for fopen, File.Open, etc)
}

export type RuntimeHelpers = {
    get_call_sig: MonoMethod;
    runtime_namespace: string;
    runtime_classname: string;
    wasm_runtime_class: MonoClass;
    bind_runtime_method: typeof bind_runtime_method;

    _box_buffer_size: number;
    _unbox_buffer_size: number;

    _box_buffer: VoidPtr;
    _unbox_buffer: VoidPtr;
    _class_int32: MonoClass;
    _class_uint32: MonoClass;
    _class_double: MonoClass;
    _class_boolean: MonoClass;
    mono_wasm_runtime_is_ready: boolean;
    mono_wasm_bindings_is_ready: boolean;

    loaded_files: string[];
    config: MonoConfig | MonoConfigError;
}

export const wasm_type_symbol = Symbol.for("wasm type");

export const enum GlobalizationMode {
    ICU = "icu", // load ICU globalization data from any runtime assets with behavior "icu".
    INVARIANT = "invariant", //  operate in invariant globalization mode.
    AUTO = "auto" // (default): if "icu" behavior assets are present, use ICU, otherwise invariant.
}

export type AOTProfilerOptions = {
    write_at?: string, // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::StopProfile'
    send_to?: string // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::DumpAotProfileData' (DumpAotProfileData stores the data into INTERNAL.aot_profile_data.)
}

export type CoverageProfilerOptions = {
    write_at?: string, // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::StopProfile'
    send_to?: string // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::DumpCoverageProfileData' (DumpCoverageProfileData stores the data into INTERNAL.coverage_profile_data.)
}

// how we extended emscripten Module
export type EmscriptenModuleMono = EmscriptenModule & EmscriptenModuleConfig;

export type EmscriptenModuleConfig = {
    disableDotNet6Compatibility?: boolean,

    // backward compatibility
    config?: MonoConfig | MonoConfigError,
    configSrc?: string,
    onConfigLoaded?: () => void;
    onDotNetReady?: () => void;

    /**
     * @deprecated DEPRECATED! backward compatibility https://github.com/search?q=mono_bind_static_method&type=Code
     */
    mono_bind_static_method: (fqn: string, signature: string) => Function,
}
