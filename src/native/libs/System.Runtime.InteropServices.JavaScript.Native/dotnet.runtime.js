//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//! This is generated file, see src/native/libs/Browser/rollup.config.defines.js


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
let Module;
let runtimeApi;
let Logger = {};
let Assert = {};
let JSEngine = {};
let loaderExports = {};
let runtimeExports = {};
let nativeExports = {};
let interopExports = {};
let dotnetInternals;
function getInternals() {
    return dotnetInternals;
}
function setInternals(internal) {
    dotnetInternals = internal;
    runtimeApi = dotnetInternals.runtimeApi;
    Module = dotnetInternals.runtimeApi.Module;
}
function updateInternals() {
    if (dotnetInternals.updates === undefined) {
        dotnetInternals.updates = [];
    }
    for (const updateImpl of dotnetInternals.updates) {
        updateImpl();
    }
}
function updateInternalsImpl() {
    if (Object.keys(loaderExports).length === 0 && dotnetInternals.loaderExportsTable) {
        loaderExports = {};
        Logger = {};
        Assert = {};
        JSEngine = {};
        loaderExportsFromTable(dotnetInternals.loaderExportsTable, Logger, Assert, JSEngine, loaderExports);
    }
    if (Object.keys(runtimeExports).length === 0 && dotnetInternals.runtimeExportsTable) {
        runtimeExports = {};
        runtimeExportsFromTable(dotnetInternals.runtimeExportsTable, runtimeExports);
    }
    if (Object.keys(nativeExports).length === 0 && dotnetInternals.hostNativeExportsTable) {
        nativeExports = {};
        hostNativeExportsFromTable(dotnetInternals.hostNativeExportsTable, nativeExports);
    }
    if (Object.keys(interopExports).length === 0 && dotnetInternals.interopJavaScriptNativeExportsTable) {
        interopExports = {};
        interopJavaScriptNativeExportsFromTable(dotnetInternals.interopJavaScriptNativeExportsTable, interopExports);
    }
}
/**
 * Functions below allow our JS modules to exchange internal interfaces by passing tables of functions in known order instead of using string symbols.
 * IMPORTANT: If you need to add more functions, make sure that you add them at the end of the table, so that the order of existing functions does not change.
 */
function loaderExportsToTable(logger, assert, loaderExports) {
    return [
        logger.info,
        logger.warn,
        logger.error,
        assert.check,
        loaderExports.ENVIRONMENT_IS_NODE,
        loaderExports.ENVIRONMENT_IS_SHELL,
        loaderExports.ENVIRONMENT_IS_WEB,
        loaderExports.ENVIRONMENT_IS_WORKER,
        loaderExports.ENVIRONMENT_IS_SIDECAR,
        loaderExports.browserHostResolveMain,
        loaderExports.browserHostRejectMain,
        loaderExports.getRunMainPromise,
    ];
}
function loaderExportsFromTable(table, logger, assert, jsEngine, loaderExports) {
    const loggerLocal = {
        info: table[0],
        warn: table[1],
        error: table[2],
    };
    const assertLocal = {
        check: table[3],
    };
    const loaderExportsLocal = {
        ENVIRONMENT_IS_NODE: table[4],
        ENVIRONMENT_IS_SHELL: table[5],
        ENVIRONMENT_IS_WEB: table[6],
        ENVIRONMENT_IS_WORKER: table[7],
        ENVIRONMENT_IS_SIDECAR: table[8],
        browserHostResolveMain: table[9],
        browserHostRejectMain: table[10],
        getRunMainPromise: table[11],
    };
    const jsEngineLocal = {
        IS_NODE: loaderExportsLocal.ENVIRONMENT_IS_NODE(),
        IS_SHELL: loaderExportsLocal.ENVIRONMENT_IS_SHELL(),
        IS_WEB: loaderExportsLocal.ENVIRONMENT_IS_WEB(),
        IS_WORKER: loaderExportsLocal.ENVIRONMENT_IS_WORKER(),
        IS_SIDECAR: loaderExportsLocal.ENVIRONMENT_IS_SIDECAR(),
    };
    Object.assign(loaderExports, loaderExportsLocal);
    Object.assign(logger, loggerLocal);
    Object.assign(assert, assertLocal);
    Object.assign(jsEngine, jsEngineLocal);
}
// eslint-disable-next-line @typescript-eslint/no-unused-vars
function runtimeExportsToTable(map) {
    return [];
}
function runtimeExportsFromTable(table, runtime) {
    Object.assign(runtime, {});
}
function hostNativeExportsToTable(map) {
    return [
        map.registerDllBytes,
        map.isSharedArrayBuffer,
    ];
}
function hostNativeExportsFromTable(table, native) {
    const nativeLocal = {
        registerDllBytes: table[0],
        isSharedArrayBuffer: table[1],
    };
    Object.assign(native, nativeLocal);
}
// eslint-disable-next-line @typescript-eslint/no-unused-vars
function interopJavaScriptNativeExportsToTable(map) {
    return [];
}
function interopJavaScriptNativeExportsFromTable(table, interop) {
    const interopLocal = {};
    Object.assign(interop, interopLocal);
}
// eslint-disable-next-line @typescript-eslint/no-unused-vars
function nativeBrowserExportsToTable(map) {
    return [];
}
function nativeBrowserExportsFromTable(table, interop) {
    const interopLocal = {};
    Object.assign(interop, interopLocal);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
function initialize(internals) {
    const runtimeApiLocal = {
        getAssemblyExports,
        setModuleImports,
    };
    const runtimeExportsLocal = {};
    setInternals(internals);
    Object.assign(internals.runtimeApi, runtimeApiLocal);
    internals.runtimeExportsTable = [...runtimeExportsToTable(runtimeExportsLocal)];
    internals.updates.push(updateInternalsImpl);
    updateInternals();
}
// eslint-disable-next-line @typescript-eslint/no-unused-vars
async function getAssemblyExports(assemblyName) {
    throw new Error("Not implemented");
}
// eslint-disable-next-line @typescript-eslint/no-unused-vars
function setModuleImports(moduleName, moduleImports) {
    throw new Error("Not implemented");
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export { initialize };
