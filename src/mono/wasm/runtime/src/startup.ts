// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    mono_wasm_new_root, mono_wasm_new_roots, mono_wasm_release_roots,
    mono_wasm_new_root_buffer, mono_wasm_new_root_buffer_from_pointer
} from './mono/roots'
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
} from './mono/debug'
import { StringDecoder } from './mono/string-decoder'
import { MONO, setMONO } from './runtime'
import { t_MONO, t_MonoSupportLib } from './mono/types'
import {
    mono_load_runtime_and_bcl_args, mono_wasm_load_config,
    mono_wasm_setenv, mono_wasm_set_runtime_options,
    mono_wasm_load_data_archive, mono_wasm_asm_loaded, mono_wasm_load_bytes_into_heap
} from './mono/init'
import { prevent_timer_throttling, mono_set_timeout, schedule_background_exec } from './mono/scheduling'
import { mono_wasm_load_icu_data, mono_wasm_get_icudt_name } from './mono/icu'

export function export_functions(mono: t_MONO, module: t_Module & t_MonoSupportLib) {
    setMONO(mono, module)
    MONO.string_decoder = new StringDecoder();
    MONO.mono_wasm_fire_debugger_agent_message = module.mono_wasm_fire_debugger_agent_message = mono_wasm_fire_debugger_agent_message;
    MONO.mono_set_timeout = module.mono_set_timeout = mono_set_timeout;
    MONO.mono_wasm_asm_loaded = module.mono_wasm_asm_loaded = mono_wasm_asm_loaded;
    MONO.schedule_background_exec = module.schedule_background_exec = schedule_background_exec;

    MONO.mono_wasm_setenv = module.mono_wasm_setenv = mono_wasm_setenv;
    MONO.mono_wasm_new_root_buffer = module.mono_wasm_new_root_buffer = mono_wasm_new_root_buffer;
    MONO.mono_wasm_new_root_buffer_from_pointer = module.mono_wasm_new_root_buffer_from_pointer = mono_wasm_new_root_buffer_from_pointer;
    MONO.mono_wasm_new_root = module.mono_wasm_new_root = mono_wasm_new_root;
    MONO.mono_wasm_new_roots = module.mono_wasm_new_roots = mono_wasm_new_roots;
    MONO.mono_wasm_release_roots = module.mono_wasm_release_roots = mono_wasm_release_roots;
    MONO.mono_wasm_load_bytes_into_heap = module.mono_wasm_load_bytes_into_heap = mono_wasm_load_bytes_into_heap;
    MONO.mono_wasm_get_loaded_files = module.mono_wasm_get_loaded_files = mono_wasm_get_loaded_files;
    MONO.mono_wasm_load_icu_data = module.mono_wasm_load_icu_data = mono_wasm_load_icu_data;
    MONO.mono_wasm_get_icudt_name = module.mono_wasm_get_icudt_name = mono_wasm_get_icudt_name;

    MONO.mono_wasm_set_runtime_options = mono_wasm_set_runtime_options;
    MONO.mono_wasm_add_dbg_command_received = mono_wasm_add_dbg_command_received;
    MONO.mono_wasm_send_dbg_command_with_parms = mono_wasm_send_dbg_command_with_parms;
    MONO.mono_wasm_send_dbg_command = mono_wasm_send_dbg_command;
    MONO.mono_wasm_get_dbg_command_info = mono_wasm_get_dbg_command_info;
    MONO.mono_wasm_get_details = mono_wasm_get_details;
    MONO.mono_wasm_release_object = mono_wasm_release_object;
    MONO.mono_wasm_call_function_on = mono_wasm_call_function_on;
    MONO.mono_wasm_debugger_resume = mono_wasm_debugger_resume;
    MONO.mono_wasm_detach_debugger = mono_wasm_detach_debugger;
    MONO.mono_wasm_runtime_ready = mono_wasm_runtime_ready;
    MONO.mono_wasm_raise_debug_event = mono_wasm_raise_debug_event;
    MONO.mono_load_runtime_and_bcl_args = mono_load_runtime_and_bcl_args;
    MONO.prevent_timer_throttling = prevent_timer_throttling;
    MONO.mono_wasm_load_config = mono_wasm_load_config;
    MONO.mono_wasm_load_data_archive = mono_wasm_load_data_archive;
}
