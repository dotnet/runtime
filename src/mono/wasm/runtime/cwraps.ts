// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    assert,
    MonoArray, MonoAssembly, MonoClass,
    MonoMethod, MonoObject, MonoString,
    MonoType
} from "./types";
import { Module } from "./imports";
import { VoidPtr, CharPtrPtr, Int32Ptr, CharPtr } from "./types/emscripten";

const fn_signatures: [ident: string, returnType: string | null, argTypes?: string[], opts?: any][] = [
    // MONO
    ["mono_wasm_register_root", "number", ["number", "number", "string"]],
    ["mono_wasm_deregister_root", null, ["number"]],
    ["mono_wasm_string_get_data", null, ["number", "number", "number", "number"]],
    ["mono_wasm_set_is_debugger_attached", "void", ["bool"]],
    ["mono_wasm_send_dbg_command", "bool", ["number", "number", "number", "number", "number"]],
    ["mono_wasm_send_dbg_command_with_parms", "bool", ["number", "number", "number", "number", "number", "number", "string"]],
    ["mono_wasm_setenv", null, ["string", "string"]],
    ["mono_wasm_parse_runtime_options", null, ["number", "number"]],
    ["mono_wasm_strdup", "number", ["string"]],
    ["mono_background_exec", null, []],
    ["mono_set_timeout_exec", null, []],
    ["mono_wasm_load_icu_data", "number", ["number"]],
    ["mono_wasm_get_icudt_name", "string", ["string"]],
    ["mono_wasm_add_assembly", "number", ["string", "number", "number"]],
    ["mono_wasm_add_satellite_assembly", "void", ["string", "string", "number", "number"]],
    ["mono_wasm_load_runtime", null, ["string", "number"]],
    ["mono_wasm_exit", null, ["number"]],
    ["mono_wasm_change_debugger_log_level", "void", ["number"]],

    // BINDING
    ["mono_wasm_get_corlib", "number", []],
    ["mono_wasm_assembly_load", "number", ["string"]],
    ["mono_wasm_find_corlib_class", "number", ["string", "string"]],
    ["mono_wasm_assembly_find_class", "number", ["number", "string", "string"]],
    ["mono_wasm_find_corlib_type", "number", ["string", "string"]],
    ["mono_wasm_assembly_find_type", "number", ["number", "string", "string"]],
    ["mono_wasm_assembly_find_method", "number", ["number", "string", "number"]],
    ["mono_wasm_invoke_method", "number", ["number", "number", "number", "number"]],
    ["mono_wasm_string_get_utf8", "number", ["number"]],
    ["mono_wasm_string_from_utf16", "number", ["number", "number"]],
    ["mono_wasm_get_obj_type", "number", ["number"]],
    ["mono_wasm_array_length", "number", ["number"]],
    ["mono_wasm_array_get", "number", ["number", "number"]],
    ["mono_wasm_obj_array_new", "number", ["number"]],
    ["mono_wasm_obj_array_set", "void", ["number", "number", "number"]],
    ["mono_wasm_register_bundled_satellite_assemblies", "void", []],
    ["mono_wasm_try_unbox_primitive_and_get_type", "number", ["number", "number", "number"]],
    ["mono_wasm_box_primitive", "number", ["number", "number", "number"]],
    ["mono_wasm_intern_string", "number", ["number"]],
    ["mono_wasm_assembly_get_entry_point", "number", ["number"]],
    ["mono_wasm_get_delegate_invoke", "number", ["number"]],
    ["mono_wasm_string_array_new", "number", ["number"]],
    ["mono_wasm_typed_array_new", "number", ["number", "number", "number", "number"]],
    ["mono_wasm_class_get_type", "number", ["number"]],
    ["mono_wasm_type_get_class", "number", ["number"]],
    ["mono_wasm_get_type_name", "string", ["number"]],
    ["mono_wasm_get_type_aqn", "string", ["number"]],
    ["mono_wasm_unbox_rooted", "number", ["number"]],

    //DOTNET
    ["mono_wasm_string_from_js", "number", ["string"]],

    //INTERNAL
    ["mono_wasm_exit", "void", ["number"]],
    ["mono_wasm_set_main_args", "void", ["number", "number"]],
    ["mono_wasm_enable_on_demand_gc", "void", ["number"]],
    ["mono_profiler_init_aot", "void", ["number"]],
    ["mono_wasm_exec_regression", "number", ["number", "string"]],
];

