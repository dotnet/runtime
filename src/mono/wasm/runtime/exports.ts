// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    mono_wasm_new_root, mono_wasm_new_roots, mono_wasm_release_roots,
    mono_wasm_new_root_buffer, mono_wasm_new_root_buffer_from_pointer
} from "./roots";
import {
    mono_wasm_add_dbg_command_received,
    mono_wasm_send_dbg_command_with_parms,
    mono_wasm_send_dbg_command,
    mono_wasm_get_dbg_command_info,
    mono_wasm_get_details,
    mono_wasm_release_object,
    mono_wasm_call_function_on,
    mono_wasm_debugger_resume,
    mono_wasm_detach_debugger,
    mono_wasm_runtime_ready,
    mono_wasm_get_loaded_files,
    mono_wasm_raise_debug_event,
    mono_wasm_fire_debugger_agent_message,
} from "./debug";
import { runtimeHelpers, setLegacyModules } from "./modules";
import { MonoConfig, MonoConfigError } from "./types";
import {
    mono_load_runtime_and_bcl_args, mono_wasm_load_config,
    mono_wasm_setenv, mono_wasm_set_runtime_options,
    mono_wasm_load_data_archive, mono_wasm_asm_loaded,
    mono_bindings_init,
    mono_wasm_invoke_js_blazor, mono_wasm_invoke_js_marshalled, mono_wasm_invoke_js_unmarshalled
} from "./startup";
import { mono_set_timeout, schedule_background_exec } from "./scheduling";
import { mono_wasm_load_icu_data, mono_wasm_get_icudt_name } from "./icu";
import { conv_string, js_string_to_mono_string, mono_intern_string, string_decoder } from "./strings";
import { js_to_mono_obj, js_typed_array_to_array, mono_wasm_typed_array_to_array } from "./js-to-cs";
import {
    mono_array_to_js_array, mono_wasm_create_cs_owned_object, unbox_mono_obj,
    _unbox_mono_obj_root_with_known_nonprimitive_type
} from "./cs-to-js";
import {
    call_static_method, mono_bind_assembly_entry_point,
    mono_bind_static_method, mono_call_assembly_entry_point,
    mono_method_get_call_signature, call_method, mono_method_resolve,
    mono_wasm_get_by_index, mono_wasm_get_global_object, mono_wasm_get_object_property,
    mono_wasm_invoke_js_with_args, mono_wasm_set_by_index, mono_wasm_set_object_property,
    _get_args_root_buffer_for_method_call, _get_buffer_for_method_call,
    _handle_exception_for_call, _teardown_after_call
} from "./method-calls";
import { mono_wasm_typed_array_copy_to, mono_wasm_typed_array_from, mono_wasm_typed_array_copy_from, mono_wasm_load_bytes_into_heap } from "./buffers";
import { mono_wasm_cancel_promise } from "./cancelable-promise";
import { mono_wasm_add_event_listener, mono_wasm_remove_event_listener } from "./event-listener";
import { mono_wasm_release_cs_owned_object } from "./gc-handles";
import { mono_bind_method } from "./method-binding";
import { mono_wasm_web_socket_open, mono_wasm_web_socket_send, mono_wasm_web_socket_receive, mono_wasm_web_socket_close, mono_wasm_web_socket_abort } from "./web-socket";
import cwraps from "./cwraps";

export const MONO: MONO = <any>{
    // current "public" MONO API
    mono_wasm_setenv,
    mono_wasm_load_bytes_into_heap,
    mono_wasm_load_icu_data,
    mono_wasm_runtime_ready,
    mono_wasm_load_data_archive,
    loaded_files: runtimeHelpers.loaded_files,

    // EM_JS macro
    string_decoder,

    // generated bindings closure `library_mono`
    mono_wasm_new_root_buffer,
    mono_wasm_new_root_buffer_from_pointer,
    mono_wasm_new_root,
    mono_wasm_new_roots,
    mono_wasm_release_roots,
    //mono.mono_wasm_get_icudt_name = mono_wasm_get_icudt_name,

    // used in debugger
    mono_wasm_get_loaded_files,
    mono_wasm_add_dbg_command_received,
    mono_wasm_send_dbg_command_with_parms,
    mono_wasm_send_dbg_command,
    mono_wasm_get_dbg_command_info,
    mono_wasm_get_details,
    mono_wasm_release_object,
    mono_wasm_call_function_on,
    mono_wasm_debugger_resume,
    mono_wasm_detach_debugger,
    mono_wasm_raise_debug_event,
    mono_wasm_runtime_is_ready: runtimeHelpers.mono_wasm_runtime_is_ready,

    // used in tests
    mono_load_runtime_and_bcl_args,
    mono_wasm_load_config,
    mono_wasm_set_runtime_options,
    config: runtimeHelpers.config,
};

