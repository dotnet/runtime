// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { MonoObjectNull, type MonoObject } from "./types/internal";
import cwraps from "./cwraps";
import { mono_wasm_send_dbg_command_with_parms, mono_wasm_send_dbg_command, mono_wasm_get_dbg_command_info, mono_wasm_get_details, mono_wasm_release_object, mono_wasm_call_function_on, mono_wasm_debugger_resume, mono_wasm_detach_debugger, mono_wasm_raise_debug_event, mono_wasm_change_debugger_log_level, mono_wasm_debugger_attached } from "./debug";
import { http_wasm_supports_streaming_request, http_wasm_supports_streaming_response, http_wasm_create_controller, http_wasm_abort_request, http_wasm_abort_response, http_wasm_transform_stream_write, http_wasm_transform_stream_close, http_wasm_fetch, http_wasm_fetch_stream, http_wasm_fetch_bytes, http_wasm_get_response_header_names, http_wasm_get_response_header_values, http_wasm_get_response_bytes, http_wasm_get_response_length, http_wasm_get_streamed_response_bytes, http_wasm_get_response_type, http_wasm_get_response_status } from "./http";
import { exportedRuntimeAPI, Module, runtimeHelpers } from "./globals";
import { get_property, set_property, has_property, get_typeof_property, get_global_this, dynamic_import } from "./invoke-js";
import { mono_wasm_stringify_as_error_with_stack } from "./logging";
import { ws_wasm_create, ws_wasm_open, ws_wasm_send, ws_wasm_receive, ws_wasm_close, ws_wasm_abort } from "./web-socket";
import { mono_wasm_get_loaded_files } from "./assets";
import { jiterpreter_dump_stats } from "./jiterpreter";
import { interp_pgo_load_data, interp_pgo_save_data } from "./interp-pgo";
import { getOptions, applyOptions } from "./jiterpreter-support";
import { mono_wasm_gc_lock, mono_wasm_gc_unlock } from "./gc-lock";
import { loadLazyAssembly } from "./lazyLoading";
import { loadSatelliteAssemblies } from "./satelliteAssemblies";
import { forceDisposeProxies } from "./gc-handles";
import { mono_wasm_get_func_id_to_name_mappings } from "./logging";
import { monoStringToStringUnsafe } from "./strings";
import { mono_wasm_bind_cs_function } from "./invoke-cs";

import { dumpThreads, thread_available } from "./pthreads";

export function export_internal(): any {
    return {
        // tests
        mono_wasm_exit: (exit_code: number) => { Module.err("early exit " + exit_code); },
        forceDisposeProxies,
        dumpThreads: WasmEnableThreads ? dumpThreads : undefined,

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
        mono_wasm_get_func_id_to_name_mappings,

        // interop
        get_property,
        set_property,
        has_property,
        get_typeof_property,
        get_global_this,
        get_dotnet_instance: () => exportedRuntimeAPI,
        dynamic_import,
        thread_available: WasmEnableThreads ? thread_available : undefined,
        mono_wasm_bind_cs_function,

        // BrowserWebSocket
        ws_wasm_create,
        ws_wasm_open,
        ws_wasm_send,
        ws_wasm_receive,
        ws_wasm_close,
        ws_wasm_abort,

        // BrowserHttpHandler
        http_wasm_supports_streaming_request,
        http_wasm_supports_streaming_response,
        http_wasm_create_controller,
        http_wasm_get_response_type,
        http_wasm_get_response_status,
        http_wasm_abort_request,
        http_wasm_abort_response,
        http_wasm_transform_stream_write,
        http_wasm_transform_stream_close,
        http_wasm_fetch,
        http_wasm_fetch_stream,
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

        // interpreter pgo
        interp_pgo_load_data,
        interp_pgo_save_data,

        // Blazor GC Lock support
        mono_wasm_gc_lock,
        mono_wasm_gc_unlock,

        // Blazor legacy replacement
        monoObjectAsBoolOrNullUnsafe,
        monoStringToStringUnsafe,

        loadLazyAssembly,
        loadSatelliteAssemblies
    };
}

/* @deprecated not GC safe, legacy support for Blazor */
export function monoObjectAsBoolOrNullUnsafe(obj: MonoObject): boolean | null {
    if (obj === MonoObjectNull) {
        return null;
    }
    const res = cwraps.mono_wasm_read_as_bool_or_null_unsafe(obj);
    if (res === 0) {
        return false;
    }
    if (res === 1) {
        return true;
    }
    return null;
}
