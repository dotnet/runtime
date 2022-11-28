// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    MonoArray, MonoAssembly, MonoClass,
    MonoMethod, MonoObject, MonoString,
    MonoType, MonoObjectRef, MonoStringRef, JSMarshalerArguments
} from "./types";
import { ENVIRONMENT_IS_PTHREAD, Module } from "./imports";
import { VoidPtr, CharPtrPtr, Int32Ptr, CharPtr, ManagedPointer } from "./types/emscripten";

type SigLine = [lazy: boolean, name: string, returnType: string | null, argTypes?: string[], opts?: any];

// when the method is assigned/cached at usage, instead of being invoked directly from cwraps, it can't be marked lazy, because it would be re-bound on each call
const fn_signatures: SigLine[] = [
    // MONO
    [true, "mono_wasm_register_root", "number", ["number", "number", "string"]],
    [true, "mono_wasm_deregister_root", null, ["number"]],
    [true, "mono_wasm_string_get_data", null, ["number", "number", "number", "number"]],
    [true, "mono_wasm_string_get_data_ref", null, ["number", "number", "number", "number"]],
    [true, "mono_wasm_set_is_debugger_attached", "void", ["bool"]],
    [true, "mono_wasm_send_dbg_command", "bool", ["number", "number", "number", "number", "number"]],
    [true, "mono_wasm_send_dbg_command_with_parms", "bool", ["number", "number", "number", "number", "number", "number", "string"]],
    [true, "mono_wasm_setenv", null, ["string", "string"]],
    [true, "mono_wasm_parse_runtime_options", null, ["number", "number"]],
    [true, "mono_wasm_strdup", "number", ["string"]],
    [true, "mono_background_exec", null, []],
    [true, "mono_set_timeout_exec", null, []],
    [true, "mono_wasm_load_icu_data", "number", ["number"]],
    [true, "mono_wasm_get_icudt_name", "string", ["string"]],
    [false, "mono_wasm_add_assembly", "number", ["string", "number", "number"]],
    [true, "mono_wasm_add_satellite_assembly", "void", ["string", "string", "number", "number"]],
    [false, "mono_wasm_load_runtime", null, ["string", "number"]],
    [true, "mono_wasm_change_debugger_log_level", "void", ["number"]],

    // BINDING
    [true, "mono_wasm_get_corlib", "number", []],
    [true, "mono_wasm_assembly_load", "number", ["string"]],
    [true, "mono_wasm_find_corlib_class", "number", ["string", "string"]],
    [true, "mono_wasm_assembly_find_class", "number", ["number", "string", "string"]],
    [true, "mono_wasm_runtime_run_module_cctor", "void", ["number"]],
    [true, "mono_wasm_find_corlib_type", "number", ["string", "string"]],
    [true, "mono_wasm_assembly_find_type", "number", ["number", "string", "string"]],
    [true, "mono_wasm_assembly_find_method", "number", ["number", "string", "number"]],
    [true, "mono_wasm_invoke_method", "number", ["number", "number", "number", "number"]],
    [false, "mono_wasm_invoke_method_ref", "void", ["number", "number", "number", "number", "number"]],
    [true, "mono_wasm_string_get_utf8", "number", ["number"]],
    [true, "mono_wasm_string_from_utf16_ref", "void", ["number", "number", "number"]],
    [true, "mono_wasm_get_obj_type", "number", ["number"]],
    [true, "mono_wasm_array_length", "number", ["number"]],
    [true, "mono_wasm_array_length_ref", "number", ["number"]],
    [true, "mono_wasm_array_get", "number", ["number", "number"]],
    [true, "mono_wasm_array_get_ref", "void", ["number", "number", "number"]],
    [false, "mono_wasm_obj_array_new", "number", ["number"]],
    [false, "mono_wasm_obj_array_new_ref", "void", ["number", "number"]],
    [false, "mono_wasm_obj_array_set", "void", ["number", "number", "number"]],
    [false, "mono_wasm_obj_array_set_ref", "void", ["number", "number", "number"]],
    [true, "mono_wasm_register_bundled_satellite_assemblies", "void", []],
    [false, "mono_wasm_try_unbox_primitive_and_get_type_ref", "number", ["number", "number", "number"]],
    [true, "mono_wasm_box_primitive_ref", "void", ["number", "number", "number", "number"]],
    [true, "mono_wasm_intern_string_ref", "void", ["number"]],
    [true, "mono_wasm_assembly_get_entry_point", "number", ["number"]],
    [true, "mono_wasm_get_delegate_invoke_ref", "number", ["number"]],
    [true, "mono_wasm_string_array_new_ref", "void", ["number", "number"]],
    [true, "mono_wasm_typed_array_new_ref", "void", ["number", "number", "number", "number", "number"]],
    [true, "mono_wasm_class_get_type", "number", ["number"]],
    [true, "mono_wasm_type_get_class", "number", ["number"]],
    [true, "mono_wasm_get_type_name", "string", ["number"]],
    [true, "mono_wasm_get_type_aqn", "string", ["number"]],

    // MONO.diagnostics
    [true, "mono_wasm_event_pipe_enable", "bool", ["string", "number", "number", "string", "bool", "number"]],
    [true, "mono_wasm_event_pipe_session_start_streaming", "bool", ["number"]],
    [true, "mono_wasm_event_pipe_session_disable", "bool", ["number"]],
    [true, "mono_wasm_diagnostic_server_create_thread", "bool", ["string", "number"]],
    [true, "mono_wasm_diagnostic_server_thread_attach_to_runtime", "void", []],
    [true, "mono_wasm_diagnostic_server_post_resume_runtime", "void", []],
    [true, "mono_wasm_diagnostic_server_create_stream", "number", []],

    //DOTNET
    [true, "mono_wasm_string_from_js", "number", ["string"]],

    //INTERNAL
    [false, "mono_wasm_exit", "void", ["number"]],
    [true, "mono_wasm_getenv", "number", ["string"]],
    [true, "mono_wasm_set_main_args", "void", ["number", "number"]],
    [false, "mono_wasm_enable_on_demand_gc", "void", ["number"]],
    // These two need to be lazy because they may be missing
    [true, "mono_wasm_profiler_init_aot", "void", ["number"]],
    [true, "mono_wasm_profiler_init_browser", "void", ["number"]],
    [false, "mono_wasm_exec_regression", "number", ["number", "string"]],
    [false, "mono_wasm_invoke_method_bound", "number", ["number", "number"]],
    [true, "mono_wasm_write_managed_pointer_unsafe", "void", ["number", "number"]],
    [true, "mono_wasm_copy_managed_pointer", "void", ["number", "number"]],
    [true, "mono_wasm_i52_to_f64", "number", ["number", "number"]],
    [true, "mono_wasm_u52_to_f64", "number", ["number", "number"]],
    [true, "mono_wasm_f64_to_i52", "number", ["number", "number"]],
    [true, "mono_wasm_f64_to_u52", "number", ["number", "number"]],
    [true, "mono_wasm_method_get_name", "number", ["number"]],
    [true, "mono_wasm_method_get_full_name", "number", ["number"]],

    // jiterpreter
    [true, "mono_jiterp_get_trace_bailout_count", "number", ["number"]],
    [true, "mono_jiterp_value_copy", "void", ["number", "number", "number"]],
    [true, "mono_jiterp_get_offset_of_vtable_initialized_flag", "number", []],
    [true, "mono_jiterp_get_offset_of_array_data", "number", []],
    [false, "mono_jiterp_encode_leb52", "number", ["number", "number", "number"]],
    [false, "mono_jiterp_encode_leb64_ref", "number", ["number", "number", "number"]],
    [true, "mono_jiterp_type_is_byref", "number", ["number"]],
    [true, "mono_jiterp_get_size_of_stackval", "number", []],
    [true, "mono_jiterp_parse_option", "number", ["string"]],
    [true, "mono_jiterp_get_options_as_json", "number", []],
    [true, "mono_jiterp_get_options_version", "number", []],
    [true, "mono_jiterp_adjust_abort_count", "number", ["number", "number"]],
    [true, "mono_jiterp_register_jit_call_thunk", "void", ["number", "number"]],
    [true, "mono_jiterp_type_get_raw_value_size", "number", ["number"]],
    [true, "mono_jiterp_update_jit_call_dispatcher", "void", ["number"]],
];

