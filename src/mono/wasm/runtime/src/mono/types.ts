// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    mono_wasm_new_root, mono_wasm_new_roots, mono_wasm_release_roots,
    mono_wasm_new_root_buffer, mono_wasm_new_root_buffer_from_pointer
} from './roots'
import {
    mono_wasm_add_dbg_command_received,
    mono_wasm_send_dbg_command_with_parms,
    mono_wasm_send_dbg_command,
    mono_wasm_get_dbg_command_info,
    mono_wasm_get_details,
    mono_wasm_release_object,
    mono_wasm_call_function_on,
    mono_wasm_debugger_resume,
    mono_wasm_detach_debugger,
    mono_wasm_runtime_ready,
    mono_wasm_get_loaded_files,
    mono_wasm_raise_debug_event,
    mono_wasm_fire_debugger_agent_message
} from './debug'
import { StringDecoder } from './string-decoder'
import {
    mono_load_runtime_and_bcl_args, mono_wasm_load_config,
    mono_wasm_setenv, mono_wasm_set_runtime_options,
    mono_wasm_load_data_archive, mono_wasm_asm_loaded,
    mono_wasm_load_bytes_into_heap
} from './init'
import { prevent_timer_throttling, mono_set_timeout, schedule_background_exec } from './scheduling'
import { mono_wasm_load_icu_data, mono_wasm_get_icudt_name, GlobalizationMode } from './icu'
import { AOTProfilerOptions, CoverageProfilerOptions } from './profiler'

// this represents visibility for the C code
export interface t_MonoSupportLib {
    mono_set_timeout: typeof mono_set_timeout
    mono_wasm_asm_loaded: typeof mono_wasm_asm_loaded
    mono_wasm_fire_debugger_agent_message: typeof mono_wasm_fire_debugger_agent_message,
    schedule_background_exec: typeof schedule_background_exec
    mono_wasm_setenv: typeof mono_wasm_setenv
    mono_wasm_new_root_buffer: typeof mono_wasm_new_root_buffer;
    mono_wasm_new_root_buffer_from_pointer: typeof mono_wasm_new_root_buffer_from_pointer;
    mono_wasm_new_root: typeof mono_wasm_new_root;
    mono_wasm_new_roots: typeof mono_wasm_new_roots;
    mono_wasm_release_roots: typeof mono_wasm_release_roots;
    mono_wasm_load_bytes_into_heap: typeof mono_wasm_load_bytes_into_heap;
    mono_wasm_get_loaded_files: typeof mono_wasm_get_loaded_files;
    mono_wasm_load_icu_data: typeof mono_wasm_load_icu_data;
    mono_wasm_get_icudt_name: typeof mono_wasm_get_icudt_name;
}

// this represents visibility in the javascript
// TODO limit this only to public API methods
export interface t_MONO {
    loaded_files: string[];
    loaded_assets: { [id: string]: [VoidPtr, number] },
    mono_wasm_runtime_is_ready: boolean;
    config?: MonoConfig,

    string_decoder: StringDecoder,
    mono_set_timeout: typeof mono_set_timeout
    mono_wasm_asm_loaded: typeof mono_wasm_asm_loaded
    mono_wasm_fire_debugger_agent_message: typeof mono_wasm_fire_debugger_agent_message,
    schedule_background_exec: typeof schedule_background_exec
    mono_wasm_new_root: typeof mono_wasm_new_root,
    mono_wasm_new_roots: typeof mono_wasm_new_roots,
    mono_wasm_release_roots: typeof mono_wasm_release_roots,
    mono_wasm_new_root_buffer: typeof mono_wasm_new_root_buffer,
    mono_wasm_new_root_buffer_from_pointer: typeof mono_wasm_new_root_buffer_from_pointer
    mono_wasm_load_bytes_into_heap: typeof mono_wasm_load_bytes_into_heap
    mono_wasm_add_dbg_command_received: typeof mono_wasm_add_dbg_command_received
    mono_wasm_send_dbg_command_with_parms: typeof mono_wasm_send_dbg_command_with_parms
    mono_wasm_send_dbg_command: typeof mono_wasm_send_dbg_command
    mono_wasm_get_dbg_command_info: typeof mono_wasm_get_dbg_command_info
    mono_wasm_get_details: typeof mono_wasm_get_details
    mono_wasm_release_object: typeof mono_wasm_release_object
    mono_wasm_call_function_on: typeof mono_wasm_call_function_on
    mono_wasm_debugger_resume: typeof mono_wasm_debugger_resume
    mono_wasm_detach_debugger: typeof mono_wasm_detach_debugger
    mono_wasm_runtime_ready: typeof mono_wasm_runtime_ready
    mono_wasm_get_loaded_files: typeof mono_wasm_get_loaded_files
    mono_wasm_raise_debug_event: typeof mono_wasm_raise_debug_event
    mono_load_runtime_and_bcl_args: typeof mono_load_runtime_and_bcl_args
    prevent_timer_throttling: typeof prevent_timer_throttling
    mono_wasm_load_icu_data: typeof mono_wasm_load_icu_data
    mono_wasm_get_icudt_name: typeof mono_wasm_get_icudt_name
    mono_wasm_load_config: typeof mono_wasm_load_config
    mono_wasm_setenv: typeof mono_wasm_setenv
    mono_wasm_set_runtime_options: typeof mono_wasm_set_runtime_options
    mono_wasm_load_data_archive: typeof mono_wasm_load_data_archive
}

// how we extended wasm Module
export interface t_ModuleExtension {
    config?: MonoConfig,
}

export type MonoConfig = {
    assembly_root: string, // the subfolder containing managed assemblies and pdbs
    assets: (AssetEntry | AssetEntry | SatelliteAssemblyEntry | VfsEntry | IcuData)[], // a list of assets to load along with the runtime. each asset is a dictionary-style Object with the following properties:
    loaded_cb: Function, // a function invoked when loading has completed
    debug_level?: number, // Either this or the next one needs to be set
    enable_debugging?: number, // Either this or the previous one needs to be set
    fetch_file_cb?: Request, // a function (string) invoked to fetch a given file. If no callback is provided a default implementation appropriate for the current environment will be selected (readFileSync in node, fetch elsewhere). If no default implementation is available this call will fail.
    globalization_mode: GlobalizationMode, // configures the runtime's globalization mode
    assembly_list?: any, // obsolete but necessary for the check
    runtime_assets?: any, // obsolete but necessary for the check
    runtime_asset_sources?: any, // obsolete but necessary for the check
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

export type MonoConfigError = { message: string, error: any }

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
