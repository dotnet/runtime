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
import { MonoArray, MonoConfig, MonoConfigError, MonoObject } from "./types";
import {
    mono_load_runtime_and_bcl_args, mono_wasm_load_config,
    mono_wasm_setenv, mono_wasm_set_runtime_options,
    mono_wasm_load_data_archive, mono_wasm_asm_loaded,
    mono_wasm_set_main_args
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
    call_static_method, mono_bind_static_method, mono_call_assembly_entry_point,
    mono_method_resolve,
    mono_wasm_compile_function,
    mono_wasm_get_by_index, mono_wasm_get_global_object, mono_wasm_get_object_property,
    mono_wasm_invoke_js,
    mono_wasm_invoke_js_blazor,
    mono_wasm_invoke_js_with_args, mono_wasm_set_by_index, mono_wasm_set_object_property,
    _get_args_root_buffer_for_method_call, _get_buffer_for_method_call,
    _handle_exception_for_call, _teardown_after_call
} from "./method-calls";
import { mono_wasm_typed_array_copy_to, mono_wasm_typed_array_from, mono_wasm_typed_array_copy_from, mono_wasm_load_bytes_into_heap } from "./buffers";
import { mono_wasm_cancel_promise } from "./cancelable-promise";
import { mono_wasm_add_event_listener, mono_wasm_remove_event_listener } from "./event-listener";
import { mono_wasm_release_cs_owned_object } from "./gc-handles";
import { mono_wasm_web_socket_open, mono_wasm_web_socket_send, mono_wasm_web_socket_receive, mono_wasm_web_socket_close, mono_wasm_web_socket_abort } from "./web-socket";
import cwraps from "./cwraps";
import { ArgsMarshalString } from "./method-binding";

export const MONO: MONO = <any>{
    // current "public" MONO API
    mono_wasm_setenv,
    mono_wasm_load_bytes_into_heap,
    mono_wasm_load_icu_data,
    mono_wasm_runtime_ready,
    mono_wasm_load_data_archive,
    mono_wasm_load_config,
    mono_load_runtime_and_bcl_args,
    mono_wasm_new_root_buffer,
    mono_wasm_new_root,
    mono_wasm_release_roots,

    // for Blazor's future!
    mono_wasm_add_assembly: cwraps.mono_wasm_add_assembly,
    mono_wasm_load_runtime: cwraps.mono_wasm_load_runtime,

    config: runtimeHelpers.config,
    loaded_files: [],

    // generated bindings closure `library_mono`
    mono_wasm_new_root_buffer_from_pointer,
    mono_wasm_new_roots,
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
    // todo use the methods directly in the closure, not via BINDING
    _get_args_root_buffer_for_method_call,
    _get_buffer_for_method_call,
    invoke_method: cwraps.mono_wasm_invoke_method,
    _handle_exception_for_call,
    mono_wasm_try_unbox_primitive_and_get_type: cwraps.mono_wasm_try_unbox_primitive_and_get_type,
    _unbox_mono_obj_root_with_known_nonprimitive_type,
    _teardown_after_call,
};

// this is executed early during load of emscripten runtime
// it exports methods to global objects MONO, BINDING and Module in backward compatible way
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
function export_to_emscripten(dotnet: any, mono: any, binding: any, internal: any, module: any): void {
    // we want to have same instance of MONO, BINDING and Module in dotnet iffe
    setLegacyModules(dotnet, mono, binding, internal, module);

    // here we merge methods to it from the local objects
    Object.assign(dotnet, DOTNET);
    Object.assign(mono, MONO);
    Object.assign(binding, BINDING);
    Object.assign(internal, INTERNAL);

    // backward compatibility, sync with EmscriptenModuleMono
    Object.assign(module, {
        // https://github.com/search?q=mono_bind_static_method&type=Code
        mono_bind_static_method: (fqn: string, signature: ArgsMarshalString): Function => {
            console.warn("Module.mono_bind_static_method is obsolete, please use BINDING.bind_static_method instead");
            return mono_bind_static_method(fqn, signature);
        },
    });

    // here we expose objects used in tests to global namespace
    if (!module.no_global_exports) {
        (<any>globalThis).Module = module;
        const warnWrap = (name: string, value: any) => {
            if (typeof ((<any>globalThis)[name]) !== "undefined") {
                // it already exists in the global namespace
                return;
            }
            let warnOnce = true;
            Object.defineProperty(globalThis, name, {
                get: () => {
                    if (warnOnce) {
                        const stack = (new Error()).stack;
                        const nextLine = stack ? stack.substr(stack.indexOf("\n", 8) + 1) : "";
                        console.warn(`global ${name} is obsolete, please use Module.${name} instead ${nextLine}`);
                        warnOnce = false;
                    }
                    return value;
                }
            });
        };
        warnWrap("MONO", mono);
        warnWrap("BINDING", binding);

        // Blazor back compat
        warnWrap("cwrap", Module.cwrap);
        warnWrap("addRunDependency", Module.addRunDependency);
        warnWrap("removeRunDependency", Module.removeRunDependency);
    }
}

