// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import MonoWasmThreads from "consts:monoWasmThreads";
import WasmEnableLegacyJsInterop from "consts:wasmEnableLegacyJsInterop";

import { mono_wasm_debugger_log, mono_wasm_add_dbg_command_received, mono_wasm_set_entrypoint_breakpoint, mono_wasm_fire_debugger_agent_message_with_data, mono_wasm_fire_debugger_agent_message_with_data_to_pause } from "./debug";
import { mono_wasm_release_cs_owned_object } from "./gc-handles";
import { mono_wasm_bind_cs_function } from "./invoke-cs";
import { mono_wasm_bind_js_function, mono_wasm_invoke_bound_function, mono_wasm_invoke_import } from "./invoke-js";
import { mono_interp_tier_prepare_jiterpreter } from "./jiterpreter";
import { mono_interp_jit_wasm_entry_trampoline, mono_interp_record_interp_entry } from "./jiterpreter-interp-entry";
import { mono_interp_jit_wasm_jit_call_trampoline, mono_interp_invoke_wasm_jit_call_trampoline, mono_interp_flush_jitcall_queue, mono_jiterp_do_jit_call_indirect } from "./jiterpreter-jit-call";
import { mono_wasm_marshal_promise } from "./marshal-to-js";
import { mono_wasm_eventloop_has_unsettled_interop_promises } from "./pthreads/shared/eventloop";
import { mono_wasm_pthread_on_pthread_attached, mono_wasm_pthread_on_pthread_detached } from "./pthreads/worker";
import { mono_wasm_schedule_timer, schedule_background_exec } from "./scheduling";
import { mono_wasm_asm_loaded } from "./startup";
import { mono_wasm_diagnostic_server_on_server_thread_created } from "./diagnostics/server_pthread";
import { mono_wasm_diagnostic_server_on_runtime_server_init, mono_wasm_event_pipe_early_startup_callback } from "./diagnostics";
import { mono_wasm_diagnostic_server_stream_signal_work_available } from "./diagnostics/server_pthread/stream-queue";
import { mono_log_debug, mono_log_warn, mono_wasm_trace_logger } from "./logging";
import { mono_wasm_profiler_leave, mono_wasm_profiler_enter } from "./profiler";
import { mono_wasm_change_case, mono_wasm_change_case_invariant } from "./hybrid-globalization/change-case";
import { mono_wasm_compare_string, mono_wasm_ends_with, mono_wasm_starts_with, mono_wasm_index_of } from "./hybrid-globalization/collations";
import { mono_wasm_get_calendar_info } from "./hybrid-globalization/calendar";
import { mono_wasm_install_js_worker_interop, mono_wasm_uninstall_js_worker_interop } from "./pthreads/shared";

import {
    mono_wasm_invoke_js_blazor, mono_wasm_invoke_js_with_args_ref, mono_wasm_get_object_property_ref, mono_wasm_set_object_property_ref,
    mono_wasm_get_by_index_ref, mono_wasm_set_by_index_ref, mono_wasm_get_global_object_ref
} from "./net6-legacy/method-calls";
import { mono_wasm_create_cs_owned_object_ref } from "./net6-legacy/cs-to-js";
import { mono_wasm_typed_array_to_array_ref } from "./net6-legacy/js-to-cs";
import { mono_wasm_typed_array_from_ref } from "./net6-legacy/buffers";
import { mono_wasm_get_culture_info } from "./hybrid-globalization/culture-info";
import { mono_wasm_get_first_day_of_week, mono_wasm_get_first_week_of_year } from "./hybrid-globalization/locales";

// the JS methods would be visible to EMCC linker and become imports of the WASM module

export const mono_wasm_threads_imports = !MonoWasmThreads ? [] : [
    // mono-threads-wasm.c
    mono_wasm_pthread_on_pthread_attached,
    mono_wasm_pthread_on_pthread_detached,
    // threads.c
    mono_wasm_eventloop_has_unsettled_interop_promises,
    // diagnostics_server.c
    mono_wasm_diagnostic_server_on_server_thread_created,
    mono_wasm_diagnostic_server_on_runtime_server_init,
    mono_wasm_diagnostic_server_stream_signal_work_available,

    // corebindings.c
    mono_wasm_install_js_worker_interop,
    mono_wasm_uninstall_js_worker_interop,
];

