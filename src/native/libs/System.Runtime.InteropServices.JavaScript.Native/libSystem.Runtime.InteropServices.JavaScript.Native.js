//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//! This is generated file, see src/native/libs/Browser/rollup.config.defines.js


var libSystemRuntimeInteropServicesJavaScriptNativeBrowserJS = (function (exports) {
    'use strict';

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    let JSEngine;
    let Module;
    let runtimeApi;
    let Logger = {};
    let Assert = {};
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
        if (!JSEngine) {
            const ENVIRONMENT_IS_NODE = typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
            const ENVIRONMENT_IS_WEB_WORKER = typeof importScripts == "function";
            const ENVIRONMENT_IS_SIDECAR = ENVIRONMENT_IS_WEB_WORKER && typeof dotnetSidecar !== "undefined"; // sidecar is emscripten main running in a web worker
            const ENVIRONMENT_IS_WORKER = ENVIRONMENT_IS_WEB_WORKER && !ENVIRONMENT_IS_SIDECAR; // we redefine what ENVIRONMENT_IS_WORKER, we replace it in emscripten internals, so that sidecar works
            const ENVIRONMENT_IS_WEB = typeof window == "object" || (ENVIRONMENT_IS_WEB_WORKER && !ENVIRONMENT_IS_NODE);
            const ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE;
            JSEngine = {
                IS_NODE: ENVIRONMENT_IS_NODE,
                IS_SHELL: ENVIRONMENT_IS_SHELL,
                IS_WEB: ENVIRONMENT_IS_WEB,
                IS_WORKER: ENVIRONMENT_IS_WORKER,
                IS_SIDECAR: ENVIRONMENT_IS_SIDECAR,
            };
        }
        if (Object.keys(loaderExports).length === 0 && dotnetInternals.loaderExportsTable) {
            loaderExports = {};
            Logger = {};
            Assert = {};
            loaderExportsFromTable(dotnetInternals.loaderExportsTable, Logger, Assert, loaderExports);
        }
        if (Object.keys(runtimeExports).length === 0 && dotnetInternals.runtimeExportsTable) {
            runtimeExports = {};
            runtimeExportsFromTable(dotnetInternals.runtimeExportsTable, runtimeExports);
        }
        if (Object.keys(nativeExports).length === 0 && dotnetInternals.nativeExportsTable) {
            nativeExports = {};
            nativeExportsFromTable(dotnetInternals.nativeExportsTable, nativeExports);
        }
        if (Object.keys(interopExports).length === 0 && dotnetInternals.interopExportsTable) {
            interopExports = {};
            interopExportsFromTable(dotnetInternals.interopExportsTable, interopExports);
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
            loaderExports.browserHostResolveMain,
            loaderExports.browserHostRejectMain,
            loaderExports.getRunMainPromise,
        ];
    }
    function loaderExportsFromTable(table, logger, assert, loaderExports) {
        const loggerLocal = {
            info: table[0],
            warn: table[1],
            error: table[2],
        };
        const assertLocal = {
            check: table[3],
        };
        const loaderExportsLocal = {
            browserHostResolveMain: table[4],
            browserHostRejectMain: table[5],
            getRunMainPromise: table[6],
        };
        Object.assign(logger, loggerLocal);
        Object.assign(assert, assertLocal);
        Object.assign(loaderExports, loaderExportsLocal);
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function runtimeExportsToTable(map) {
        return [];
    }
    function runtimeExportsFromTable(table, runtime) {
        Object.assign(runtime, {});
    }
    function nativeExportsToTable(map) {
        return [
            map.registerDllBytes,
            map.isSharedArrayBuffer,
        ];
    }
    function nativeExportsFromTable(table, native) {
        const nativeLocal = {
            registerDllBytes: table[0],
            isSharedArrayBuffer: table[1],
        };
        Object.assign(native, nativeLocal);
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function interopExportsToTable(map) {
        return [];
    }
    function interopExportsFromTable(table, interop) {
        const interopLocal = {};
        Object.assign(interop, interopLocal);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.

    var crossModule = /*#__PURE__*/Object.freeze({
        __proto__: null,
        get Assert() { return Assert; },
        get JSEngine() { return JSEngine; },
        get Logger() { return Logger; },
        get Module() { return Module; },
        get dotnetInternals() { return dotnetInternals; },
        getInternals: getInternals,
        get interopExports() { return interopExports; },
        interopExportsFromTable: interopExportsFromTable,
        interopExportsToTable: interopExportsToTable,
        get loaderExports() { return loaderExports; },
        loaderExportsFromTable: loaderExportsFromTable,
        loaderExportsToTable: loaderExportsToTable,
        get nativeExports() { return nativeExports; },
        nativeExportsFromTable: nativeExportsFromTable,
        nativeExportsToTable: nativeExportsToTable,
        get runtimeApi() { return runtimeApi; },
        get runtimeExports() { return runtimeExports; },
        runtimeExportsFromTable: runtimeExportsFromTable,
        runtimeExportsToTable: runtimeExportsToTable,
        setInternals: setInternals,
        updateInternals: updateInternals,
        updateInternalsImpl: updateInternalsImpl
    });

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    async function initialize(internals) {
        const interopExportsFunctions = {};
        Object.assign(interopExports, interopExportsFunctions);
        internals.interopExportsTable = [...interopExportsToTable(interopExportsFunctions)];
        internals.updates.push(updateInternalsImpl);
        updateInternals();
    }

    var nativeExchange = /*#__PURE__*/Object.freeze({
        __proto__: null,
        initialize: initialize
    });

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function SystemInteropJS_InvokeJSImportST(function_handle, args) {
        // WASMTODO implementation
        Logger.error("SystemInteropJS_InvokeJSImportST called");
        return -1;
    }
    SystemInteropJS_InvokeJSImportST["__deps"] = ["loadedAssemblies"];

    var trimmableExports = /*#__PURE__*/Object.freeze({
        __proto__: null,
        SystemInteropJS_InvokeJSImportST: SystemInteropJS_InvokeJSImportST
    });

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    /**
     * This is root of **Emscripten library** that would become part of `dotnet.native.js`
     * It implements interop between JS and .NET
     */
    function DotnetInteropLibFactory() {
        // Symbols that would be protected from emscripten linker
        const moduleDeps = ["$DOTNET"];
        const trimmableDeps = {};
        for (const exportName of Reflect.ownKeys(trimmableExports)) {
            const emName = exportName.toString() + "__deps";
            const deps = trimmableExports[exportName]["__deps"];
            if (deps) {
                trimmableDeps[emName] = deps;
            }
        }
        const lib = {
            $DOTNET_INTEROP: {
                selfInitialize: () => {
                    if (typeof dotnetInternals !== "undefined") {
                        DOTNET_INTEROP.dotnetInternals = dotnetInternals;
                        DOTNET_INTEROP.initialize(dotnetInternals);
                    }
                },
                initialize: initialize,
            },
            "$DOTNET_INTEROP__deps": moduleDeps,
            "$DOTNET_INTEROP__postset": "DOTNET_INTEROP.selfInitialize();",
            ...trimmableExports,
            ...trimmableDeps,
        };
        autoAddDeps(lib, "$DOTNET_INTEROP");
        addToLibrary(lib);
    }
    DotnetInteropLibFactory();

    exports.commonInfra = crossModule;
    exports.exchange = nativeExchange;
    exports.trimmableExports = trimmableExports;

    return exports;

})({});