// the methods would be visible to EMCC linker
// --- keep in sync with library-dotnet.js ---
const linker_exports = {
    // mini-wasm.c
    mono_set_timeout,

    // mini-wasm-debugger.c
    mono_wasm_asm_loaded,
    mono_wasm_fire_debugger_agent_message,

    // mono-threads-wasm.c
    schedule_background_exec,

    // also keep in sync with driver.c
    mono_wasm_invoke_js,
    mono_wasm_invoke_js_blazor,

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
    mono_wasm_compile_function,

    //  also keep in sync with pal_icushim_static.c
    mono_wasm_load_icu_data,
    mono_wasm_get_icudt_name,
};
export const DOTNET: any = {
};

export const INTERNAL: any = {
    // startup
    BINDING_ASM: "[System.Private.Runtime.InteropServices.JavaScript]System.Runtime.InteropServices.JavaScript.Runtime",
    export_to_emscripten,

    // linker
    linker_exports: linker_exports,

    // tests
    call_static_method,
    mono_wasm_exit: cwraps.mono_wasm_exit,
    mono_wasm_enable_on_demand_gc: cwraps.mono_wasm_enable_on_demand_gc,
    mono_profiler_init_aot: cwraps.mono_profiler_init_aot,
    mono_wasm_set_runtime_options,
    mono_wasm_set_main_args: mono_wasm_set_main_args,
    mono_wasm_strdup: cwraps.mono_wasm_strdup,
    mono_wasm_exec_regression: cwraps.mono_wasm_exec_regression,
    mono_method_resolve,//MarshalTests.cs
    mono_bind_static_method,// MarshalTests.cs
    mono_intern_string,// MarshalTests.cs

    // EM_JS,EM_ASM,EM_ASM_INT macros
    string_decoder,
    logging: undefined,

    // used in EM_ASM macros in debugger
    mono_wasm_add_dbg_command_received,

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
    mono_wasm_runtime_is_ready: runtimeHelpers.mono_wasm_runtime_is_ready,
};

// this represents visibility in the javascript
// like https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoTypes.ts
export interface MONO {
    mono_wasm_runtime_ready: typeof mono_wasm_runtime_ready
    mono_wasm_setenv: typeof mono_wasm_setenv
    mono_wasm_load_data_archive: typeof mono_wasm_load_data_archive;
    mono_wasm_load_bytes_into_heap: typeof mono_wasm_load_bytes_into_heap;
    mono_wasm_load_icu_data: typeof mono_wasm_load_icu_data;
    mono_wasm_load_config: typeof mono_wasm_load_config;
    mono_load_runtime_and_bcl_args: typeof mono_load_runtime_and_bcl_args;
    mono_wasm_new_root_buffer: typeof mono_wasm_new_root_buffer;
    mono_wasm_new_root: typeof mono_wasm_new_root;
    mono_wasm_release_roots: typeof mono_wasm_release_roots;

    // for Blazor's future!
    mono_wasm_add_assembly: typeof cwraps.mono_wasm_add_assembly,
    mono_wasm_load_runtime: typeof cwraps.mono_wasm_load_runtime,

    loaded_files: string[];
    config: MonoConfig | MonoConfigError,
}

// this represents visibility in the javascript
// like https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoTypes.ts
export interface BINDING {
    mono_obj_array_new: (size: number) => MonoArray,
    mono_obj_array_set: (array: MonoArray, idx: number, obj: MonoObject) => void,
    js_string_to_mono_string: typeof js_string_to_mono_string,
    js_typed_array_to_array: typeof js_typed_array_to_array,
    js_to_mono_obj: typeof js_to_mono_obj,
    mono_array_to_js_array: typeof mono_array_to_js_array,
    conv_string: typeof conv_string,
    bind_static_method: typeof mono_bind_static_method,
    call_assembly_entry_point: typeof mono_call_assembly_entry_point,
    unbox_mono_obj: typeof unbox_mono_obj
}