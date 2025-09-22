//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//! This is generated file, see src/native/libs/Browser/rollup.config.defines.js


var libBrowserHost = (function (exports) {
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

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    // see also `reserved` in `rollup.config.defines.js`

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    const loadedAssemblies = {};
    function registerDllBytes(bytes, asset) {
        const sp = Module.stackSave();
        try {
            const sizeOfPtr = 4;
            const ptrPtr = Module.stackAlloc(sizeOfPtr);
            if (Module._posix_memalign(ptrPtr, 16, bytes.length)) {
                throw new Error("posix_memalign failed");
            }
            const ptr = Module.HEAPU32[ptrPtr >>> 2];
            Module.HEAPU8.set(bytes, ptr);
            loadedAssemblies[asset.name] = { ptr, length: bytes.length };
        }
        finally {
            Module.stackRestore(sp);
        }
    }
    // bool browserHostExternalAssemblyProbe(const char* pathPtr, /*out*/ void **outDataStartPtr, /*out*/ int64_t* outSize);
    function browserHostExternalAssemblyProbe(pathPtr, outDataStartPtr, outSize) {
        const path = Module.UTF8ToString(pathPtr);
        const assembly = loadedAssemblies[path];
        if (assembly) {
            Module.HEAPU32[outDataStartPtr >>> 2] = assembly.ptr;
            // int64_t target
            Module.HEAPU32[outSize >>> 2] = assembly.length;
            Module.HEAPU32[(outSize + 4) >>> 2] = 0;
            return true;
        }
        Module.HEAPU32[outDataStartPtr >>> 2] = 0;
        Module.HEAPU32[outSize >>> 2] = 0;
        Module.HEAPU32[(outSize + 4) >>> 2] = 0;
        return false;
    }
    function browserHostResolveMain(exitCode) {
        loaderExports.browserHostResolveMain(exitCode);
    }
    function browserHostRejectMain(reason) {
        loaderExports.browserHostRejectMain(reason);
    }
    // TODO-WASM: take ideas from Mono
    // - second call to exit should be silent
    // - second call to exit not override the first exit code
    // - improve reason extraction
    // - install global handler for unhandled exceptions and promise rejections
    function exit(exit_code, reason) {
        const reasonStr = reason ? (reason.stack ? reason.stack || reason.message : reason.toString()) : "";
        if (exit_code !== 0) {
            Logger.error(`Exit with code ${exit_code} ${reason ? "and reason: " + reasonStr : ""}`);
        }
        if (JSEngine.IS_NODE) {
            globalThis.process.exit(exit_code);
        }
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    async function runMain(mainAssemblyName, args) {
        // int browserHostExecuteAssembly(char * assemblyPath)
        const res = Module.ccall("browserHostExecuteAssembly", "number", ["string"], [mainAssemblyName]);
        if (res != 0) {
            const reason = new Error("Failed to execute assembly");
            exit(res, reason);
            throw reason;
        }
        return loaderExports.getRunMainPromise();
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    async function runMainAndExit(mainAssemblyName, args) {
        try {
            await runMain(mainAssemblyName, args);
        }
        catch (error) {
            exit(1, error);
            throw error;
        }
        exit(0, null);
        return 0;
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function setEnvironmentVariable(name, value) {
        throw new Error("Not implemented");
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    const max_int64_big = BigInt("9223372036854775807");
    const min_int64_big = BigInt("-9223372036854775808");
    const sharedArrayBufferDefined = typeof SharedArrayBuffer !== "undefined";
    function assert_int_in_range(value, min, max) {
        Assert.check(Number.isSafeInteger(value), () => `Value is not an integer: ${value} (${typeof (value)})`);
        Assert.check(value >= min && value <= max, () => `Overflow: value ${value} is out of ${min} ${max} range`);
    }
    function _zero_region(byteOffset, sizeBytes) {
        localHeapViewU8().fill(0, byteOffset, byteOffset + sizeBytes);
    }
    /** note: boolean is 8 bits not 32 bits when inside a structure or array */
    function setHeapB32(offset, value) {
        const boolValue = !!value;
        if (typeof (value) === "number")
            assert_int_in_range(value, 0, 1);
        Module.HEAP32[offset >>> 2] = boolValue ? 1 : 0;
    }
    function setHeapB8(offset, value) {
        const boolValue = !!value;
        if (typeof (value) === "number")
            assert_int_in_range(value, 0, 1);
        Module.HEAPU8[offset] = boolValue ? 1 : 0;
    }
    function setHeapU8(offset, value) {
        assert_int_in_range(value, 0, 0xFF);
        Module.HEAPU8[offset] = value;
    }
    function setHeapU16(offset, value) {
        assert_int_in_range(value, 0, 0xFFFF);
        Module.HEAPU16[offset >>> 1] = value;
    }
    // does not check for growable heap
    function setHeapU16_local(localView, offset, value) {
        assert_int_in_range(value, 0, 0xFFFF);
        localView[offset >>> 1] = value;
    }
    // does not check for overflow nor growable heap
    function setHeapU16_unchecked(offset, value) {
        Module.HEAPU16[offset >>> 1] = value;
    }
    // does not check for overflow nor growable heap
    function setHeapU32_unchecked(offset, value) {
        Module.HEAPU32[offset >>> 2] = value;
    }
    function setHeapU32(offset, value) {
        assert_int_in_range(value, 0, 4294967295);
        Module.HEAPU32[offset >>> 2] = value;
    }
    function setHeapI8(offset, value) {
        assert_int_in_range(value, -0x80, 0x7F);
        Module.HEAP8[offset] = value;
    }
    function setHeapI16(offset, value) {
        assert_int_in_range(value, -0x8000, 0x7FFF);
        Module.HEAP16[offset >>> 1] = value;
    }
    function setHeapI32_unchecked(offset, value) {
        Module.HEAP32[offset >>> 2] = value;
    }
    function setHeapI32(offset, value) {
        assert_int_in_range(value, -2147483648, 2147483647);
        Module.HEAP32[offset >>> 2] = value;
    }
    /**
     * Throws for values which are not 52 bit integer. See Number.isSafeInteger()
     */
    function setHeapI52(offset, value) {
        Assert.check(Number.isSafeInteger(value), () => `Value is not a safe integer: ${value} (${typeof (value)})`);
        throw new Error("TODO");
        // const error = cwraps.mono_wasm_f64_to_i52(<any>offset, value);
        // autoThrowI52(error);
    }
    /**
     * Throws for values which are not 52 bit integer or are negative. See Number.isSafeInteger().
     */
    function setHeapU52(offset, value) {
        Assert.check(Number.isSafeInteger(value), () => `Value is not a safe integer: ${value} (${typeof (value)})`);
        Assert.check(value >= 0, "Can't convert negative Number into UInt64");
        throw new Error("TODO");
        //const error = cwraps.mono_wasm_f64_to_u52(<any>offset, value);
        //autoThrowI52(error);
    }
    function setHeapI64Big(offset, value) {
        Assert.check(typeof value === "bigint", () => `Value is not an bigint: ${value} (${typeof (value)})`);
        Assert.check(value >= min_int64_big && value <= max_int64_big, () => `Overflow: value ${value} is out of ${min_int64_big} ${max_int64_big} range`);
        Module.HEAP64[offset >>> 3] = value;
    }
    function setHeapF32(offset, value) {
        Assert.check(typeof value === "number", () => `Value is not a Number: ${value} (${typeof (value)})`);
        Module.HEAPF32[offset >>> 2] = value;
    }
    function setHeapF64(offset, value) {
        Assert.check(typeof value === "number", () => `Value is not a Number: ${value} (${typeof (value)})`);
        Module.HEAPF64[offset >>> 3] = value;
    }
    function getHeapB32(offset) {
        const value = (Module.HEAPU32[offset >>> 2]);
        if (value > 1 && !getHeapB32.warnDirtyBool) {
            getHeapB32.warnDirtyBool = true;
            Logger.warn(`getB32: value at ${offset} is not a boolean, but a number: ${value}`);
        }
        return !!value;
    }
    function getHeapB8(offset) {
        return !!(Module.HEAPU8[offset]);
    }
    function getHeapU8(offset) {
        return Module.HEAPU8[offset];
    }
    function getHeapU16(offset) {
        return Module.HEAPU16[offset >>> 1];
    }
    // does not check for growable heap
    function getHeapU16_local(localView, offset) {
        return localView[offset >>> 1];
    }
    function getHeapU32(offset) {
        return Module.HEAPU32[offset >>> 2];
    }
    // does not check for growable heap
    function getHeapU32_local(localView, offset) {
        return localView[offset >>> 2];
    }
    function getHeapI8(offset) {
        return Module.HEAP8[offset];
    }
    function getHeapI16(offset) {
        return Module.HEAP16[offset >>> 1];
    }
    // does not check for growable heap
    function getHeapI16_local(localView, offset) {
        return localView[offset >>> 1];
    }
    function getHeapI32(offset) {
        return Module.HEAP32[offset >>> 2];
    }
    // does not check for growable heap
    function getHeapI32_local(localView, offset) {
        return localView[offset >>> 2];
    }
    /**
     * Throws for Number.MIN_SAFE_INTEGER > value > Number.MAX_SAFE_INTEGER
     */
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function getHeapI52(offset) {
        throw new Error("TODO");
        //const result = cwraps.mono_wasm_i52_to_f64(<any>offset, runtimeHelpers._i52_error_scratch_buffer);
        //const error = getI32(runtimeHelpers._i52_error_scratch_buffer);
        //autoThrowI52(error);
        //return result;
    }
    /**
     * Throws for 0 > value > Number.MAX_SAFE_INTEGER
     */
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function getHeapU52(offset) {
        throw new Error("TODO");
        //const result = cwraps.mono_wasm_u52_to_f64(<any>offset, runtimeHelpers._i52_error_scratch_buffer);
        //const error = getI32(runtimeHelpers._i52_error_scratch_buffer);
        //autoThrowI52(error);
        //return result;
    }
    function getHeapI64Big(offset) {
        return Module.HEAP64[offset >>> 3];
    }
    function getHeapF32(offset) {
        return Module.HEAPF32[offset >>> 2];
    }
    function getHeapF64(offset) {
        return Module.HEAPF64[offset >>> 3];
    }
    // returns memory view which is valid within current synchronous call stack
    function localHeapViewI8() {
        return Module.HEAP8;
    }
    // returns memory view which is valid within current synchronous call stack
    function localHeapViewI16() {
        return Module.HEAP16;
    }
    // returns memory view which is valid within current synchronous call stack
    function localHeapViewI32() {
        return Module.HEAP32;
    }
    // returns memory view which is valid within current synchronous call stack
    function localHeapViewI64Big() {
        return Module.HEAP64;
    }
    // returns memory view which is valid within current synchronous call stack
    function localHeapViewU8() {
        return Module.HEAPU8;
    }
    // returns memory view which is valid within current synchronous call stack
    function localHeapViewU16() {
        return Module.HEAPU16;
    }
    // returns memory view which is valid within current synchronous call stack
    function localHeapViewU32() {
        return Module.HEAPU32;
    }
    // returns memory view which is valid within current synchronous call stack
    function localHeapViewF32() {
        return Module.HEAPF32;
    }
    // returns memory view which is valid within current synchronous call stack
    function localHeapViewF64() {
        return Module.HEAPF64;
    }
    function copyBytes(srcPtr, dstPtr, bytes) {
        const heap = localHeapViewU8();
        heap.copyWithin(dstPtr, srcPtr, srcPtr + bytes);
    }
    function isSharedArrayBuffer(buffer) {
        // BEWARE: In some cases, `instanceof SharedArrayBuffer` returns false even though buffer is an SAB.
        // Patch adapted from https://github.com/emscripten-core/emscripten/pull/16994
        // See also https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Symbol/toStringTag
        return sharedArrayBufferDefined && buffer[Symbol.toStringTag] === "SharedArrayBuffer";
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    function initialize(internals) {
        const runtimeApiLocal = {
            runMain,
            runMainAndExit,
            setEnvironmentVariable,
            exit,
            setHeapB32, setHeapB8, setHeapU8, setHeapU16, setHeapU32, setHeapI8, setHeapI16, setHeapI32, setHeapI52, setHeapU52, setHeapI64Big, setHeapF32, setHeapF64,
            getHeapB32, getHeapB8, getHeapU8, getHeapU16, getHeapU32, getHeapI8, getHeapI16, getHeapI32, getHeapI52, getHeapU52, getHeapI64Big, getHeapF32, getHeapF64,
            localHeapViewI8, localHeapViewI16, localHeapViewI32, localHeapViewI64Big, localHeapViewU8, localHeapViewU16, localHeapViewU32, localHeapViewF32, localHeapViewF64,
        };
        const hostNativeExportsLocal = {
            registerDllBytes,
            isSharedArrayBuffer
        };
        setInternals(internals);
        Object.assign(internals.runtimeApi, runtimeApiLocal);
        internals.hostNativeExportsTable = [...tabulateHE(hostNativeExportsLocal)];
        internals.updates.push(updateMyInternals);
        updateAllInternals();
    }

    exports.browserHostExternalAssemblyProbe = browserHostExternalAssemblyProbe;
    exports.browserHostRejectMain = browserHostRejectMain;
    exports.browserHostResolveMain = browserHostResolveMain;
    exports.initialize = initialize;

    return exports;

});
//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements the corehost and a part of public JS API related to memory and runtime hosting.
 */

(function (exports) {
    function libFactory() {
        const lib = {
            $BROWSER_HOST: {
                selfInitialize: () => {
                    if (typeof dotnetInternals !== "undefined") {
                        BROWSER_HOST.dotnetInternals = dotnetInternals;

                        const exports = libBrowserHostFn(BROWSER_HOST);
                        exports.initialize(dotnetInternals);
                        BROWSER_HOST.assignExports(exports, BROWSER_HOST);

                        const HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES = "TRUSTED_PLATFORM_ASSEMBLIES";
                        const HOST_PROPERTY_ENTRY_ASSEMBLY_NAME = "ENTRY_ASSEMBLY_NAME";
                        const HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES = "NATIVE_DLL_SEARCH_DIRECTORIES";
                        const HOST_PROPERTY_APP_PATHS = "APP_PATHS";

                        const config = dotnetInternals.config;
                        const assemblyPaths = config.resources.assembly.map(a => a.virtualPath);
                        const coreAssemblyPaths = config.resources.coreAssembly.map(a => a.virtualPath);
                        ENV[HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES] = config.environmentVariables[HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES] = [...coreAssemblyPaths, assemblyPaths].join(":");
                        ENV[HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES] = config.environmentVariables[HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES] = config.virtualWorkingDirectory;
                        ENV[HOST_PROPERTY_APP_PATHS] = config.environmentVariables[HOST_PROPERTY_APP_PATHS] = config.virtualWorkingDirectory;
                        ENV[HOST_PROPERTY_ENTRY_ASSEMBLY_NAME] = config.environmentVariables[HOST_PROPERTY_ENTRY_ASSEMBLY_NAME] = config.mainAssemblyName;
                    }
                },
            },
            "$libBrowserHostFn": libBrowserHost,
            "$BROWSER_HOST__postset": "BROWSER_HOST.selfInitialize();",
        };

        // this executes the function at compile time in order to capture export names
        const exports = libBrowserHost({});
        let commonDeps = ["$libBrowserHostFn", "$DOTNET", "$DOTNET_INTEROP", "$ENV"];
        let assignExportsBuilder = "";
        for (const exportName of Reflect.ownKeys(exports)) {
            const name = String(exportName);
            if (name === "cross") continue;
            if (name === "initialize") continue;
            lib[name] = () => "dummy";
            assignExportsBuilder += `_${String(name)} = exports.${String(name)};\n`;
        }
        lib.$BROWSER_HOST.assignExports = new Function("exports", assignExportsBuilder);
        lib["$BROWSER_HOST__deps"] = commonDeps;

        autoAddDeps(lib, "$BROWSER_HOST");
        addToLibrary(lib);
    }
    libFactory();
    return exports;
})({});
