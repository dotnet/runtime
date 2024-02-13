// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import type {
    MonoAssembly, MonoClass,
    MonoMethod, MonoObject,
    MonoType, MonoObjectRef, MonoStringRef, JSMarshalerArguments
} from "./types/internal";
import type { VoidPtr, CharPtrPtr, Int32Ptr, CharPtr, ManagedPointer } from "./types/emscripten";
import { Module, runtimeHelpers } from "./globals";
import { mono_log_error } from "./logging";
import { mono_assert } from "./globals";

type SigLine = [lazyOrSkip: boolean | (() => boolean), name: string, returnType: string | null, argTypes?: string[], opts?: any];

const threading_cwraps: SigLine[] = WasmEnableThreads ? [
    // MONO.diagnostics
    [true, "mono_wasm_event_pipe_enable", "bool", ["string", "number", "number", "string", "bool", "number"]],
    [true, "mono_wasm_event_pipe_session_start_streaming", "bool", ["number"]],
    [true, "mono_wasm_event_pipe_session_disable", "bool", ["number"]],
    [true, "mono_wasm_diagnostic_server_create_thread", "bool", ["string", "number"]],
    [true, "mono_wasm_diagnostic_server_thread_attach_to_runtime", "void", []],
    [true, "mono_wasm_diagnostic_server_post_resume_runtime", "void", []],
    [true, "mono_wasm_diagnostic_server_create_stream", "number", []],
    [false, "mono_wasm_init_finalizer_thread", null, []],
] : [];

