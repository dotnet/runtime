// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* eslint-disable no-undef */

"use strict";

// -- this javascript file is evaluated by emcc during compilation! --

// because we can't pass custom define symbols to acorn optimizer, we use environment variables to pass other build options
const DISABLE_LEGACY_JS_INTEROP = process.env.DISABLE_LEGACY_JS_INTEROP === "1";
const WASM_ENABLE_SIMD = process.env.WASM_ENABLE_SIMD === "1";
const WASM_ENABLE_EH = process.env.WASM_ENABLE_EH === "1";
const ENABLE_BROWSER_PROFILER = process.env.ENABLE_BROWSER_PROFILER === "1";
const ENABLE_AOT_PROFILER = process.env.ENABLE_AOT_PROFILER === "1";
var methodIndexByName = undefined;

function setup(linkerSetup) {
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
        quit_, ExitStatus,
        ...linkerSetup
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
    DOTNET.setup({ `+
    `linkerDisableLegacyJsInterop: ${DISABLE_LEGACY_JS_INTEROP ? "true" : "false"},` +
    `linkerWasmEnableSIMD: ${WASM_ENABLE_SIMD ? "true" : "false"},` +
    `linkerWasmEnableEH: ${WASM_ENABLE_EH ? "true" : "false"},` +
    `linkerEnableAotProfiler: ${ENABLE_AOT_PROFILER ? "true" : "false"}, ` +
    `linkerEnableBrowserProfiler: ${ENABLE_BROWSER_PROFILER ? "true" : "false"}` +
    `});
`;

const DotnetSupportLib = {
    $DOTNET: { setup },
    $DOTNET__postset: postset,
    icudt68_dat: function () { throw new Error('dummy link symbol') },
};

// the JS methods would be visible to EMCC linker and become imports of the WASM module
// we generate simple stub for each exported function so that emcc will include them in the final output
// we will replace them with the real implementation in replace_linker_placeholders
function createWasmImportStubs() {
    for (let functionName in methodIndexByName.mono_wasm_imports) {
        const idx = methodIndexByName.mono_wasm_imports[functionName];
        DotnetSupportLib[functionName] = new Function(`return {runtime_idx:${idx}};//${functionName}`);
    }

    #if USE_PTHREADS
    for (let functionName in methodIndexByName.mono_wasm_threads_imports) {
        const idx = methodIndexByName.mono_wasm_threads_imports[functionName];
        DotnetSupportLib[functionName] = new Function(`return {runtime_idx:${idx}};//${functionName}`);
    }
    #endif

    if (!DISABLE_LEGACY_JS_INTEROP) {
        for (let functionName in methodIndexByName.mono_wasm_legacy_interop_imports) {
            const idx = methodIndexByName.mono_wasm_legacy_interop_imports[functionName];
            DotnetSupportLib[functionName] = new Function(`return {runtime_idx:${idx}};//${functionName}`);
        }
    }

    autoAddDeps(DotnetSupportLib, "$DOTNET");
    mergeInto(LibraryManager.library, DotnetSupportLib);
}


// var methodIndexByName wil be appended below by the MSBuild in wasm.proj
