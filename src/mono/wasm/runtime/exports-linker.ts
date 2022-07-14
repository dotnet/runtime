import MonoWasmThreads from "consts:monoWasmThreads";
import { dotnet_browser_can_use_subtle_crypto_impl, dotnet_browser_simple_digest_hash, dotnet_browser_sign, dotnet_browser_encrypt_decrypt, dotnet_browser_derive_bits } from "./crypto-worker";
import { mono_wasm_fire_debugger_agent_message, mono_wasm_debugger_log, mono_wasm_add_dbg_command_received, mono_wasm_trace_logger, mono_wasm_set_entrypoint_breakpoint } from "./debug";
import { mono_wasm_release_cs_owned_object } from "./gc-handles";
import { mono_wasm_load_icu_data, mono_wasm_get_icudt_name } from "./icu";
import { mono_wasm_bind_cs_function } from "./invoke-cs";
import { mono_wasm_bind_js_function, mono_wasm_invoke_bound_function } from "./invoke-js";
import { mono_wasm_typed_array_from_ref } from "./legacy/buffers";
import { mono_wasm_create_cs_owned_object_ref } from "./legacy/cs-to-js";
import { mono_wasm_typed_array_to_array_ref } from "./legacy/js-to-cs";
import { mono_wasm_invoke_js_blazor, mono_wasm_invoke_js_with_args_ref, mono_wasm_get_object_property_ref, mono_wasm_set_object_property_ref, mono_wasm_get_by_index_ref, mono_wasm_set_by_index_ref, mono_wasm_get_global_object_ref } from "./legacy/method-calls";
import { mono_wasm_marshal_promise } from "./marshal-to-js";
import { mono_wasm_pthread_on_pthread_attached } from "./pthreads/worker";
import { mono_set_timeout, schedule_background_exec } from "./scheduling";
import { mono_wasm_asm_loaded } from "./startup";

// the methods would be visible to EMCC linker
// --- keep in sync with dotnet.cjs.lib.js ---
const mono_wasm_threads_exports = !MonoWasmThreads ? undefined : {
    // mono-threads-wasm.c
    mono_wasm_pthread_on_pthread_attached,
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
        mono_wasm_fire_debugger_agent_message,
        mono_wasm_debugger_log,
        mono_wasm_add_dbg_command_received,

        // mono-threads-wasm.c
        schedule_background_exec,

        // also keep in sync with driver.c
        mono_wasm_invoke_js_blazor,
        mono_wasm_trace_logger,
        mono_wasm_set_entrypoint_breakpoint,

        // also keep in sync with corebindings.c
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
        mono_wasm_bind_cs_function,
        mono_wasm_marshal_promise,

        //  also keep in sync with pal_icushim_static.c
        mono_wasm_load_icu_data,
        mono_wasm_get_icudt_name,

        // pal_crypto_webworker.c
        dotnet_browser_can_use_subtle_crypto_impl,
        dotnet_browser_simple_digest_hash,
        dotnet_browser_sign,
        dotnet_browser_encrypt_decrypt,
        dotnet_browser_derive_bits,

        // threading exports, if threading is enabled
        ...mono_wasm_threads_exports,
    };
}