export interface t_Cwraps {
    // MONO
    mono_wasm_register_root(start: VoidPtr, size: number, name: string): number;
    mono_wasm_deregister_root(addr: VoidPtr): void;
    mono_wasm_string_get_data_ref(stringRef: MonoStringRef, outChars: CharPtrPtr, outLengthBytes: Int32Ptr, outIsInterned: Int32Ptr): void;
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
    mono_wasm_load_runtime(unused: string, debugLevel: number): void;
    mono_wasm_change_debugger_log_level(value: number): void;

    /**
     * @deprecated Not GC or thread safe
     */
    mono_wasm_string_get_data(string: MonoString, outChars: CharPtrPtr, outLengthBytes: Int32Ptr, outIsInterned: Int32Ptr): void;

    // BINDING
    mono_wasm_get_corlib(): MonoAssembly;
    mono_wasm_assembly_load(name: string): MonoAssembly;
    mono_wasm_find_corlib_class(namespace: string, name: string): MonoClass;
    mono_wasm_assembly_find_class(assembly: MonoAssembly, namespace: string, name: string): MonoClass;
    mono_wasm_find_corlib_type(namespace: string, name: string): MonoType;
    mono_wasm_assembly_find_type(assembly: MonoAssembly, namespace: string, name: string): MonoType;
    mono_wasm_assembly_find_method(klass: MonoClass, name: string, args: number): MonoMethod;
    mono_wasm_invoke_method_ref(method: MonoMethod, this_arg: MonoObjectRef, params: VoidPtr, out_exc: MonoObjectRef, out_result: MonoObjectRef): void;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_wasm_string_get_utf8(str: MonoString): CharPtr;
    mono_wasm_string_from_utf16_ref(str: CharPtr, len: number, result: MonoObjectRef): void;
    mono_wasm_array_length(array: MonoArray): number;