export const BINDING: BINDING = <any>{
    //current "public" BINDING API
    mono_obj_array_new: cwraps.mono_wasm_obj_array_new,
    mono_obj_array_set: cwraps.mono_wasm_obj_array_set,
    js_string_to_mono_string,
    js_typed_array_to_array,
    js_to_mono_obj,
    mono_array_to_js_array,
    conv_string,
    bind_static_method: mono_bind_static_method,
    call_assembly_entry_point: mono_call_assembly_entry_point,
    unbox_mono_obj,

    // generated bindings closure `binding_support`
    _get_args_root_buffer_for_method_call,
    _get_buffer_for_method_call,
    invoke_method: cwraps.mono_wasm_invoke_method,
    _handle_exception_for_call,
    mono_wasm_try_unbox_primitive_and_get_type: cwraps.mono_wasm_try_unbox_primitive_and_get_type,
    _unbox_mono_obj_root_with_known_nonprimitive_type,
    _teardown_after_call,

    // tests
    call_static_method,
    mono_intern_string,

    // startup
    BINDING_ASM: "[System.Private.Runtime.InteropServices.JavaScript]System.Runtime.InteropServices.JavaScript.Runtime",
};

// this is executed early during load of emscripten runtime
// it exports methods to global objects MONO, BINDING and Module in backward compatible way
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function export_to_emscripten(mono: any, binding: any, dotnet: any, module: t_ModuleExtension): void {
    // we want to have same instance of MONO, BINDING and Module in dotnet iffe
    setLegacyModules(mono, binding, module);

    // here we merge methods to it from the local objects
    Object.assign(mono, MONO);
    Object.assign(binding, BINDING);
    Object.assign(module, _linker_exports);

    // here we merge objects used in tests
    Object.assign(module, {
        // config,
        call_static_method,
        mono_call_static_method: call_static_method
    });
}

// the methods would be visible to EMCC linker
// --- keep in sync with library-dotnet.js ---
export const _linker_exports = {
    //MonoSupportLib
    mono_set_timeout,
    mono_wasm_asm_loaded,
    mono_wasm_fire_debugger_agent_message,
    schedule_background_exec,
    mono_wasm_setenv,

    //BindingSupportLib
    mono_bindings_init,// TODO remove
    mono_bind_method,// TODO remove
    mono_method_invoke: call_method,// TODO remove, rename this
    mono_method_get_call_signature,// TODO remove
    mono_method_resolve,// tests
    mono_bind_static_method,// tests
    mono_bind_assembly_entry_point,// TODO remove
    mono_call_assembly_entry_point,// tests
    mono_intern_string,// TODO remove

    //DotNetSupportLib 
    mono_wasm_invoke_js_blazor,
    mono_wasm_invoke_js_marshalled,
    mono_wasm_invoke_js_unmarshalled,

    // also keep in sync with corebindings.c
    mono_wasm_invoke_js_with_args,
    mono_wasm_get_object_property,
    mono_wasm_set_object_property,
    mono_wasm_get_by_index,
    mono_wasm_set_by_index,
    mono_wasm_get_global_object,
    mono_wasm_create_cs_owned_object,
    mono_wasm_release_cs_owned_object,
    mono_wasm_typed_array_to_array,
    mono_wasm_typed_array_copy_to,
    mono_wasm_typed_array_from,
    mono_wasm_typed_array_copy_from,
    mono_wasm_add_event_listener,
    mono_wasm_remove_event_listener,
    mono_wasm_cancel_promise,
    mono_wasm_web_socket_open,
    mono_wasm_web_socket_send,
    mono_wasm_web_socket_receive,
    mono_wasm_web_socket_close,
    mono_wasm_web_socket_abort,

    //  also keep in sync with pal_icushim_static.c
    mono_wasm_load_icu_data,
    mono_wasm_get_icudt_name,
};

// this represents visibility in the javascript
// like https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoTypes.ts
export interface MONO {
    mono_wasm_runtime_ready: typeof mono_wasm_runtime_ready
    mono_wasm_setenv: typeof mono_wasm_setenv
    mono_wasm_load_data_archive: typeof mono_wasm_load_data_archive;
    mono_wasm_load_bytes_into_heap: typeof mono_wasm_load_bytes_into_heap;
    mono_wasm_load_icu_data: typeof mono_wasm_load_icu_data;
    loaded_files: string[];
}

// this represents visibility in the javascript
// like https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoTypes.ts
export interface BINDING {
    mono_obj_array_new: typeof cwraps.mono_wasm_obj_array_new,
    mono_obj_array_set: typeof cwraps.mono_wasm_obj_array_set,
    js_string_to_mono_string: typeof js_string_to_mono_string,
    js_typed_array_to_array: typeof js_typed_array_to_array,
    js_to_mono_obj: typeof js_to_mono_obj,
    mono_array_to_js_array: typeof mono_array_to_js_array,
    conv_string: typeof conv_string,
    bind_static_method: typeof mono_bind_static_method,
    call_assembly_entry_point: typeof mono_call_assembly_entry_point,
    unbox_mono_obj: typeof unbox_mono_obj
}

// how we extended wasm Module
export type t_ModuleExtension = t_Module & {
    config?: MonoConfig | MonoConfigError,

    //tests
    mono_call_static_method: typeof call_static_method
}