export const mono_wasm_legacy_interop_imports = !WasmEnableLegacyJsInterop ? [] : [
    // corebindings.c
    mono_wasm_invoke_js_with_args_ref,
    mono_wasm_get_object_property_ref,
    mono_wasm_set_object_property_ref,
    mono_wasm_get_by_index_ref,
    mono_wasm_set_by_index_ref,
    mono_wasm_get_global_object_ref,
    mono_wasm_create_cs_owned_object_ref,
    mono_wasm_typed_array_to_array_ref,
    mono_wasm_typed_array_from_ref,
    mono_wasm_invoke_js_blazor,
];

export const mono_wasm_imports = [
    // mini-wasm.c
    mono_wasm_schedule_timer,

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
    mono_wasm_trace_logger,
    mono_wasm_set_entrypoint_breakpoint,
    mono_wasm_event_pipe_early_startup_callback,

    // corebindings.c
    mono_wasm_release_cs_owned_object,
    mono_wasm_bind_js_function,
    mono_wasm_invoke_bound_function,
    mono_wasm_invoke_import,
    mono_wasm_bind_cs_function,
    mono_wasm_marshal_promise,
    mono_wasm_change_case_invariant,
    mono_wasm_change_case,
    mono_wasm_compare_string,
    mono_wasm_starts_with,
    mono_wasm_ends_with,
    mono_wasm_index_of,
    mono_wasm_get_calendar_info,
    mono_wasm_get_culture_info,
    mono_wasm_get_first_day_of_week,
    mono_wasm_get_first_week_of_year,
];

const wasmImports: Function[] = [
    ...mono_wasm_imports,
    // threading exports, if threading is enabled
    ...mono_wasm_threads_imports,
    // legacy interop exports, if enabled
    ...mono_wasm_legacy_interop_imports
];

export function replace_linker_placeholders(imports: WebAssembly.Imports) {
    // the output from emcc contains wrappers for these linker imports which add overhead,
    //  but now we have what we need to replace them with the actual functions
    // By default the imports all live inside of 'env', but emscripten minification could rename it to 'a'.
    // See https://github.com/emscripten-core/emscripten/blob/c5d1a856592b788619be11bbdc1dd119dec4e24c/src/preamble.js#L933-L936
    const env = imports.env || imports.a;
    if (!env) {
        mono_log_warn("WARNING: Neither imports.env or imports.a were present when instantiating the wasm module. This likely indicates an emscripten configuration issue.");
        return;
    }

    // the import names could be minified by applyImportAndExportNameChanges in emcc
    // we call each stub function to get the runtime_idx, which is the index into the wasmImports array
    const indexToNameMap: string[] = new Array(wasmImports.length);
    for (const shortName in env) {
        const stub_fn = env[shortName] as Function;
        if (typeof stub_fn === "function" && stub_fn.toString().indexOf("runtime_idx") !== -1) {
            try {
                const { runtime_idx } = stub_fn();
                if (indexToNameMap[runtime_idx] !== undefined) throw new Error(`Duplicate runtime_idx ${runtime_idx}`);
                indexToNameMap[runtime_idx] = shortName;
            } catch {
                // no-action
            }
        }
    }

    for (const [idx, realFn] of wasmImports.entries()) {
        const shortName = indexToNameMap[idx];
        // if it's not found it means the emcc linker didn't include it, which is fine
        if (shortName !== undefined) {
            const stubFn = env[shortName];
            if (typeof stubFn !== "function") throw new Error(`Expected ${shortName} to be a function`);
            env[shortName] = realFn;
            mono_log_debug(`Replaced WASM import ${shortName} stub ${stubFn.name} with ${realFn.name || "minified implementation"}`);
        }
    }

}