    mono_wasm_array_length_ref(array: MonoObjectRef): number;
    mono_wasm_array_get_ref(array: MonoObjectRef, idx: number, result: MonoObjectRef): void;
    mono_wasm_obj_array_new_ref(size: number, result: MonoObjectRef): void;
    mono_wasm_obj_array_set_ref(array: MonoObjectRef, idx: number, obj: MonoObjectRef): void;
    mono_wasm_register_bundled_satellite_assemblies(): void;
    mono_wasm_try_unbox_primitive_and_get_type_ref(obj: MonoObjectRef, buffer: VoidPtr, buffer_size: number): number;
    mono_wasm_box_primitive_ref(klass: MonoClass, value: VoidPtr, value_size: number, result: MonoObjectRef): void;
    mono_wasm_intern_string_ref(strRef: MonoStringRef): void;
    mono_wasm_assembly_get_entry_point(assembly: MonoAssembly, idx: number): MonoMethod;
    mono_wasm_string_array_new_ref(size: number, result: MonoObjectRef): void;
    mono_wasm_typed_array_new_ref(arr: VoidPtr, length: number, size: number, type: number, result: MonoObjectRef): void;
    mono_wasm_class_get_type(klass: MonoClass): MonoType;
    mono_wasm_type_get_class(ty: MonoType): MonoClass;
    mono_wasm_get_delegate_invoke_ref(delegate: MonoObjectRef): MonoMethod;
    mono_wasm_get_type_name(ty: MonoType): string;
    mono_wasm_get_type_aqn(ty: MonoType): string;

    /**
     * @deprecated Not GC or thread safe
     */
    mono_wasm_get_obj_type(str: MonoObject): number;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_wasm_invoke_method(method: MonoMethod, this_arg: MonoObject, params: VoidPtr, out_exc: MonoObjectRef): MonoObject;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_wasm_obj_array_new(size: number): MonoArray;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_wasm_array_get(array: MonoArray, idx: number): MonoObject;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_wasm_obj_array_set(array: MonoArray, idx: number, obj: MonoObject): void;

    // MONO.diagnostics
    mono_wasm_event_pipe_enable(outputPath: string | null, stream: VoidPtr, bufferSizeInMB: number, providers: string, rundownRequested: boolean, outSessionId: VoidPtr): boolean;
    mono_wasm_event_pipe_session_start_streaming(sessionId: number): boolean;
    mono_wasm_event_pipe_session_disable(sessionId: number): boolean;
    mono_wasm_diagnostic_server_create_thread(websocketURL: string, threadIdOutPtr: VoidPtr): boolean;
    mono_wasm_diagnostic_server_thread_attach_to_runtime(): void;
    mono_wasm_diagnostic_server_post_resume_runtime(): void;
    mono_wasm_diagnostic_server_create_stream(): VoidPtr;

