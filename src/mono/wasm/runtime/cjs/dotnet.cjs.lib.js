// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* eslint-disable no-undef */

"use strict";

const DotnetSupportLib = {
    $DOTNET: {},
    // these lines will be placed early on emscripten runtime creation, passing import and export objects into __dotnet_runtime IFFE
    // we replace implementation of readAsync and fetch
    // replacement of require is there for consistency with ES6 code
    $DOTNET__postset: `
let __dotnet_replacements = {readAsync, fetch: globalThis.fetch, require};
let __dotnet_exportedAPI = __dotnet_runtime.__initializeImportsAndExports(
    { isESM:false, isGlobal:ENVIRONMENT_IS_GLOBAL, isNode:ENVIRONMENT_IS_NODE, isShell:ENVIRONMENT_IS_SHELL, isWeb:ENVIRONMENT_IS_WEB, locateFile, quit_, ExitStatus, requirePromise:Promise.resolve(require)},
    { mono:MONO, binding:BINDING, internal:INTERNAL, module:Module },
    __dotnet_replacements);
readAsync = __dotnet_replacements.readAsync;
var fetch = __dotnet_replacements.fetch;
require = __dotnet_replacements.requireOut;
var noExitRuntime = __dotnet_replacements.noExitRuntime;
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
    "mono_wasm_invoke_js",
    "mono_wasm_invoke_js_blazor",
    "mono_wasm_trace_logger",

    // corebindings.c
    "mono_wasm_invoke_js_with_args",
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
    "mono_wasm_cancel_promise",
    "mono_wasm_web_socket_open_ref",
    "mono_wasm_web_socket_send",
    "mono_wasm_web_socket_receive",
    "mono_wasm_web_socket_close_ref",
    "mono_wasm_web_socket_abort",
    "mono_wasm_compile_function",

    // pal_icushim_static.c
    "mono_wasm_load_icu_data",
    "mono_wasm_get_icudt_name",
];

// -- this javascript file is evaluated by emcc during compilation! --
// we generate simple proxy for each exported function so that emcc will include them in the final output
for (let linked_function of linked_functions) {
    const fn_template = `return __dotnet_runtime.__linker_exports.${linked_function}.apply(__dotnet_runtime, arguments)`;
    DotnetSupportLib[linked_function] = new Function(fn_template);
}

autoAddDeps(DotnetSupportLib, "$DOTNET");
mergeInto(LibraryManager.library, DotnetSupportLib);
