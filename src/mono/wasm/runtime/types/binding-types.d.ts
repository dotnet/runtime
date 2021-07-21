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
    mono_wasm_try_unbox_primitive_and_get_type (a: number, b: number): CPrimativeTypes;
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
    _interned_js_string_table: Map<string | Symbol, number>;
    _method_descriptions: Map<number, string>;
    _signature_converters: Map<string, Converter>;
    _primitive_converters: Map<string, Converter>;
    scratchBuffer: number;
    setup_js_cont: number;
    create_tcs: number;
    set_tcs_result: number;
    set_tcs_failure: number;
    tcs_get_task_and_bind: number;
    get_call_sig: number;
    get_date_value: number;
    create_date_time: number;
    create_uri: number;
    mono_wasm_object_registry: JSObject[];

    _unbind_raw_obj_and_free: (handle: number) => void;
    _get_raw_mono_obj: (gchandle: number, should_add_in_flight: number) => number;
    _get_js_id: (mono_id: number) => number;
    _bind_existing_obj: (mono_obj: number, js_id: number) => number;
    _bind_js_obj: (js_obj_id: number, ownsHandle: boolean, type: number) => number;
    _bind_core_clr_obj: (js_id: number, gc_handle: number) => number;
    _object_to_string: (obj: number) => any;
    _is_simple_array: (obj: any) => boolean;
}

// NAMESPACES ///////////////////////////////////////////////////////////////////////////////
declare var BINDING: typeof BindingSupportLib.$BINDING & BINDING_C_FUNCS & BINDING_VARS;

// OTHER TYPES ///////////////////////////////////////////////////////////////////////
type JSObject = {
    __mono_gchandle__?: number, 
    __mono_jshandle__?: number,
    __mono_bound_tcs__?: number,
    __owns_handle__?: boolean,
    __mono_delegate_alive__?: boolean,
    __mono_js_cont__?: number,
    is_mono_bridged_obj?: boolean,
}

type Converter = {
    steps: {
        convert: boolean;
        needs_root: boolean;
        indirect: ConverterStepIndirects;
        size: number;
    }[];
    size: number;
    args_marshal?: ArgsMarshalString;
    is_result_definitely_unmarshaled?: boolean;
    is_result_possibly_unmarshaled?: boolean;
    result_unmarshaled_if_argc?: number;
    needs_root_buffer?: boolean;
    name?: string;
    needs_root?: boolean;
    compiled_variadic_function?: Function;
    compiled_function?: Function;
    scratchRootBuffer?: WasmRootBuffer;
    scratchBuffer?: number;
    has_warned_about_signature?: boolean;
}

// Note that since these are annoated as `declare const enum` they are replaces by tsc with their raw value during compilation
declare const enum ConverterStepIndirects {
    UInt32 = "u32",
    Int32 = "i32",
    Float = "float",
    Float64 = "double",
    Int64 = "i64",
}

declare const enum ArgsMarshal {
    Int32 = "i", // int32
    Int32Enum = "j", // int32 - Enum with underlying type of int32
    Int64 = "l", // int64
    Int64Enum = "k", // int64 - Enum with underlying type of int64
    Float32 = "f", // float
    Float64 = "d", // double
    String = "s", // string
    Char = "s", // interned string
    JSObj = "o", // js object will be converted to a C# object (this will box numbers/bool/promises)
    MONOObj = "m", // raw mono object. Don't use it unless you know what you're doing
}

// to suppress marshaling of the return value, place '!' at the end of args_marshal, i.e. 'ii!' instead of 'ii'
type _ExtraArgsMarshalOperators = "!" | "";

// TODO make this more efficient so we can add more parameters (currently it only checks up to 4). One option is to add a
// blank to the ArgsMarshal enum but that doesn't solve the TS limit of number of options in 1 type
// Take the 2 marshaling enums and convert to all the valid strings for type checking. 
type ArgsMarshalString = 
                      `${ArgsMarshal}${_ExtraArgsMarshalOperators}` 
                    | `${ArgsMarshal}${ArgsMarshal}${_ExtraArgsMarshalOperators}` 
                    | `${ArgsMarshal}${ArgsMarshal}${ArgsMarshal}${_ExtraArgsMarshalOperators}`
                    | `${ArgsMarshal}${ArgsMarshal}${ArgsMarshal}${ArgsMarshal}${_ExtraArgsMarshalOperators}`;


declare const enum CNonPrimativeTypes {
    String = 3,
    VTS = 4, // throws errors due to "no idea on how to unbox value types"
    Delegate = 5,
    Task = 6,
    Ref = 7,
    Int8Array = 10, // unimplemented
    Uint8Array = 11, // unimplemented
    Uint8ClampedArray = 12, // unimplemented
    Int16Array = 13, // unimplemented
    UInt16Array = 14, // unimplemented
    Int32Array = 15, // unimplemented
    UInt32Array = 16, // unimplemented
    Float32Array = 17, // unimplemented
    Float64Array = 18, // unimplemented
    DateTime = 20,
    DateTimeOffset = 21,
    Uri = 22,
    SafeHandle = 23,
    Int64 = 26, // TODO: Fix this once emscripten offers HEAPI64/HEAPU64 or can return them, currently throws an error
    UInt64 = 27, // TODO: Fix this once emscripten offers HEAPI64/HEAPU64 or can return them, currently throws an error
    Char = 29,
    Undefined = 30
}

declare const enum CPrimativeTypes {
    Int = 1,
    Float64 = 2,
    Bool = 8,
    Float32 = 24,
    UInt32 = 25,
    Char = 28
}

declare const enum JSTypedArrays {
    Int8Array = 5,
    Uint8Array = 6,
    Int16Array = 7,
    Uint16Array = 8,
    Int32Array = 9,
    Uint32Array = 10,
    Float32Array = 13,
    Float64Array = 14,
    Uint8ClampedArray = 15,
}