// when the method is assigned/cached at usage, instead of being invoked directly from cwraps, it can't be marked lazy, because it would be re-bound on each call
const fn_signatures: SigLine[] = [
    [true, "mono_wasm_register_root", "number", ["number", "number", "string"]],
    [true, "mono_wasm_deregister_root", null, ["number"]],
    [true, "mono_wasm_string_get_data_ref", null, ["number", "number", "number", "number"]],
    [true, "mono_wasm_set_is_debugger_attached", "void", ["bool"]],
    [true, "mono_wasm_send_dbg_command", "bool", ["number", "number", "number", "number", "number"]],
    [true, "mono_wasm_send_dbg_command_with_parms", "bool", ["number", "number", "number", "number", "number", "number", "string"]],
    [true, "mono_wasm_setenv", null, ["string", "string"]],
    [true, "mono_wasm_parse_runtime_options", null, ["number", "number"]],
    [true, "mono_wasm_strdup", "number", ["string"]],
    [true, "mono_background_exec", null, []],
    [true, "mono_wasm_execute_timer", null, []],
    [true, "mono_wasm_load_icu_data", "number", ["number"]],
    [false, "mono_wasm_add_assembly", "number", ["string", "number", "number"]],
    [true, "mono_wasm_add_satellite_assembly", "void", ["string", "string", "number", "number"]],
    [false, "mono_wasm_load_runtime", null, ["string", "number"]],
    [true, "mono_wasm_change_debugger_log_level", "void", ["number"]],

    [true, "mono_wasm_assembly_load", "number", ["string"]],
    [true, "mono_wasm_assembly_find_class", "number", ["number", "string", "string"]],
    [true, "mono_wasm_runtime_run_module_cctor", "void", ["number"]],
    [true, "mono_wasm_assembly_find_method", "number", ["number", "string", "number"]],
    [false, "mono_wasm_invoke_method_ref", "void", ["number", "number", "number", "number", "number"]],
    [true, "mono_wasm_string_from_utf16_ref", "void", ["number", "number", "number"]],
    [true, "mono_wasm_intern_string_ref", "void", ["number"]],
    [true, "mono_wasm_assembly_get_entry_point", "number", ["number", "number"]],

    [false, "mono_wasm_exit", "void", ["number"]],
    [false, "mono_wasm_abort", "void", []],
    [true, "mono_wasm_getenv", "number", ["string"]],
    [true, "mono_wasm_set_main_args", "void", ["number", "number"]],
    // These two need to be lazy because they may be missing
    [() => !runtimeHelpers.emscriptenBuildOptions.enableAotProfiler, "mono_wasm_profiler_init_aot", "void", ["string"]],
    [() => !runtimeHelpers.emscriptenBuildOptions.enableBrowserProfiler, "mono_wasm_profiler_init_aot", "void", ["string"]],
    [true, "mono_wasm_profiler_init_browser", "void", ["number"]],
    [false, "mono_wasm_exec_regression", "number", ["number", "string"]],
    [false, "mono_wasm_invoke_method", "number", ["number", "number", "number"]],
    [true, "mono_wasm_write_managed_pointer_unsafe", "void", ["number", "number"]],
    [true, "mono_wasm_copy_managed_pointer", "void", ["number", "number"]],
    [true, "mono_wasm_i52_to_f64", "number", ["number", "number"]],
    [true, "mono_wasm_u52_to_f64", "number", ["number", "number"]],
    [true, "mono_wasm_f64_to_i52", "number", ["number", "number"]],
    [true, "mono_wasm_f64_to_u52", "number", ["number", "number"]],
    [true, "mono_wasm_method_get_name", "number", ["number"]],
    [true, "mono_wasm_method_get_full_name", "number", ["number"]],
    [true, "mono_wasm_gc_lock", "void", []],
    [true, "mono_wasm_gc_unlock", "void", []],
    [true, "mono_wasm_get_i32_unaligned", "number", ["number"]],
    [true, "mono_wasm_get_f32_unaligned", "number", ["number"]],
    [true, "mono_wasm_get_f64_unaligned", "number", ["number"]],
    [true, "mono_wasm_read_as_bool_or_null_unsafe", "number", ["number"]],

    // jiterpreter
    [true, "mono_jiterp_trace_bailout", "void", ["number"]],
    [true, "mono_jiterp_get_trace_bailout_count", "number", ["number"]],
    [true, "mono_jiterp_value_copy", "void", ["number", "number", "number"]],
    [true, "mono_jiterp_get_member_offset", "number", ["number"]],
    [true, "mono_jiterp_encode_leb52", "number", ["number", "number", "number"]],
    [true, "mono_jiterp_encode_leb64_ref", "number", ["number", "number", "number"]],
    [true, "mono_jiterp_encode_leb_signed_boundary", "number", ["number", "number", "number"]],
    [true, "mono_jiterp_write_number_unaligned", "void", ["number", "number", "number"]],
    [true, "mono_jiterp_type_is_byref", "number", ["number"]],
    [true, "mono_jiterp_get_size_of_stackval", "number", []],
    [true, "mono_jiterp_parse_option", "number", ["string"]],
    [true, "mono_jiterp_get_options_as_json", "number", []],
    [true, "mono_jiterp_get_options_version", "number", []],
    [true, "mono_jiterp_adjust_abort_count", "number", ["number", "number"]],
    [true, "mono_jiterp_register_jit_call_thunk", "void", ["number", "number"]],
    [true, "mono_jiterp_type_get_raw_value_size", "number", ["number"]],
    [true, "mono_jiterp_get_signature_has_this", "number", ["number"]],
    [true, "mono_jiterp_get_signature_return_type", "number", ["number"]],
    [true, "mono_jiterp_get_signature_param_count", "number", ["number"]],
    [true, "mono_jiterp_get_signature_params", "number", ["number"]],
    [true, "mono_jiterp_type_to_ldind", "number", ["number"]],
    [true, "mono_jiterp_type_to_stind", "number", ["number"]],
    [true, "mono_jiterp_imethod_to_ftnptr", "number", ["number"]],
    [true, "mono_jiterp_debug_count", "number", []],
    [true, "mono_jiterp_get_trace_hit_count", "number", ["number"]],
    [true, "mono_jiterp_get_polling_required_address", "number", []],
    [true, "mono_jiterp_get_rejected_trace_count", "number", []],
    [true, "mono_jiterp_boost_back_branch_target", "void", ["number"]],
    [true, "mono_jiterp_is_imethod_var_address_taken", "number", ["number", "number"]],
    [true, "mono_jiterp_get_opcode_value_table_entry", "number", ["number"]],
    [true, "mono_jiterp_get_simd_intrinsic", "number", ["number", "number"]],
    [true, "mono_jiterp_get_simd_opcode", "number", ["number", "number"]],
    [true, "mono_jiterp_get_arg_offset", "number", ["number", "number", "number"]],
    [true, "mono_jiterp_get_opcode_info", "number", ["number", "number"]],
    [true, "mono_wasm_is_zero_page_reserved", "number", []],
    [true, "mono_jiterp_is_special_interface", "number", ["number"]],
    [true, "mono_jiterp_initialize_table", "void", ["number", "number", "number"]],
    [true, "mono_jiterp_allocate_table_entry", "number", ["number"]],
    [true, "mono_jiterp_get_interp_entry_func", "number", ["number"]],
    [true, "mono_jiterp_get_counter", "number", ["number"]],
    [true, "mono_jiterp_modify_counter", "number", ["number", "number"]],
    [true, "mono_jiterp_tlqueue_next", "number", ["number"]],
    [true, "mono_jiterp_tlqueue_add", "number", ["number", "number"]],
    [true, "mono_jiterp_tlqueue_clear", "void", ["number"]],
    [true, "mono_jiterp_begin_catch", "void", ["number"]],
    [true, "mono_jiterp_end_catch", "void", []],
    [true, "mono_interp_pgo_load_table", "number", ["number", "number"]],
    [true, "mono_interp_pgo_save_table", "number", ["number", "number"]],

    ...threading_cwraps,
];

