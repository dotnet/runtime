import { mono_wasm_cancel_promise } from "./cancelable-promise";
import cwraps from "./cwraps";
import { mono_wasm_symbolicate_string, mono_wasm_stringify_as_error_with_stack, mono_wasm_get_loaded_files, mono_wasm_send_dbg_command_with_parms, mono_wasm_send_dbg_command, mono_wasm_get_dbg_command_info, mono_wasm_get_details, mono_wasm_release_object, mono_wasm_call_function_on, mono_wasm_debugger_resume, mono_wasm_detach_debugger, mono_wasm_raise_debug_event, mono_wasm_change_debugger_log_level, mono_wasm_debugger_attached } from "./debug";
import { get_dotnet_instance } from "./exports";
import { http_wasm_supports_streaming_response, http_wasm_create_abort_controler, http_wasm_abort_request, http_wasm_abort_response, http_wasm_fetch, http_wasm_fetch_bytes, http_wasm_get_response_header_names, http_wasm_get_response_header_values, http_wasm_get_response_bytes, http_wasm_get_response_length, http_wasm_get_streamed_response_bytes } from "./http";
import { Module, runtimeHelpers } from "./imports";
import { get_property, set_property, has_property, get_typeof_property, get_global_this, dynamic_import } from "./invoke-js";
import { mono_method_resolve } from "./net6-legacy/method-binding";
import { mono_wasm_set_runtime_options } from "./startup";
import { mono_intern_string } from "./strings";
import { ws_wasm_create, ws_wasm_open, ws_wasm_send, ws_wasm_receive, ws_wasm_close, ws_wasm_abort } from "./web-socket";

export function export_internal(): any {
    return {
        // tests
        mono_wasm_exit: (exit_code: number) => { Module.printErr("MONO_WASM: early exit " + exit_code); },
        mono_wasm_enable_on_demand_gc: cwraps.mono_wasm_enable_on_demand_gc,
        mono_profiler_init_aot: cwraps.mono_profiler_init_aot,
        mono_wasm_set_runtime_options,
        mono_wasm_exec_regression: cwraps.mono_wasm_exec_regression,
        mono_method_resolve,//MarshalTests.cs
        mono_intern_string,// MarshalTests.cs

        // with mono_wasm_debugger_log and mono_wasm_trace_logger
        logging: undefined,

        //
        mono_wasm_symbolicate_string,
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
        get_dotnet_instance,
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
    };
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function cwraps_internal(internal: any): void {
    Object.assign(internal, {
        mono_wasm_exit: cwraps.mono_wasm_exit,
        mono_wasm_enable_on_demand_gc: cwraps.mono_wasm_enable_on_demand_gc,
        mono_profiler_init_aot: cwraps.mono_profiler_init_aot,
        mono_wasm_exec_regression: cwraps.mono_wasm_exec_regression,
    });
}
