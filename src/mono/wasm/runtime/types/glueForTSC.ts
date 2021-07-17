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
    mono_wasm_register_root (a: number, b: number, c: string): number;
    mono_wasm_deregister_root (a: number): void;
    mono_wasm_string_get_data (a: number, b: number, c: number, d: number): void;
    mono_wasm_set_is_debugger_attached (a: boolean): void;
    mono_wasm_send_dbg_command (a: number, b:number, c: number, d: number, e: number): boolean;
    mono_wasm_send_dbg_command_with_parms (a: number, b:number, c: number, d: number, e: number, f: number, g: string): boolean;
    mono_wasm_setenv (a: string, b: string): void;
    mono_wasm_parse_runtime_options (a: number, b:number): void;
    mono_wasm_strdup (a: string): number;
    mono_wasm_load_icu_data (a: number): number;
    mono_wasm_get_icudt_name (a: string): string;
    mono_wasm_load_runtime (a: string, b: number): void;
    mono_wasm_exit (a: number): void;
    mono_wasm_add_assembly (a: string, b: number, c: number): number;
    mono_wasm_add_satellite_assembly (a: string, b: string, c: number, d: number): void;
    mono_set_timeout_exec (a: number): void;
}

interface BINDING_C_FUNCS {
    mono_wasm_typed_array_new (a: string, b: string, c: number, d: number): number;
    assembly_load (a: string): number;
    find_corlib_class (a: string, b: string): number;
    find_class (a: number, b: string, c: string): number;
    _find_method (a: number, b: string, c: number): number; 
    invoke_method (a: number, b: number, c: number, d: number): number;
    mono_string_get_utf8 (a: number): number;
    mono_wasm_string_from_utf16 (a: number, b: number): number;
    mono_get_obj_type (a: number): number;
    mono_array_length (a: number): number;
    mono_array_get (a: number, b: number): number;
    mono_obj_array_new (a: number): number;
    mono_obj_array_set (a: number, b: number, c: number): void;
    mono_wasm_register_bundled_satellite_assemblies (): void;
    mono_wasm_try_unbox_primitive_and_get_type (a: number, b: number): number;
    mono_wasm_box_primitive (a: number, b: number, c: number): number;
    mono_wasm_intern_string (a: number): number;
    assembly_get_entry_point (a: number): number;
    mono_wasm_string_array_new (a: number): number;
    mono_wasm_typed_array_new (a: number, b: number, c: number, d: number): number;
}

interface DOTNET_C_FUNCS {
    mono_wasm_string_from_js (a: string): number;
}

// NAMESPACES ///////////////////////////////////////////////////////////////////////////////
var MONO: typeof MonoSupportLib.$MONO & MONO_C_FUNCS;
var DOTNET: typeof DotNetSupportLib.$DOTNET & DOTNET_C_FUNCS;
var BINDING: typeof BindingSupportLib.$BINDING & BINDING_C_FUNCS;

// LIBRARY_MONO TYPES ///////////////////////////////////////////////////////////////////////
type ManagedPointer = number; // - address in the managed heap

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

type MonoRuntimeArgs = {
    fetch_file_cb: (asset: string) => void,
    loaded_cb: () => void,
    debug_level: number,
    assembly_root: string,
    assets: {
        name: string,
        behavior: string,
    }[],
}

type LoadedFiles = { 
    url: string,
    file: string,
}[];

type GlobalizationMode = "icu" | "invarient" | "auto";

type WasmRootBuffer = {
    length: number,
    get_address: (index: number) => number,
    get_address_32: (index: number) => NativePointer,
    get: (index: number) => ManagedPointer,
    set: (index: number, value: number) => void,
    release: () => void,
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

// EMSDK NON MODULE RELATED /////////////////////////////////////////////////////////////////
var ENVIRONMENT_IS_WEB: boolean;
var ENVIRONMENT_IS_SHELL: boolean;
var ENVIRONMENT_IS_NODE: boolean;
var ENVIRONMENT_IS_WORKER: boolean;

declare function locateFile(path: string): string;

// OTHER ////////////////////////////////////////////////////////////////////////////////////
declare function read (path: string): string;
declare function load (path: string): string;
declare function require (path: string): object;
