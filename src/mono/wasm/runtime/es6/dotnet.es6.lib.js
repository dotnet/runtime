// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* eslint-disable no-undef */

"use strict";

// USE_PTHREADS is emscripten's define symbol, which is passed to acorn optimizer, so we could use it here
#if USE_PTHREADS
const monoWasmThreads = true;
#else
const monoWasmThreads = false;
#endif

// because we can't pass custom define symbols to acorn optimizer, we use environment variables to pass other build options
const WasmEnableLegacyJsInterop = process.env.WasmEnableLegacyJsInterop !== "false";

function setup() {
    const initializeImportsAndExports = Module['initializeImportsAndExports'];
    const pthreadReplacements = {};
    const dotnet_replacements = {
        scriptUrl: import.meta.url,
        fetch: globalThis.fetch,
        require,
        updateMemoryViews,
        pthreadReplacements,
        scriptDirectory
    };
    #if USE_PTHREADS
    dotnet_replacements.loadWasmModuleToWorker = PThread.loadWasmModuleToWorker;
    dotnet_replacements.threadInitTLS = PThread.threadInitTLS;
    dotnet_replacements.allocateUnusedWorker = PThread.allocateUnusedWorker;
    #else
    const ENVIRONMENT_IS_PTHREAD = false;
    #endif
    if (ENVIRONMENT_IS_NODE) {
        dotnet_replacements.requirePromise = import(/* webpackIgnore: true */'module').then(mod => mod.createRequire(import.meta.url));
        __dotnet_replacements.requirePromise.then(someRequire => {
            require = someRequire;
        });
    }
    Module['noExitRuntime'] = ENVIRONMENT_IS_WEB;
    if (!Module['locateFile']) Module['locateFile'] = Module['__locateFile'] = (path) => dotnet_replacements.scriptDirectory + path;

    // call runtime
    initializeImportsAndExports(
        {
            isNode: ENVIRONMENT_IS_NODE, isWorker: ENVIRONMENT_IS_WORKER, isShell: ENVIRONMENT_IS_SHELL, isWeb: ENVIRONMENT_IS_WEB, isPThread: ENVIRONMENT_IS_PTHREAD,
            quit_, ExitStatus
        }, dotnet_replacements);

    updateMemoryViews = dotnet_replacements.updateMemoryViews;
    fetch = dotnet_replacements.fetch;
    _scriptDir = __dirname = scriptDirectory = dotnet_replacements.scriptDirectory;
    #if USE_PTHREADS
    PThread.loadWasmModuleToWorker = dotnet_replacements.loadWasmModuleToWorker;
    PThread.threadInitTLS = dotnet_replacements.threadInitTLS;
    PThread.allocateUnusedWorker = dotnet_replacements.allocateUnusedWorker;
    #endif
}

const postset = `
    DOTNET.setup();
`;

const DotnetSupportLib = {
    $DOTNET: { setup },
    $DOTNET__postset: postset
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

    "icudt68_dat",
];

if (monoWasmThreads) {
    linked_functions = [...linked_functions,
        /// mono-threads-wasm.c
        "mono_wasm_pthread_on_pthread_attached",
        // diagnostics_server.c
        "mono_wasm_diagnostic_server_on_server_thread_created",
        "mono_wasm_diagnostic_server_on_runtime_server_init",
        "mono_wasm_diagnostic_server_stream_signal_work_available",
    ]
}
if (WasmEnableLegacyJsInterop) {
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
