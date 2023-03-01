// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import MonoWasmThreads from "consts:monoWasmThreads";
import { mono_wasm_fire_debugger_agent_message_with_data, mono_wasm_fire_debugger_agent_message_with_data_to_pause, mono_wasm_debugger_log, mono_wasm_add_dbg_command_received, mono_wasm_set_entrypoint_breakpoint } from "./debug";

import { mono_wasm_release_cs_owned_object } from "./gc-handles";
import { mono_wasm_load_icu_data } from "./icu";
import { mono_wasm_bind_cs_function } from "./invoke-cs";
import { mono_wasm_bind_js_function, mono_wasm_invoke_bound_function, mono_wasm_invoke_import } from "./invoke-js";
import { mono_interp_tier_prepare_jiterpreter } from "./jiterpreter";
import { mono_interp_jit_wasm_entry_trampoline, mono_interp_record_interp_entry } from "./jiterpreter-interp-entry";
import { mono_interp_jit_wasm_jit_call_trampoline, mono_interp_invoke_wasm_jit_call_trampoline, mono_interp_flush_jitcall_queue, mono_jiterp_do_jit_call_indirect } from "./jiterpreter-jit-call";
import { mono_wasm_typed_array_from_ref } from "./net6-legacy/buffers";
import {
    mono_wasm_invoke_js_blazor, mono_wasm_invoke_js_with_args_ref, mono_wasm_get_object_property_ref, mono_wasm_set_object_property_ref,
    mono_wasm_get_by_index_ref, mono_wasm_set_by_index_ref, mono_wasm_get_global_object_ref
} from "./net6-legacy/method-calls";
import { mono_wasm_marshal_promise } from "./marshal-to-js";
import { mono_wasm_pthread_on_pthread_attached } from "./pthreads/worker";
import { mono_set_timeout, schedule_background_exec } from "./scheduling";
import { mono_wasm_asm_loaded } from "./startup";
import { mono_wasm_diagnostic_server_on_server_thread_created } from "./diagnostics/server_pthread";
import { mono_wasm_diagnostic_server_on_runtime_server_init, mono_wasm_event_pipe_early_startup_callback } from "./diagnostics";
import { mono_wasm_diagnostic_server_stream_signal_work_available } from "./diagnostics/server_pthread/stream-queue";
import { mono_wasm_create_cs_owned_object_ref } from "./net6-legacy/cs-to-js";
import { mono_wasm_typed_array_to_array_ref } from "./net6-legacy/js-to-cs";
import { mono_wasm_trace_logger } from "./logging";
import { mono_wasm_profiler_leave, mono_wasm_profiler_enter } from "./profiler";

// the methods would be visible to EMCC linker
// --- keep in sync with dotnet.cjs.lib.js ---
const mono_wasm_threads_exports = !MonoWasmThreads ? undefined : {
    // mono-threads-wasm.c
    mono_wasm_pthread_on_pthread_attached,
    // diagnostics_server.c
    mono_wasm_diagnostic_server_on_server_thread_created,
    mono_wasm_diagnostic_server_on_runtime_server_init,
    mono_wasm_diagnostic_server_stream_signal_work_available,
};

// the methods would be visible to EMCC linker
// --- keep in sync with dotnet.cjs.lib.js ---
// --- keep in sync with dotnet.es6.lib.js ---
export function export_linker(): any {
    return {
        // mini-wasm.c
        mono_set_timeout,

        // mini-wasm-debugger.c
        mono_wasm_asm_loaded,
        mono_wasm_debugger_log,
        mono_wasm_add_dbg_command_received,
        mono_wasm_fire_debugger_agent_message_with_data,
        mono_wasm_fire_debugger_agent_message_with_data_to_pause,
        // mono-threads-wasm.c
        schedule_background_exec,

        // interp.c and jiterpreter.c
        mono_interp_tier_prepare_jiterpreter,
        mono_interp_record_interp_entry,
        mono_interp_jit_wasm_entry_trampoline,
        mono_interp_jit_wasm_jit_call_trampoline,
        mono_interp_invoke_wasm_jit_call_trampoline,
        mono_interp_flush_jitcall_queue,
        mono_jiterp_do_jit_call_indirect,

        mono_wasm_profiler_enter,
        mono_wasm_profiler_leave,

        // driver.c
        mono_wasm_invoke_js_blazor,
        mono_wasm_trace_logger,
        mono_wasm_set_entrypoint_breakpoint,
        mono_wasm_event_pipe_early_startup_callback,

        // corebindings.c
        mono_wasm_invoke_js_with_args_ref,
        mono_wasm_get_object_property_ref,
        mono_wasm_set_object_property_ref,
        mono_wasm_get_by_index_ref,
        mono_wasm_set_by_index_ref,
        mono_wasm_get_global_object_ref,
        mono_wasm_create_cs_owned_object_ref,
        mono_wasm_release_cs_owned_object,
        mono_wasm_typed_array_to_array_ref,
        mono_wasm_typed_array_from_ref,
        mono_wasm_bind_js_function,
        mono_wasm_invoke_bound_function,
        mono_wasm_invoke_import,
        mono_wasm_bind_cs_function,
        mono_wasm_marshal_promise,

        //  pal_icushim_static.c
        mono_wasm_load_icu_data,

        // threading exports, if threading is enabled
        ...mono_wasm_threads_exports,
    };
}
