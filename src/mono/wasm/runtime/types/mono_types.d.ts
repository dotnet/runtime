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
    mono_wasm_send_dbg_command (a: number, b:number, c: number, d: number, e: number): boolean;
    mono_wasm_send_dbg_command_with_parms (a: number, b:number, c: number, d: number, e: number, f: number, g: string): boolean;
    mono_wasm_set_is_debugger_attached (a: boolean): void;
    wasm_setenv (a: string, b: string): void;
    mono_wasm_strdup (a: string): number;
    mono_wasm_string_get_data (a: number, b: number, c: number, d: number): void;
}

interface MONO_VARS {
    _base64Table: string[];
    mono_wasm_string_decoder_buffer: number;
    mono_wasm_empty_string: string;
    _next_call_function_res_id: number;
    _next_id_var: number;
    _call_function_res_cache: any;
}

// NAMESPACES ///////////////////////////////////////////////////////////////////////////////
declare var MONO: typeof MonoSupportLib.$MONO & MONO_C_FUNCS & MONO_VARS;

// OTHER TYPES ///////////////////////////////////////////////////////////////////////

type GlobalizationMode = "icu" | "invarient" | "auto";

type LoadedFiles = { 
    url: string,
    file: string,
}[];

type ManagedPointer = number; // - address in the managed heap

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
