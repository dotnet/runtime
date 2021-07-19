// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*******************************************************************************************
 This file just acts as a set of object definitions to help the TSC compiler understand 
 the various namespaces that we use. These namespaces are not defined explicitly until
 the dotnet.js file is merged, so we pretend they exist by defining them here.

 THIS FILE IS NOT INCLUDED IN DOTNET.JS. ALL CODE HERE WILL BE IGNORED DURING THE BUILD
********************************************************************************************/

// VARIOUS C FUNCTIONS THAT WE CALL INTO ////////////////////////////////////////////////////
interface BINDING_C_FUNCS {
    _find_method (a: number, b: string, c: number): number; 
    assembly_get_entry_point (a: number): number;
    assembly_load (a: string): number;
    find_class (a: number, b: string, c: string): number;
    find_corlib_class (a: string, b: string): number;
    invoke_method (a: number, b: number, c: number, d: number): number;
    
    mono_array_length (a: number): number;
    mono_array_get (a: number, b: number): number;
    mono_get_obj_type (a: number): number;
    mono_obj_array_new (a: number): number;
    mono_obj_array_set (a: number, b: number, c: number): void;
    mono_string_get_utf8 (a: number): number;
    
    mono_wasm_box_primitive (a: number, b: number, c: number): number;
    mono_wasm_get_delegate_invoke (a: number): number;
    mono_wasm_intern_string (a: number): number;
    mono_wasm_register_bundled_satellite_assemblies (): void;
    mono_wasm_string_array_new (a: number): number;
    mono_wasm_string_from_utf16 (a: number, b: number): number;
    mono_wasm_try_unbox_primitive_and_get_type (a: number, b: number): number;
    mono_typed_array_new (a: number, b: number, c: number, d: number): number;
}

interface BINDING_VARS {
    init: boolean;
    _box_buffer: number;
    _unbox_buffer: number;
    _class_int32: number;
    _class_uint32: number;
    _class_double: number;
    _class_boolean: number;
    binding_module: number;
    safehandle_addref: number;
    safehandle_release: number;
    safehandle_get_handle: number;
    safehandle_release_by_handle: number;
    _are_promises_supported: boolean;
    _empty_string: string;
    _empty_string_ptr: number;
    _interned_string_full_root_buffers: WasmRootBuffer[];
    _interned_string_current_root_buffer: WasmRootBuffer;
    _interned_string_current_root_buffer_count: number;
    _interned_js_string_table: Map<string, number>;
    _method_descriptions: Map<number, string>;
    _signature_converters: Map<string, Converter>;
    _primitive_converters: Map<string, Converter>;
    scratchBuffer: number;

    _bind_js_obj: Function;
    _bind_core_clr_obj: Function;
    _bind_existing_obj: Function;
    _unbind_raw_obj_and_free: Function;
    _get_js_id: Function;
    _get_raw_mono_obj: Function;
    _is_simple_array: Function;
    _object_to_string: Function;
    setup_js_cont: number;
    create_tcs: number;
    set_tcs_result: number;
    set_tcs_failure: number;
    tcs_get_task_and_bind: number;
    get_call_sig: number;
    get_date_value: number;
    create_date_time: number;
    create_uri: number;
}

// NAMESPACES ///////////////////////////////////////////////////////////////////////////////
declare var BINDING: typeof BindingSupportLib.$BINDING & BINDING_C_FUNCS & BINDING_VARS;

// OTHER TYPES ///////////////////////////////////////////////////////////////////////
type Converter = {
    steps: any[];
    size: number;
    args_marshal?: any;
    is_result_definitely_unmarshaled?: boolean;
    is_result_possibly_unmarshaled?: boolean;
    result_unmarshaled_if_argc?: number;
    needs_root_buffer?: boolean;
    name?: string;
    needs_root?: boolean;
    compiled_variadic_function?: any;
    compiled_function?: any;
    scratchRootBuffer?: any;
    scratchBuffer?: any;
}
