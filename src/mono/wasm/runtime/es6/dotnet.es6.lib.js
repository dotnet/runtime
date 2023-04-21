// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* eslint-disable no-undef */

"use strict";

// USE_PTHREADS is emscripten's define symbol, which is passed to acorn optimizer, so we could use it here
#if USE_PTHREADS
const monoWasmThreads = true;
const isPThread = `ENVIRONMENT_IS_PTHREAD`;
#else
const monoWasmThreads = false;
const isPThread = "false";
#endif

// because we can't pass custom define symbols to acorn optimizer, we use environment variables to pass other build options
const DISABLE_LEGACY_JS_INTEROP = process.env.DISABLE_LEGACY_JS_INTEROP === "1";
const disableLegacyJsInterop = DISABLE_LEGACY_JS_INTEROP ? "true" : "false";

const DotnetSupportLib = {
    $DOTNET: {},
    // this line will be placed early on emscripten runtime creation, passing import and export objects into __dotnet_runtime IIFE
    // Emscripten uses require function for nodeJS even in ES6 module. We need https://nodejs.org/api/module.html#modulecreaterequirefilename
    // We use dynamic import because there is no "module" module in the browser.
    // This is async init of it, note it would become available only after first tick.
    // Also fix of scriptDirectory would be delayed
    // Emscripten's getBinaryPromise is not async for NodeJs, but we would like to have it async, so we replace it.
    // We also replace implementation of fetch
    $DOTNET__postset: `
let __dotnet_replacement_PThread = ${monoWasmThreads} ? {} : undefined;
${monoWasmThreads ? `
__dotnet_replacement_PThread.loadWasmModuleToWorker = PThread.loadWasmModuleToWorker;
__dotnet_replacement_PThread.threadInitTLS = PThread.threadInitTLS;
__dotnet_replacement_PThread.allocateUnusedWorker = PThread.allocateUnusedWorker;
` : ''}
let __dotnet_replacements = {scriptUrl: import.meta.url, fetch: globalThis.fetch, require, updateMemoryViews, pthreadReplacements: __dotnet_replacement_PThread};
if (ENVIRONMENT_IS_NODE) {
    __dotnet_replacements.requirePromise = __requirePromise;
}
let __dotnet_exportedAPI = __initializeImportsAndExports(
    { isGlobal:false, isNode:ENVIRONMENT_IS_NODE, isWorker:ENVIRONMENT_IS_WORKER, isShell:ENVIRONMENT_IS_SHELL, isWeb:ENVIRONMENT_IS_WEB, isPThread:${isPThread}, disableLegacyJsInterop:${disableLegacyJsInterop}, quit_, ExitStatus, requirePromise:__dotnet_replacements.requirePromise },
    { mono:MONO, binding:BINDING, internal:INTERNAL, module:Module, marshaled_imports: IMPORTS },
    __dotnet_replacements, __callbackAPI);
updateMemoryViews = __dotnet_replacements.updateMemoryViews;
fetch = __dotnet_replacements.fetch;
_scriptDir = __dirname = scriptDirectory = __dotnet_replacements.scriptDirectory;
if (ENVIRONMENT_IS_NODE) {
    __dotnet_replacements.requirePromise.then(someRequire => {
        require = someRequire;
    });
}
var noExitRuntime = __dotnet_replacements.noExitRuntime;
${monoWasmThreads ? `
PThread.loadWasmModuleToWorker = __dotnet_replacements.pthreadReplacements.loadWasmModuleToWorker;
PThread.threadInitTLS = __dotnet_replacements.pthreadReplacements.threadInitTLS;
PThread.allocateUnusedWorker = __dotnet_replacements.pthreadReplacements.allocateUnusedWorker;
` : ''}
`,
};

// the methods would be visible to EMCC linker
// --- keep in sync with exports.ts ---
let linked_functions = [
    // mini-wasm.c
    "mono_set_timeout",

    // mini-wasm-debugger.c
    "mono_wasm_asm_loaded",
    "mono_wasm_fire_debugger_agent_message_with_data",
    "mono_wasm_debugger_log",
    "mono_wasm_add_dbg_command_received",
    "mono_wasm_set_entrypoint_breakpoint",

    // mono-threads-wasm.c
    "schedule_background_exec",

    // interp.c
    "mono_wasm_profiler_enter",
    "mono_wasm_profiler_leave",

    // driver.c
    "mono_wasm_trace_logger",
    "mono_wasm_event_pipe_early_startup_callback",

    // jiterpreter.c / interp.c / transform.c
    "mono_interp_tier_prepare_jiterpreter",
    "mono_interp_record_interp_entry",
    "mono_interp_jit_wasm_entry_trampoline",
    "mono_interp_jit_wasm_jit_call_trampoline",
    "mono_interp_invoke_wasm_jit_call_trampoline",
    "mono_interp_flush_jitcall_queue",
    "mono_jiterp_do_jit_call_indirect",

    // corebindings.c
    "mono_wasm_release_cs_owned_object",
    "mono_wasm_bind_js_function",
    "mono_wasm_invoke_bound_function",
    "mono_wasm_invoke_import",
    "mono_wasm_bind_cs_function",
    "mono_wasm_marshal_promise",
    "mono_wasm_change_case_invariant",
    "mono_wasm_change_case",
    "mono_wasm_compare_string",
    "mono_wasm_starts_with",
    "mono_wasm_ends_with",

    "icudt68_dat",
];

if (monoWasmThreads) {
    linked_functions = [...linked_functions,
        /// mono-threads-wasm.c
        "mono_wasm_pthread_on_pthread_attached",
        // threads.c
        "mono_wasm_eventloop_has_unsettled_interop_promises",
        // diagnostics_server.c
        "mono_wasm_diagnostic_server_on_server_thread_created",
        "mono_wasm_diagnostic_server_on_runtime_server_init",
        "mono_wasm_diagnostic_server_stream_signal_work_available",
    ]
}
if (!DISABLE_LEGACY_JS_INTEROP) {
    linked_functions = [...linked_functions,
        "mono_wasm_invoke_js_with_args_ref",
        "mono_wasm_get_object_property_ref",
        "mono_wasm_set_object_property_ref",
        "mono_wasm_get_by_index_ref",
        "mono_wasm_set_by_index_ref",
        "mono_wasm_get_global_object_ref",
        "mono_wasm_create_cs_owned_object_ref",
        "mono_wasm_typed_array_to_array_ref",
        "mono_wasm_typed_array_from_ref",
        "mono_wasm_invoke_js_blazor",
    ]
}

// -- this javascript file is evaluated by emcc during compilation! --
// we generate simple proxy for each exported function so that emcc will include them in the final output
for (let linked_function of linked_functions) {
    DotnetSupportLib[linked_function] = new Function('throw new Error("unreachable");');
}

autoAddDeps(DotnetSupportLib, "$DOTNET");
mergeInto(LibraryManager.library, DotnetSupportLib);