    //DOTNET
    /**
     * @deprecated Not GC or thread safe
     */
    mono_wasm_string_from_js(str: string): MonoString;

    //INTERNAL
    mono_wasm_exit(exit_code: number): number;
    mono_wasm_getenv(name: string): CharPtr;
    mono_wasm_enable_on_demand_gc(enable: number): void;
    mono_wasm_set_main_args(argc: number, argv: VoidPtr): void;
    mono_wasm_profiler_init_aot(desc: string): void;
    mono_wasm_profiler_init_browser(desc: string): void;
    mono_wasm_exec_regression(verbose_level: number, image: string): number;
    mono_wasm_invoke_method_bound(method: MonoMethod, args: JSMarshalerArguments): MonoString;
    mono_wasm_write_managed_pointer_unsafe(destination: VoidPtr | MonoObjectRef, pointer: ManagedPointer): void;
    mono_wasm_copy_managed_pointer(destination: VoidPtr | MonoObjectRef, source: VoidPtr | MonoObjectRef): void;
    mono_wasm_i52_to_f64(source: VoidPtr, error: Int32Ptr): number;
    mono_wasm_u52_to_f64(source: VoidPtr, error: Int32Ptr): number;
    mono_wasm_f64_to_i52(destination: VoidPtr, value: number): I52Error;
    mono_wasm_f64_to_u52(destination: VoidPtr, value: number): I52Error;
    mono_wasm_runtime_run_module_cctor(assembly: MonoAssembly): void;
    mono_wasm_method_get_name(method: MonoMethod): CharPtr;
    mono_wasm_method_get_full_name(method: MonoMethod): CharPtr;

    mono_jiterp_get_trace_bailout_count(reason: number): number;
    mono_jiterp_value_copy(destination: VoidPtr, source: VoidPtr, klass: MonoClass): void;
    mono_jiterp_get_offset_of_vtable_initialized_flag(): number;
    mono_jiterp_get_offset_of_array_data(): number;
    // Returns bytes written (or 0 if writing failed)
    mono_jiterp_encode_leb52 (destination: VoidPtr, value: number, valueIsSigned: number): number;
    // Returns bytes written (or 0 if writing failed)
    // Source is the address of a 64-bit int or uint
    mono_jiterp_encode_leb64_ref (destination: VoidPtr, source: VoidPtr, valueIsSigned: number): number;
    mono_jiterp_type_is_byref (type: MonoType): number;
    mono_jiterp_get_size_of_stackval (): number;
    mono_jiterp_type_get_raw_value_size (type: MonoType): number;
    mono_jiterp_parse_option (name: string): number;
    mono_jiterp_get_options_as_json (): number;
    mono_jiterp_get_options_version (): number;
    mono_jiterp_adjust_abort_count (opcode: number, delta: number): number;
    mono_jiterp_register_jit_call_thunk (cinfo: number, func: number): void;
    mono_jiterp_update_jit_call_dispatcher (fn: number): void;
}

const wrapped_c_functions: t_Cwraps = <any>{};

export default wrapped_c_functions;

// see src/mono/wasm/driver.c I52_ERROR_xxx
export const enum I52Error {
    NONE = 0,
    NON_INTEGRAL = 1,
    OUT_OF_RANGE = 2,
}

export function init_c_exports(): void {
    // init_c_exports is called very early in a pthread before Module.cwrap is available
    const alwaysLazy = !!ENVIRONMENT_IS_PTHREAD;
    for (const sig of fn_signatures) {
        const wf: any = wrapped_c_functions;
        const [lazy, name, returnType, argTypes, opts] = sig;
        if (lazy || alwaysLazy) {
            // lazy init on first run
            wf[name] = function (...args: any[]) {
                const fce = Module.cwrap(name, returnType, argTypes, opts);
                if (typeof (fce) !== "function")
                    throw new Error(`cwrap ${name} not found or not a function`);
                wf[name] = fce;
                return fce(...args);
            };
        } else {
            const fce = Module.cwrap(name, returnType, argTypes, opts);
            // throw would be preferable, but it causes really hard to debug startup errors and
            //  unhandled promise rejections so this is more useful
            if (typeof (fce) !== "function")
                console.error(`cwrap ${name} not found or not a function`);
            wf[name] = fce;
        }
    }
}
