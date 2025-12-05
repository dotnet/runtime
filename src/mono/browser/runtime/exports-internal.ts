// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { MonoObjectNull, type MonoObject } from "./types/internal";
import cwraps, { profiler_c_functions, threads_c_functions as twraps } from "./cwraps";
import { mono_wasm_send_dbg_command_with_parms, mono_wasm_send_dbg_command, mono_wasm_get_dbg_command_info, mono_wasm_get_details, mono_wasm_release_object, mono_wasm_call_function_on, mono_wasm_debugger_resume, mono_wasm_detach_debugger, mono_wasm_raise_debug_event, mono_wasm_change_debugger_log_level, mono_wasm_debugger_attached } from "./debug";
import { http_wasm_supports_streaming_request, http_wasm_supports_streaming_response, http_wasm_create_controller, http_wasm_abort, http_wasm_transform_stream_write, http_wasm_transform_stream_close, http_wasm_fetch, http_wasm_fetch_stream, http_wasm_fetch_bytes, http_wasm_get_response_header_names, http_wasm_get_response_header_values, http_wasm_get_response_bytes, http_wasm_get_response_length, http_wasm_get_streamed_response_bytes, http_wasm_get_response_type, http_wasm_get_response_status } from "./http";
import { exportedRuntimeAPI, Module, runtimeHelpers } from "./globals";
import { get_property, set_property, has_property, get_typeof_property, get_global_this, dynamic_import } from "./invoke-js";
import { mono_wasm_stringify_as_error_with_stack } from "./logging";
import { ws_wasm_create, ws_wasm_open, ws_wasm_send, ws_wasm_receive, ws_wasm_close, ws_wasm_abort, ws_get_state } from "./web-socket";
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

import { mono_wasm_dump_threads } from "./pthreads";

export function export_internal (): any {
    return {
        // tests
        monoWasmExit: (exit_code: number) => {
            Module.err("early exit " + exit_code);
        },
        forceDisposeProxies,
        monoWasmDumpThreads: WasmEnableThreads ? mono_wasm_dump_threads : undefined,

        // with mono_wasm_debugger_log and mono_wasm_trace_logger
        logging: undefined,

        monoWasmStringifyAsErrorWithStack: mono_wasm_stringify_as_error_with_stack,

        // used in debugger DevToolsHelper.cs
        monoWasmGetLoadedFiles: mono_wasm_get_loaded_files,
        monoWasmSendDbgCommandWithParms: mono_wasm_send_dbg_command_with_parms,
        monoWasmSendDbgCommand: mono_wasm_send_dbg_command,
        monoWasmGetDbgCommandInfo: mono_wasm_get_dbg_command_info,
        monoWasmGetDetails: mono_wasm_get_details,
        monoWasmReleaseObject: mono_wasm_release_object,
        monoWasmCallFunctionOn: mono_wasm_call_function_on,
        monoWasmDebuggerResume: mono_wasm_debugger_resume,
        monoWasmDetachDebugger: mono_wasm_detach_debugger,
        monoWasmRaiseDebugEvent: mono_wasm_raise_debug_event,
        monoWasmChangeDebuggerLogLevel: mono_wasm_change_debugger_log_level,
        monoWasmDebuggerAttached: mono_wasm_debugger_attached,
        monoWasmRuntimeIsReady: runtimeHelpers.mono_wasm_runtime_is_ready,
        monoWasmGetFuncIdToNameMappings: mono_wasm_get_func_id_to_name_mappings,

        // interop
        getProperty: get_property,
        setProperty: set_property,
        hasProperty: has_property,
        getTypeofProperty: get_typeof_property,
        getGlobalThis: get_global_this,
        getDotnetInstance: () => exportedRuntimeAPI,
        dynamicImport: dynamic_import,
        monoWasmBindCsFunction: mono_wasm_bind_cs_function,

        // BrowserWebSocket
        wsWasmCreate: ws_wasm_create,
        wsWasmOpen: ws_wasm_open,
        wsWasmSend: ws_wasm_send,
        wsWasmReceive: ws_wasm_receive,
        wsWasmClose: ws_wasm_close,
        wsWasmAbort: ws_wasm_abort,
        wsGetState: ws_get_state,

        // BrowserHttpHandler
        httpWasmSupportsStreamingRequest: http_wasm_supports_streaming_request,
        httpWasmSupportsStreamingResponse: http_wasm_supports_streaming_response,
        httpWasmCreateController: http_wasm_create_controller,
        httpWasmGetResponseType: http_wasm_get_response_type,
        httpWasmGetResponseStatus: http_wasm_get_response_status,
        httpWasmAbort: http_wasm_abort,
        httpWasmTransformStreamWrite: http_wasm_transform_stream_write,
        httpWasmTransformStreamClose: http_wasm_transform_stream_close,
        httpWasmFetch: http_wasm_fetch,
        httpWasmFetchStream: http_wasm_fetch_stream,
        httpWasmFetchBytes: http_wasm_fetch_bytes,
        httpWasmGetResponseHeaderNames: http_wasm_get_response_header_names,
        httpWasmGetResponseHeaderValues: http_wasm_get_response_header_values,
        httpWasmGetResponseBytes: http_wasm_get_response_bytes,
        httpWasmGetResponseLength: http_wasm_get_response_length,
        httpWasmGetStreamedResponseBytes: http_wasm_get_streamed_response_bytes,

        // jiterpreter
        jiterpreterDumpStats: jiterpreter_dump_stats,
        jiterpreter_apply_options: applyOptions,
        jiterpreter_get_options: getOptions,

        // interpreter pgo
        interpPgoLoadData: interp_pgo_load_data,
        interpPgoSaveData: interp_pgo_save_data,

        // Blazor GC Lock support
        monoWasmGcLock: mono_wasm_gc_lock,
        monoWasmGcUnlock: mono_wasm_gc_unlock,

        // Blazor legacy replacement
        monoObjectAsBoolOrNullUnsafe,
        monoStringToStringUnsafe,

        loadLazyAssembly,
        loadSatelliteAssemblies
    };
}

export function cwraps_internal (internal: any): void {
    Object.assign(internal, {
        monoWasmExit: cwraps.mono_wasm_exit,
        mono_wasm_profiler_init_aot: profiler_c_functions.mono_wasm_profiler_init_aot,
        mono_wasm_profiler_init_browser_devtools: profiler_c_functions.mono_wasm_profiler_init_browser_devtools,
        mono_wasm_exec_regression: cwraps.mono_wasm_exec_regression,
        mono_wasm_print_thread_dump: WasmEnableThreads ? twraps.mono_wasm_print_thread_dump : undefined,
    });
}

/* @deprecated not GC safe, legacy support for Blazor */
export function monoObjectAsBoolOrNullUnsafe (obj: MonoObject): boolean | null {
    // TODO https://github.com/dotnet/runtime/issues/100411
    // after Blazor stops using monoObjectAsBoolOrNullUnsafe

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