export interface t_ThreadingCwraps {
    // MONO.diagnostics
    mono_wasm_event_pipe_enable(outputPath: string | null, stream: VoidPtr, bufferSizeInMB: number, providers: string, rundownRequested: boolean, outSessionId: VoidPtr): boolean;
    mono_wasm_event_pipe_session_start_streaming(sessionId: number): boolean;
    mono_wasm_event_pipe_session_disable(sessionId: number): boolean;
    mono_wasm_diagnostic_server_create_thread(websocketURL: string, threadIdOutPtr: VoidPtr): boolean;
    mono_wasm_diagnostic_server_thread_attach_to_runtime(): void;
    mono_wasm_diagnostic_server_post_resume_runtime(): void;
    mono_wasm_diagnostic_server_create_stream(): VoidPtr;
    mono_wasm_init_finalizer_thread(): void;
}

export interface t_ProfilerCwraps {
    mono_wasm_profiler_init_aot(desc: string): void;
    mono_wasm_profiler_init_browser(desc: string): void;
}

export interface t_Cwraps {
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
    mono_wasm_execute_timer(): void;
    mono_wasm_load_icu_data(offset: VoidPtr): number;
    mono_wasm_add_assembly(name: string, data: VoidPtr, size: number): number;
    mono_wasm_add_satellite_assembly(name: string, culture: string, data: VoidPtr, size: number): void;
    mono_wasm_load_runtime(debugLevel: number): void;
    mono_wasm_change_debugger_log_level(value: number): void;

    mono_wasm_assembly_load(name: string): MonoAssembly;
    mono_wasm_assembly_find_class(assembly: MonoAssembly, namespace: string, name: string): MonoClass;
    mono_wasm_assembly_find_method(klass: MonoClass, name: string, args: number): MonoMethod;
    mono_wasm_invoke_method_ref(method: MonoMethod, this_arg: MonoObjectRef, params: VoidPtr, out_exc: MonoObjectRef, out_result: MonoObjectRef): void;
    mono_wasm_string_from_utf16_ref(str: CharPtr, len: number, result: MonoObjectRef): void;
    mono_wasm_assembly_get_entry_point(assembly: MonoAssembly, idx: number): MonoMethod;
    mono_wasm_intern_string_ref(strRef: MonoStringRef): void;

