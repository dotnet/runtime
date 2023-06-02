// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_cancel_promise } from "./cancelable-promise";
import cwraps from "./cwraps";
import { mono_wasm_send_dbg_command_with_parms, mono_wasm_send_dbg_command, mono_wasm_get_dbg_command_info, mono_wasm_get_details, mono_wasm_release_object, mono_wasm_call_function_on, mono_wasm_debugger_resume, mono_wasm_detach_debugger, mono_wasm_raise_debug_event, mono_wasm_change_debugger_log_level, mono_wasm_debugger_attached } from "./debug";
import { http_wasm_supports_streaming_response, http_wasm_create_abort_controler, http_wasm_abort_request, http_wasm_abort_response, http_wasm_fetch, http_wasm_fetch_bytes, http_wasm_get_response_header_names, http_wasm_get_response_header_values, http_wasm_get_response_bytes, http_wasm_get_response_length, http_wasm_get_streamed_response_bytes } from "./http";
import { exportedRuntimeAPI, Module, runtimeHelpers } from "./globals";
import { get_property, set_property, has_property, get_typeof_property, get_global_this, dynamic_import } from "./invoke-js";
import { mono_wasm_stringify_as_error_with_stack } from "./logging";
import { ws_wasm_create, ws_wasm_open, ws_wasm_send, ws_wasm_receive, ws_wasm_close, ws_wasm_abort } from "./web-socket";
import { mono_wasm_get_loaded_files } from "./assets";
import { jiterpreter_dump_stats } from "./jiterpreter";
import { getOptions, applyOptions } from "./jiterpreter-support";
import { mono_wasm_gc_lock, mono_wasm_gc_unlock } from "./gc-lock";

export function export_internal(): any {
    return {
        // tests
        mono_wasm_exit: (exit_code: number) => { Module.err("early exit " + exit_code); },
        mono_wasm_enable_on_demand_gc: cwraps.mono_wasm_enable_on_demand_gc,
        mono_wasm_profiler_init_aot: cwraps.mono_wasm_profiler_init_aot,
        mono_wasm_profiler_init_browser: cwraps.mono_wasm_profiler_init_browser,
        mono_wasm_exec_regression: cwraps.mono_wasm_exec_regression,

        // with mono_wasm_debugger_log and mono_wasm_trace_logger
        logging: undefined,

        mono_wasm_stringify_as_error_with_stack,

        // used in debugger DevToolsHelper.cs
        mono_wasm_get_loaded_files,
        mono_wasm_send_dbg_command_with_parms,
        mono_wasm_send_dbg_command,
        mono_wasm_get_dbg_command_info,
        mono_wasm_get_details,
        mono_wasm_release_object,
        mono_wasm_call_function_on,
        mono_wasm_debugger_resume,
        mono_wasm_detach_debugger,
        mono_wasm_raise_debug_event,
        mono_wasm_change_debugger_log_level,
        mono_wasm_debugger_attached,
        mono_wasm_runtime_is_ready: runtimeHelpers.mono_wasm_runtime_is_ready,

        // interop
        get_property,
        set_property,
        has_property,
        get_typeof_property,
        get_global_this,
        get_dotnet_instance: () => exportedRuntimeAPI,
        dynamic_import,

        // BrowserWebSocket
        mono_wasm_cancel_promise,
        ws_wasm_create,
        ws_wasm_open,
        ws_wasm_send,
        ws_wasm_receive,
        ws_wasm_close,
        ws_wasm_abort,

        // BrowserHttpHandler
        http_wasm_supports_streaming_response,
        http_wasm_create_abort_controler,
        http_wasm_abort_request,
        http_wasm_abort_response,
        http_wasm_fetch,
        http_wasm_fetch_bytes,
        http_wasm_get_response_header_names,
        http_wasm_get_response_header_values,
        http_wasm_get_response_bytes,
        http_wasm_get_response_length,
        http_wasm_get_streamed_response_bytes,

        // jiterpreter
        jiterpreter_dump_stats,
        jiterpreter_apply_options: applyOptions,
        jiterpreter_get_options: getOptions,

        // Blazor GC Lock support
        mono_wasm_gc_lock,
        mono_wasm_gc_unlock,
    };
}

export function cwraps_internal(internal: any): void {
    Object.assign(internal, {
        mono_wasm_exit: cwraps.mono_wasm_exit,
        mono_wasm_enable_on_demand_gc: cwraps.mono_wasm_enable_on_demand_gc,
        mono_wasm_profiler_init_aot: cwraps.mono_wasm_profiler_init_aot,
        mono_wasm_profiler_init_browser: cwraps.mono_wasm_profiler_init_browser,
        mono_wasm_exec_regression: cwraps.mono_wasm_exec_regression,
    });
}
