// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* eslint-disable no-undef */

"use strict";

#if USE_PTHREADS
const usePThreads = `true`;
const isPThread = `ENVIRONMENT_IS_PTHREAD`;
#else
const usePThreads = `false`;
const isPThread = `false`;
#endif

const DotnetSupportLib = {
    $DOTNET: {},
    // these lines will be placed early on emscripten runtime creation, passing import and export objects into __dotnet_runtime IFFE
    // we replace implementation of readAsync and fetch
    // replacement of require is there for consistency with ES6 code
    $DOTNET__postset: `
let __dotnet_replacement_PThread = ${usePThreads} ? {} : undefined;
if (${usePThreads}) {
    __dotnet_replacement_PThread.loadWasmModuleToWorker = PThread.loadWasmModuleToWorker;
    __dotnet_replacement_PThread.threadInitTLS = PThread.threadInitTLS;
}
let __dotnet_replacements = {readAsync, fetch: globalThis.fetch, require, updateGlobalBufferAndViews, pthreadReplacements: __dotnet_replacement_PThread};
let __dotnet_exportedAPI = __dotnet_runtime.__initializeImportsAndExports(
    { isESM:false, isGlobal:ENVIRONMENT_IS_GLOBAL, isNode:ENVIRONMENT_IS_NODE, isWorker:ENVIRONMENT_IS_WORKER, isShell:ENVIRONMENT_IS_SHELL, isWeb:ENVIRONMENT_IS_WEB, isPThread:${isPThread}, locateFile, quit_, ExitStatus, requirePromise:Promise.resolve(require)},
    { mono:MONO, binding:BINDING, internal:INTERNAL, module:Module, marshaled_exports: EXPORTS, marshaled_imports: IMPORTS  },
    __dotnet_replacements);
updateGlobalBufferAndViews = __dotnet_replacements.updateGlobalBufferAndViews;
readAsync = __dotnet_replacements.readAsync;
var fetch = __dotnet_replacements.fetch;
require = __dotnet_replacements.requireOut;
var noExitRuntime = __dotnet_replacements.noExitRuntime;
if (${usePThreads}) {
    PThread.loadWasmModuleToWorker = __dotnet_replacements.pthreadReplacements.loadWasmModuleToWorker;
    PThread.threadInitTLS = __dotnet_replacements.pthreadReplacements.threadInitTS;
}
`,
};

// the methods would be visible to EMCC linker
// --- keep in sync with exports.ts ---
const linked_functions = [
    // mini-wasm.c
    "mono_set_timeout",

    // mini-wasm-debugger.c
    "mono_wasm_asm_loaded",
    "mono_wasm_fire_debugger_agent_message",
    "mono_wasm_debugger_log",
    "mono_wasm_add_dbg_command_received",

    // mono-threads-wasm.c
    "schedule_background_exec",

    // driver.c
    "mono_wasm_invoke_js_blazor",
    "mono_wasm_trace_logger",
    "mono_wasm_set_entrypoint_breakpoint",

    // corebindings.c
    "mono_wasm_invoke_js_with_args_ref",
    "mono_wasm_get_object_property_ref",
    "mono_wasm_set_object_property_ref",
    "mono_wasm_get_by_index_ref",
    "mono_wasm_set_by_index_ref",
    "mono_wasm_get_global_object_ref",
    "mono_wasm_create_cs_owned_object_ref",
    "mono_wasm_release_cs_owned_object",
    "mono_wasm_typed_array_to_array_ref",
    "mono_wasm_typed_array_copy_to_ref",
    "mono_wasm_typed_array_from_ref",
    "mono_wasm_typed_array_copy_from_ref",
    "mono_wasm_compile_function_ref",
    "mono_wasm_bind_js_function",
    "mono_wasm_invoke_bound_function",
    "mono_wasm_bind_cs_function",
    "mono_wasm_marshal_promise",

    // pal_icushim_static.c
    "mono_wasm_load_icu_data",
    "mono_wasm_get_icudt_name",

    // pal_crypto_webworker.c
    "dotnet_browser_can_use_subtle_crypto_impl",
    "dotnet_browser_simple_digest_hash",
    "dotnet_browser_sign",
    "dotnet_browser_encrypt_decrypt",
    "dotnet_browser_derive_bits",

    /// mono-threads-wasm.c
    #if USE_PTHREADS
    "mono_wasm_pthread_on_pthread_attached",
    #endif
];

// -- this javascript file is evaluated by emcc during compilation! --
// we generate simple proxy for each exported function so that emcc will include them in the final output
for (let linked_function of linked_functions) {
    const fn_template = `return __dotnet_runtime.__linker_exports.${linked_function}.apply(__dotnet_runtime, arguments)`;
    DotnetSupportLib[linked_function] = new Function(fn_template);
}

autoAddDeps(DotnetSupportLib, "$DOTNET");
mergeInto(LibraryManager.library, DotnetSupportLib);
