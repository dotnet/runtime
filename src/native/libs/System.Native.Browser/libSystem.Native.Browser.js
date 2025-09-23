//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//! This is generated file, see src/native/libs/Browser/rollup.config.defines.js


var libNativeBrowser = (function (exports) {
    'use strict';

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    let Module;
    let runtimeApi;
    let Logger = {};
    let Assert = {};
    let JSEngine = {};
    let loaderExports = {};
    let runtimeExports = {};
    let hostExports = {};
    let interopExports = {};
    let nativeBrowserExports = {};
    let dotnetInternals;
    function getInternals() {
        return dotnetInternals;
    }
    function setInternals(internal) {
        dotnetInternals = internal;
        runtimeApi = dotnetInternals.runtimeApi;
        Module = dotnetInternals.runtimeApi.Module;
    }
    function updateAllInternals() {
        if (dotnetInternals.updates === undefined) {
            dotnetInternals.updates = [];
        }
        for (const updateImpl of dotnetInternals.updates) {
            updateImpl();
        }
    }
    function updateMyInternals() {
        if (Object.keys(loaderExports).length === 0 && dotnetInternals.loaderExportsTable) {
            loaderExports = {};
            Logger = {};
            Assert = {};
            JSEngine = {};
            expandLE(dotnetInternals.loaderExportsTable, Logger, Assert, JSEngine, loaderExports);
        }
        if (Object.keys(runtimeExports).length === 0 && dotnetInternals.runtimeExportsTable) {
            runtimeExports = {};
            expandRE(dotnetInternals.runtimeExportsTable, runtimeExports);
        }
        if (Object.keys(hostExports).length === 0 && dotnetInternals.hostNativeExportsTable) {
            hostExports = {};
            expandHE(dotnetInternals.hostNativeExportsTable, hostExports);
        }
        if (Object.keys(interopExports).length === 0 && dotnetInternals.interopJavaScriptNativeExportsTable) {
            interopExports = {};
            expandJSNE(dotnetInternals.interopJavaScriptNativeExportsTable, interopExports);
        }
        if (Object.keys(nativeBrowserExports).length === 0 && dotnetInternals.nativeBrowserExportsTable) {
            nativeBrowserExports = {};
            expandNBE(dotnetInternals.nativeBrowserExportsTable, nativeBrowserExports);
        }
    }
    /**
     * Functions below allow our JS modules to exchange internal interfaces by passing tables of functions in known order instead of using string symbols.
     * IMPORTANT: If you need to add more functions, make sure that you add them at the end of the table, so that the order of existing functions does not change.
     */
    function tabulateLE(logger, assert, loaderExports) {
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
    function expandLE(table, logger, assert, jsEngine, loaderExports) {
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
    function tabulateRE(map) {
        return [];
    }
    function expandRE(table, runtime) {
        Object.assign(runtime, {});
    }
    function tabulateHE(map) {
        return [
            map.registerDllBytes,
            map.isSharedArrayBuffer,
        ];
    }
    function expandHE(table, native) {
        const nativeLocal = {
            registerDllBytes: table[0],
            isSharedArrayBuffer: table[1],
        };
        Object.assign(native, nativeLocal);
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function tabulateJSNE(map) {
        return [];
    }
    function expandJSNE(table, interop) {
        const interopLocal = {};
        Object.assign(interop, interopLocal);
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function tabulateNBE(map) {
        return [];
    }
    function expandNBE(table, interop) {
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
        expandHE: expandHE,
        expandJSNE: expandJSNE,
        expandLE: expandLE,
        expandNBE: expandNBE,
        expandRE: expandRE,
        getInternals: getInternals,
        get hostExports() { return hostExports; },
        get interopExports() { return interopExports; },
        get loaderExports() { return loaderExports; },
        get nativeBrowserExports() { return nativeBrowserExports; },
        get runtimeApi() { return runtimeApi; },
        get runtimeExports() { return runtimeExports; },
        setInternals: setInternals,
        tabulateHE: tabulateHE,
        tabulateJSNE: tabulateJSNE,
        tabulateLE: tabulateLE,
        tabulateNBE: tabulateNBE,
        tabulateRE: tabulateRE,
        updateAllInternals: updateAllInternals,
        updateMyInternals: updateMyInternals
    });

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    function SystemJS_RandomBytes(bufferPtr, bufferLength) {
        // batchedQuotaMax is the max number of bytes as specified by the api spec.
        // If the byteLength of array is greater than 65536, throw a QuotaExceededError and terminate the algorithm.
        // https://www.w3.org/TR/WebCryptoAPI/#Crypto-method-getRandomValues
        const batchedQuotaMax = 65536;
        if (!globalThis.crypto || !globalThis.crypto.getRandomValues) {
            if (!globalThis["cryptoWarnOnce"]) {
                Logger.warn("This engine doesn't support crypto.getRandomValues. Please use a modern version or provide polyfill for 'globalThis.crypto.getRandomValues'.");
                globalThis["cryptoWarnOnce"] = true;
            }
            return -1;
        }
        const memoryView = runtimeApi.localHeapViewU8();
        const targetView = memoryView.subarray(bufferPtr, bufferPtr + bufferLength);
        // When threading is enabled, Chrome doesn't want SharedArrayBuffer to be passed to crypto APIs
        const needsCopy = hostExports.isSharedArrayBuffer(memoryView.buffer);
        const targetBuffer = needsCopy
            ? new Uint8Array(bufferLength)
            : targetView;
        // fill the targetBuffer in batches of batchedQuotaMax
        for (let i = 0; i < bufferLength; i += batchedQuotaMax) {
            const targetBatch = targetBuffer.subarray(i, i + Math.min(bufferLength - i, batchedQuotaMax));
            globalThis.crypto.getRandomValues(targetBatch);
        }
        if (needsCopy) {
            targetView.set(targetBuffer);
        }
        return 0;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    function initialize(internals) {
        const nativeBrowserExportsLocal = {};
        setInternals(internals);
        internals.nativeBrowserExportsTable = [...tabulateNBE(nativeBrowserExportsLocal)];
        internals.updates.push(updateMyInternals);
        updateAllInternals();
    }

    exports.SystemJS_RandomBytes = SystemJS_RandomBytes;
    exports.cross = crossModule;
    exports.initialize = initialize;

    return exports;

});
//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements PAL for the VM/runtime.
 */

(function (exports) {
    function libFactory() {
        const lib = {
            $DOTNET: {
                selfInitialize: () => {
                    if (typeof dotnetInternals !== "undefined") {
                        DOTNET.dotnetInternals = dotnetInternals;
                        DOTNET.initialize(dotnetInternals);
                    }
                },
            },
            "$DOTNET__postset": "DOTNET.selfInitialize();",
        };

        // this executes the function at compile time in order to capture exports
        const exports = libNativeBrowser({});
        let commonDeps = [];
        for (const exportName of Reflect.ownKeys(exports.cross)) {
            const name = String(exportName);
            if (name === "dotnetInternals") continue;
            if (name === "Module") continue;
            const emName = "$" + name;
            lib[emName] = exports.cross[exportName];
            commonDeps.push(emName);
        }
        for (const exportName of Reflect.ownKeys(exports)) {
            const name = String(exportName);
            if (name === "cross") continue;
            if (name === "initialize") continue;
            lib[name] = exports[name];
        }
        lib["$DOTNET__deps"] = commonDeps;
        lib.$DOTNET.initialize = exports.initialize;

        autoAddDeps(lib, "$DOTNET");
        addToLibrary(lib);
    }
    libFactory();
    return exports;
})({});