export interface t_Cwraps {
    // MONO
    mono_wasm_register_root(start: VoidPtr, size: number, name: string): number;
    mono_wasm_deregister_root(addr: VoidPtr): void;
    mono_wasm_string_get_data(string: MonoString, outChars: CharPtrPtr, outLengthBytes: Int32Ptr, outIsInterned: Int32Ptr): void;
    mono_wasm_set_is_debugger_attached(value: boolean): void;
    mono_wasm_send_dbg_command(id: number, command_set: number, command: number, data: VoidPtr, size: number): boolean;
    mono_wasm_send_dbg_command_with_parms(id: number, command_set: number, command: number, data: VoidPtr, size: number, valtype: number, newvalue: string): boolean;
    mono_wasm_setenv(name: string, value: string): void;
    mono_wasm_strdup(value: string): number;
    mono_wasm_parse_runtime_options(length: number, argv: VoidPtr): void;
    mono_background_exec(): void;
    mono_set_timeout_exec(): void;
    mono_wasm_load_icu_data(offset: VoidPtr): number;
    mono_wasm_get_icudt_name(name: string): string;
    mono_wasm_add_assembly(name: string, data: VoidPtr, size: number): number;
    mono_wasm_add_satellite_assembly(name: string, culture: string, data: VoidPtr, size: number): void;
    mono_wasm_load_runtime(unused: string, debug_level: number): void;
    mono_wasm_change_debugger_log_level(value: number): void;

    // BINDING
    mono_wasm_get_corlib(): MonoAssembly;
    mono_wasm_assembly_load(name: string): MonoAssembly;
    mono_wasm_find_corlib_class(namespace: string, name: string): MonoClass;
    mono_wasm_assembly_find_class(assembly: MonoAssembly, namespace: string, name: string): MonoClass;
    mono_wasm_find_corlib_type(namespace: string, name: string): MonoType;
    mono_wasm_assembly_find_type(assembly: MonoAssembly, namespace: string, name: string): MonoType;
    mono_wasm_assembly_find_method(klass: MonoClass, name: string, args: number): MonoMethod;
    mono_wasm_invoke_method(method: MonoMethod, this_arg: MonoObject, params: VoidPtr, out_exc: VoidPtr): MonoObject;
    mono_wasm_string_get_utf8(str: MonoString): CharPtr;
    mono_wasm_string_from_utf16(str: CharPtr, len: number): MonoString;
    mono_wasm_get_obj_type(str: MonoObject): number;
    mono_wasm_array_length(array: MonoArray): number;
    mono_wasm_array_get(array: MonoArray, idx: number): MonoObject;
    mono_wasm_obj_array_new(size: number): MonoArray;
    mono_wasm_obj_array_set(array: MonoArray, idx: number, obj: MonoObject): void;
    mono_wasm_register_bundled_satellite_assemblies(): void;
    mono_wasm_try_unbox_primitive_and_get_type(obj: MonoObject, buffer: VoidPtr, buffer_size: number): number;
    mono_wasm_box_primitive(klass: MonoClass, value: VoidPtr, value_size: number): MonoObject;
    mono_wasm_intern_string(str: MonoString): MonoString;
    mono_wasm_assembly_get_entry_point(assembly: MonoAssembly): MonoMethod;
    mono_wasm_get_delegate_invoke(delegate: MonoObject): MonoMethod;
    mono_wasm_string_array_new(size: number): MonoArray;
    mono_wasm_typed_array_new(arr: VoidPtr, length: number, size: number, type: number): MonoArray;
    mono_wasm_class_get_type(klass: MonoClass): MonoType;
    mono_wasm_type_get_class(ty: MonoType): MonoClass;
    mono_wasm_get_type_name(ty: MonoType): string;
    mono_wasm_get_type_aqn(ty: MonoType): string;
    mono_wasm_unbox_rooted(obj: MonoObject): VoidPtr;

    //DOTNET
    mono_wasm_string_from_js(str: string): MonoString;

    //INTERNAL
    mono_wasm_exit(exit_code: number): number;
    mono_wasm_enable_on_demand_gc(enable: number): void;
    mono_wasm_set_main_args(argc: number, argv: VoidPtr): void;
    mono_profiler_init_aot(desc: string): void;
    mono_wasm_exec_regression(verbose_level: number, image: string): number;
}

const wrapped_c_functions: t_Cwraps = <any>{};
for (const sig of fn_signatures) {
    const wf: any = wrapped_c_functions;
    // lazy init on first run
    wf[sig[0]] = function (...args: any[]) {
        const fce = Module.cwrap(sig[0], sig[1], sig[2], sig[3]);
        wf[sig[0]] = fce;
        return fce(...args);
    };
}

export default wrapped_c_functions;
export function wrap_c_function(name: string): Function {
    const wf: any = wrapped_c_functions;
    const sig = fn_signatures.find(s => s[0] === name);
    assert(sig, () => `Function ${name} not found`);
    const fce = Module.cwrap(sig[0], sig[1], sig[2], sig[3]);
    wf[sig[0]] = fce;
    return fce;
}