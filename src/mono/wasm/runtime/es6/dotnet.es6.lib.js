// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* eslint-disable no-undef */

"use strict";


// because we can't pass custom define symbols to acorn optimizer, we use environment variables to pass other build options
const DISABLE_LEGACY_JS_INTEROP = process.env.DISABLE_LEGACY_JS_INTEROP === "1";

function setup(disableLegacyJsInterop) {
    const pthreadReplacements = {};
    const dotnet_replacements = {
        fetch: globalThis.fetch,
        require,
        updateMemoryViews,
        pthreadReplacements,
        scriptDirectory,
        noExitRuntime
    };
    // USE_PTHREADS is emscripten's define symbol, which is passed to acorn optimizer, so we could use it here
    #if USE_PTHREADS
    pthreadReplacements.loadWasmModuleToWorker = PThread.loadWasmModuleToWorker;
    pthreadReplacements.threadInitTLS = PThread.threadInitTLS;
    pthreadReplacements.allocateUnusedWorker = PThread.allocateUnusedWorker;
    #else
    const ENVIRONMENT_IS_PTHREAD = false;
    #endif

    Module.__dotnet_runtime.passEmscriptenInternals({
        isPThread: ENVIRONMENT_IS_PTHREAD,
        disableLegacyJsInterop,
        quit_, ExitStatus
    });
    Module.__dotnet_runtime.initializeReplacements(dotnet_replacements);

    #if USE_PTHREADS
    if (ENVIRONMENT_IS_PTHREAD) {
        Module.config = {};
        Module.__dotnet_runtime.configureWorkerStartup(Module);
    } else {
        #endif
        Module.__dotnet_runtime.configureEmscriptenStartup(Module);
        #if USE_PTHREADS
    }
    #endif

    updateMemoryViews = dotnet_replacements.updateMemoryViews;
    noExitRuntime = dotnet_replacements.noExitRuntime;
    fetch = dotnet_replacements.fetch;
    require = dotnet_replacements.require;
    _scriptDir = __dirname = scriptDirectory = dotnet_replacements.scriptDirectory;
    #if USE_PTHREADS
    PThread.loadWasmModuleToWorker = pthreadReplacements.loadWasmModuleToWorker;
    PThread.threadInitTLS = pthreadReplacements.threadInitTLS;
    PThread.allocateUnusedWorker = pthreadReplacements.allocateUnusedWorker;
    #endif
}

const postset = `
    DOTNET.setup(${DISABLE_LEGACY_JS_INTEROP ? "true" : "false"});
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
    "mono_wasm_change_case_invariant",
    "mono_wasm_change_case",
    "mono_wasm_compare_string",
    "mono_wasm_starts_with",
    "mono_wasm_ends_with",
    "mono_wasm_index_of",

    "icudt68_dat",
];

#if USE_PTHREADS
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
#endif
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
