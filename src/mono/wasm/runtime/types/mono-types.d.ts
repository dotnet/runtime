// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*******************************************************************************************
 This file just acts as a set of object definitions to help the TSC compiler understand 
 the various namespaces that we use. These namespaces are not defined explicitly until
 the dotnet.js file is merged, so we pretend they exist by defining them here.

 THIS FILE IS NOT INCLUDED IN DOTNET.JS. ALL CODE HERE WILL BE IGNORED DURING THE BUILD
********************************************************************************************/

// VARIOUS C FUNCTIONS THAT WE CALL INTO ////////////////////////////////////////////////////
interface MONO_C_FUNCS {
    mono_background_exec (): void;
    mono_set_timeout_exec (a: number): void;
    
    mono_wasm_add_assembly (a: string, b: number, c: number): number;
    mono_wasm_add_satellite_assembly (a: string, b: string, c: number, d: number): void;
    mono_wasm_deregister_root (a: number): void;
    mono_wasm_exit (a: number): void;
    mono_wasm_get_icudt_name (a: string): string;
    mono_wasm_load_icu_data (a: number): number;
    mono_wasm_load_runtime (a: string, b: number): void;
    wasm_parse_runtime_options (a: number, b:number): void;
    mono_wasm_register_root (a: number, b: number, c: string | 0): number;
    wasm_setenv (a: string, b: string): void;
    mono_wasm_strdup (a: string): number;
    mono_wasm_string_get_data (a: number, b: number, c: number, d: number): void;
}

interface MONO_VARS {
    interned_string_table: Map<number, string | Symbol>;
    _scratch_root_free_indices: Int32Array,
    _scratch_root_free_indices_count: number;
    _mono_wasm_root_prototype: WasmRoot;
    _mono_wasm_root_buffer_prototype: WasmRootBuffer;
    _scratch_root_buffer: WasmRootBuffer,
    _scratch_root_free_instances: WasmRoot[],
    loaded_files: string[];
    loaded_assets: string[];
}

// NAMESPACES ///////////////////////////////////////////////////////////////////////////////
declare var MONO: typeof MonoSupportLib.$MONO & MONO_C_FUNCS & MONO_VARS & DEBUG_C_FUNCS & DEBUG_VARS; // Debug ones are from debugger-types.d.ts

// OTHER TYPES ///////////////////////////////////////////////////////////////////////

type FetchRequest = (asset: string) => Promise<{
    ok: boolean,
    url: string,
    arrayBuffer: () => Promise<Uint8Array>
}>;

declare const enum GlobalizationMode {
    ICU = "icu",
    INVARIANT = "invariant",
    AUTO = "auto"
}

type LoadedFiles = { 
    url: string,
    file: string,
}[];

type ManagedPointer = number; // - address in the managed heap

type MonoRuntimeArgs = {
    fetch_file_cb: FetchRequest,
    loaded_cb: () => void,
    debug_level: number,
    assembly_root: string,
    assets: AssetEntry[],
    globalization_mode?: GlobalizationMode,
    enable_debugging?: number,
    assembly_list?: any,
    runtime_assets?: any,
    runtime_asset_sources?: any,
    diagnostic_tracing?: any
    remote_sources?: string[],
    environment_variables?: object,
    runtime_options?: object,
    aot_profiler_options?: object,
    coverage_profiler_options?: object,
}

type NativePointer = number; // - address in wasm memory

type WasmEvent = {
    eventName: string, // - name of the event being raised
    [i: string]: any, // - arguments for the event itself
}

type WasmId = {
    idStr: string, // - full object id string
    scheme: string, // - eg, object, valuetype, array ..
    value: string, // - string part after `dotnet:scheme:` of the id string
    o: string, // - value parsed as JSON
}

type WasmRoot = {
    get_address: () => NativePointer,
    get_address_32: () => number,
    get: () => ManagedPointer,
    set: (value: any) => any,
    valueOf: () => ManagedPointer,
    clear: () => void,
    toString: () => string,
    release: () => void,
    value: number,
}

type WasmRootBuffer = {
    length: number,
    get_address: (index: number) => number,
    get_address_32: (index: number) => NativePointer,
    get: (index: number) => ManagedPointer,
    set: (index: number, value: number) => void,
    release: () => void,
}

type ByteReader = {
    read: () => number | false,
    get?:  () => boolean,
    configurable?: boolean,
    enumerable?: boolean
}