    mono_wasm_exit(exit_code: number): void;
    mono_wasm_abort(): void;
    mono_wasm_getenv(name: string): CharPtr;
    mono_wasm_set_main_args(argc: number, argv: VoidPtr): void;
    mono_wasm_exec_regression(verbose_level: number, image: string): number;
    mono_wasm_invoke_method(method: MonoMethod, args: JSMarshalerArguments, fail: MonoStringRef): number;
    mono_wasm_write_managed_pointer_unsafe(destination: VoidPtr | MonoObjectRef, pointer: ManagedPointer): void;
    mono_wasm_copy_managed_pointer(destination: VoidPtr | MonoObjectRef, source: VoidPtr | MonoObjectRef): void;
    mono_wasm_i52_to_f64(source: VoidPtr, error: Int32Ptr): number;
    mono_wasm_u52_to_f64(source: VoidPtr, error: Int32Ptr): number;
    mono_wasm_f64_to_i52(destination: VoidPtr, value: number): I52Error;
    mono_wasm_f64_to_u52(destination: VoidPtr, value: number): I52Error;
    mono_wasm_runtime_run_module_cctor(assembly: MonoAssembly): void;
    mono_wasm_method_get_name(method: MonoMethod): CharPtr;
    mono_wasm_method_get_full_name(method: MonoMethod): CharPtr;
    mono_wasm_gc_lock(): void;
    mono_wasm_gc_unlock(): void;
    mono_wasm_get_i32_unaligned(source: VoidPtr): number;
    mono_wasm_get_f32_unaligned(source: VoidPtr): number;
    mono_wasm_get_f64_unaligned(source: VoidPtr): number;
    mono_wasm_read_as_bool_or_null_unsafe(obj: MonoObject): number;

    mono_jiterp_trace_bailout(reason: number): void;
    mono_jiterp_get_trace_bailout_count(reason: number): number;
    mono_jiterp_value_copy(destination: VoidPtr, source: VoidPtr, klass: MonoClass): void;
    mono_jiterp_get_member_offset(id: number): number;
    // Returns bytes written (or 0 if writing failed)
    mono_jiterp_encode_leb52(destination: VoidPtr, value: number, valueIsSigned: number): number;
    // Returns bytes written (or 0 if writing failed)
    // Source is the address of a 64-bit int or uint
    mono_jiterp_encode_leb64_ref(destination: VoidPtr, source: VoidPtr, valueIsSigned: number): number;
    // Returns bytes written (or 0 if writing failed)
    // bits is either 32 or 64 (the size of the value)
    // sign is >= 0 for INTnn_MAX and < 0 for INTnn_MIN
    mono_jiterp_encode_leb_signed_boundary(destination: VoidPtr, bits: number, sign: number): number;
    mono_jiterp_type_is_byref(type: MonoType): number;
    mono_jiterp_get_size_of_stackval(): number;
    mono_jiterp_type_get_raw_value_size(type: MonoType): number;
    mono_jiterp_parse_option(name: string): number;
    mono_jiterp_get_options_as_json(): number;
    mono_jiterp_get_options_version(): number;
    mono_jiterp_adjust_abort_count(opcode: number, delta: number): number;
    mono_jiterp_register_jit_call_thunk(cinfo: number, func: number): void;
    mono_jiterp_get_signature_has_this(sig: VoidPtr): number;
    mono_jiterp_get_signature_return_type(sig: VoidPtr): MonoType;
    mono_jiterp_get_signature_param_count(sig: VoidPtr): number;
    mono_jiterp_get_signature_params(sig: VoidPtr): VoidPtr;
    mono_jiterp_type_to_ldind(type: MonoType): number;
    mono_jiterp_type_to_stind(type: MonoType): number;
    mono_jiterp_imethod_to_ftnptr(imethod: VoidPtr): VoidPtr;
    mono_jiterp_debug_count(): number;
    mono_jiterp_get_trace_hit_count(traceIndex: number): number;
    mono_jiterp_get_polling_required_address(): Int32Ptr;
    mono_jiterp_write_number_unaligned(destination: VoidPtr, value: number, mode: number): void;
    mono_jiterp_get_rejected_trace_count(): number;
    mono_jiterp_boost_back_branch_target(destination: number): void;
    mono_jiterp_is_imethod_var_address_taken(imethod: VoidPtr, offsetBytes: number): number;
    mono_jiterp_get_opcode_value_table_entry(opcode: number): number;
    mono_jiterp_get_simd_intrinsic(arity: number, index: number): VoidPtr;
    mono_jiterp_get_simd_opcode(arity: number, index: number): number;
    mono_jiterp_get_arg_offset(imethod: number, sig: number, index: number): number;
    mono_jiterp_get_opcode_info(opcode: number, type: number): number;
    mono_wasm_is_zero_page_reserved(): number;
    mono_jiterp_is_special_interface(klass: number): number;
    mono_jiterp_initialize_table(type: number, firstIndex: number, lastIndex: number): void;
    mono_jiterp_allocate_table_entry(type: number): number;
    mono_jiterp_get_interp_entry_func(type: number): number;
    mono_jiterp_get_counter(counter: number): number;
    mono_jiterp_modify_counter(counter: number, delta: number): number;
    // returns value or, if queue is empty, VoidPtrNull
    mono_jiterp_tlqueue_next(queue: number): VoidPtr;
    // returns new size of queue after add
    mono_jiterp_tlqueue_add(queue: number, value: VoidPtr): number;
    mono_jiterp_tlqueue_clear(queue: number): void;
    mono_jiterp_begin_catch(ptr: number): void;
    mono_jiterp_end_catch(): void;
    mono_interp_pgo_load_table(buffer: VoidPtr, bufferSize: number): number;
    mono_interp_pgo_save_table(buffer: VoidPtr, bufferSize: number): number;
}

