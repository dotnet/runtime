// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* eslint-disable no-undef */

"use strict";

const DotnetSupportLib = {
    $DOTNET: {},
    // this line will be placed early on emscripten runtime creation, passing import and export objects into __dotnet_runtime IFFE
    // Emscripten uses require function for nodeJS even in ES6 module. We need https://nodejs.org/api/module.html#modulecreaterequirefilename
    // We use dynamic import because there is no "module" module in the browser. 
    // This is async init of it, note it would become available only after first tick.
    // Also fix of scriptDirectory would be delayed
    // Emscripten's getBinaryPromise is not async for NodeJs, but we would like to have it async, so we replace it.
    // We also replace implementation of readAsync and fetch
    $DOTNET__postset: `
let __dotnet_replacements = {readAsync, fetch: globalThis.fetch, require};
if (ENVIRONMENT_IS_NODE) {
    __dotnet_replacements.requirePromise = import('module').then(mod => {
        const require = mod.createRequire(import.meta.url);
        const path = require('path');
        const url = require('url');
        __dotnet_replacements.require = require;
        __dirname = scriptDirectory = path.dirname(url.fileURLToPath(import.meta.url)) + '/';
        return require;
    });
    getBinaryPromise = async () => {
        if (!wasmBinary) {
            try {
                if (typeof fetch === 'function' && !isFileURI(wasmBinaryFile)) {
                    const response = await fetch(wasmBinaryFile, { credentials: 'same-origin' });
                    if (!response['ok']) {
                        throw "failed to load wasm binary file at '" + wasmBinaryFile + "'";
                    }
                    return response['arrayBuffer']();
                }
                else if (readAsync) {
                    return await new Promise(function (resolve, reject) {
                        readAsync(wasmBinaryFile, function (response) { resolve(new Uint8Array(/** @type{!ArrayBuffer} */(response))) }, reject)
                    });
                }
    
            }
            catch (err) {
                return getBinary(wasmBinaryFile);
            }
        }
        return getBinary(wasmBinaryFile);
    }
}
let __dotnet_exportedAPI = __dotnet_runtime.__initializeImportsAndExports(
    { isESM:true, isGlobal:false, isNode:ENVIRONMENT_IS_NODE, isShell:ENVIRONMENT_IS_SHELL, isWeb:ENVIRONMENT_IS_WEB, locateFile, quit_, ExitStatus, requirePromise:__dotnet_replacements.requirePromise }, 
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
    "mono_wasm_get_object_property",
    "mono_wasm_set_object_property",
    "mono_wasm_get_by_index",
    "mono_wasm_set_by_index",
    "mono_wasm_get_global_object",
    "mono_wasm_create_cs_owned_object",
    "mono_wasm_release_cs_owned_object",
    "mono_wasm_typed_array_to_array",
    "mono_wasm_typed_array_copy_to",
    "mono_wasm_typed_array_from",
    "mono_wasm_typed_array_copy_from",
    "mono_wasm_cancel_promise",
    "mono_wasm_web_socket_open",
    "mono_wasm_web_socket_send",
    "mono_wasm_web_socket_receive",
    "mono_wasm_web_socket_close",
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
