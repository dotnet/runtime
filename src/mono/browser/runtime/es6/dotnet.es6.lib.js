// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* eslint-disable no-undef */

"use strict";

// -- this javascript file is evaluated by emcc during compilation! --

// because we can't pass custom define symbols to acorn optimizer, we use environment variables to pass other build options
const WASM_ENABLE_SIMD = process.env.WASM_ENABLE_SIMD === "1";
const WASM_ENABLE_EH = process.env.WASM_ENABLE_EH === "1";
const ENABLE_BROWSER_PROFILER = process.env.ENABLE_BROWSER_PROFILER === "1";
const ENABLE_AOT_PROFILER = process.env.ENABLE_AOT_PROFILER === "1";
const RUN_AOT_COMPILATION = process.env.RUN_AOT_COMPILATION === "1";
var methodIndexByName = undefined;
var gitHash = undefined;

function setup(emscriptenBuildOptions) {
    // USE_PTHREADS is emscripten's define symbol, which is passed to acorn optimizer, so we could use it here
    #if USE_PTHREADS
    const modulePThread = PThread;
    #else
    const modulePThread = {};
    const ENVIRONMENT_IS_PTHREAD = false;
    #endif
    const dotnet_replacements = {
        fetch: globalThis.fetch,
        ENVIRONMENT_IS_WORKER,
        require,
        modulePThread,
        scriptDirectory,
    };

    ENVIRONMENT_IS_WORKER = dotnet_replacements.ENVIRONMENT_IS_WORKER;
    Module.__dotnet_runtime.initializeReplacements(dotnet_replacements);
    noExitRuntime = dotnet_replacements.noExitRuntime;
    fetch = dotnet_replacements.fetch;
    require = dotnet_replacements.require;
    _scriptDir = __dirname = scriptDirectory = dotnet_replacements.scriptDirectory;
    Module.__dotnet_runtime.passEmscriptenInternals({
        isPThread: ENVIRONMENT_IS_PTHREAD,
        quit_, ExitStatus,
        updateMemoryViews,
        getMemory: () => { return wasmMemory; },
        getWasmIndirectFunctionTable: () => { return wasmTable; },
    }, emscriptenBuildOptions);

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
}

const DotnetSupportLib = {
    $DOTNET: { setup },
    icudt68_dat: function () { throw new Error('dummy link symbol') },
};

function createWasmImportStubsFrom(collection) {
    for (let functionName in collection) {
        if (functionName in DotnetSupportLib) throw new Error(`Function ${functionName} is already defined`);
        const runtime_idx = collection[functionName]
        const stub_fn = new Function(`return {runtime_idx:${runtime_idx}};//${functionName}`);
        DotnetSupportLib[functionName] = stub_fn;
    }
}

// the JS methods would be visible to EMCC linker and become imports of the WASM module
// we generate simple stub for each exported function so that emcc will include them in the final output
// we will replace them with the real implementation in replace_linker_placeholders
function injectDependencies() {
    createWasmImportStubsFrom(methodIndexByName.mono_wasm_imports);

    #if USE_PTHREADS
    createWasmImportStubsFrom(methodIndexByName.mono_wasm_threads_imports);
    #endif

    DotnetSupportLib["$DOTNET__postset"] = `DOTNET.setup({ ` +
        `linkerWasmEnableSIMD: ${WASM_ENABLE_SIMD ? "true" : "false"},` +
        `linkerWasmEnableEH: ${WASM_ENABLE_EH ? "true" : "false"},` +
        `linkerEnableAotProfiler: ${ENABLE_AOT_PROFILER ? "true" : "false"}, ` +
        `linkerEnableBrowserProfiler: ${ENABLE_BROWSER_PROFILER ? "true" : "false"}, ` +
        `linkerRunAOTCompilation: ${RUN_AOT_COMPILATION ? "true" : "false"}, ` +
        `linkerUseThreads: ${USE_PTHREADS ? "true" : "false"}, ` +
        `moduleGitHash: "${gitHash}", ` +
        `});`;

    autoAddDeps(DotnetSupportLib, "$DOTNET");
    mergeInto(LibraryManager.library, DotnetSupportLib);
}


// var methodIndexByName wil be appended below by the MSBuild in browser.proj via exports-linker.ts