const wrapped_c_functions: t_Cwraps = <any>{};

export default wrapped_c_functions;
export const threads_c_functions: t_ThreadingCwraps & t_Cwraps = wrapped_c_functions as any;
export const profiler_c_functions: t_ProfilerCwraps & t_Cwraps = wrapped_c_functions as any;

// see src/mono/wasm/driver.c I52_ERROR_xxx
export const enum I52Error {
    NONE = 0,
    NON_INTEGRAL = 1,
    OUT_OF_RANGE = 2,
}

const fastCwrapTypes = ["void", "number", null];

function cwrap(name: string, returnType: string | null, argTypes: string[] | undefined, opts: any): Function {
    // Attempt to bypass emscripten's generated wrapper if it is safe to do so
    let fce =
        // Special cwrap options disable the fast path
        (typeof (opts) === "undefined") &&
            // Only attempt to do fast calls if all the args and the return type are either number or void
            (fastCwrapTypes.indexOf(returnType) >= 0) &&
            (!argTypes || argTypes.every(atype => fastCwrapTypes.indexOf(atype) >= 0)) &&
            // Module["asm"] may not be defined yet if we are early enough in the startup process
            //  in that case, we need to rely on emscripten's lazy wrappers
            Module["asm"]
            ? <Function>((<any>Module["asm"])[name])
            : undefined;

    // If the argument count for the wasm function doesn't match the signature, fall back to cwrap
    if (fce && argTypes && (fce.length !== argTypes.length)) {
        mono_log_error(`argument count mismatch for cwrap ${name}`);
        fce = undefined;
    }

    // We either failed to find the raw wasm func or for some reason we can't use it directly
    if (typeof (fce) !== "function")
        fce = Module.cwrap(name, returnType, argTypes, opts);

    if (typeof (fce) !== "function") {
        const msg = `cwrap ${name} not found or not a function`;
        throw new Error(msg);
    }
    return fce;
}

export function init_c_exports(): void {
    const fns = [...fn_signatures];
    for (const sig of fns) {
        const wf: any = wrapped_c_functions;
        const [lazyOrSkip, name, returnType, argTypes, opts] = sig;
        const maybeSkip = typeof lazyOrSkip === "function";
        if (lazyOrSkip === true || maybeSkip) {
            // lazy init on first run
            wf[name] = function (...args: any[]) {
                const isNotSkipped = !maybeSkip || !lazyOrSkip();
                mono_assert(isNotSkipped, () => `cwrap ${name} should not be called when binding was skipped`);
                const fce = cwrap(name, returnType, argTypes, opts);
                wf[name] = fce;
                return fce(...args);
            };
        } else {
            const fce = cwrap(name, returnType, argTypes, opts);
            wf[name] = fce;
        }
    }
}
