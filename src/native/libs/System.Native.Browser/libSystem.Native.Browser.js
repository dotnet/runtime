//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//! This is generated file, see src/native/libs/Browser/rollup.config.defines.js


var libSystemNativeBrowserJS = (function (exports) {
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
    const loadedAssemblies = {};

    var commonInfra = /*#__PURE__*/Object.freeze({
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
        loadedAssemblies: loadedAssemblies,
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
    function SystemJS_RandomBytes(bufferPtr, bufferLength) {
        // batchedQuotaMax is the max number of bytes as specified by the api spec.
        // If the byteLength of array is greater than 65536, throw a QuotaExceededError and terminate the algorithm.
        // https://www.w3.org/TR/WebCryptoAPI/#Crypto-method-getRandomValues
        const batchedQuotaMax = 65536;
        if (!globalThis.crypto || !globalThis.crypto.getRandomValues) {
            if (!SystemJS_RandomBytes["cryptoWarnOnce"]) {
                Logger.warn("This engine doesn't support crypto.getRandomValues. Please use a modern version or provide polyfill for 'globalThis.crypto.getRandomValues'.");
                SystemJS_RandomBytes["cryptoWarnOnce"] = true;
            }
            return -1;
        }
        const memoryView = runtimeApi.localHeapViewU8();
        const targetView = memoryView.subarray(bufferPtr, bufferPtr + bufferLength);
        // When threading is enabled, Chrome doesn't want SharedArrayBuffer to be passed to crypto APIs
        const needsCopy = nativeExports.isSharedArrayBuffer(memoryView.buffer);
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

    var trimmableExports = /*#__PURE__*/Object.freeze({
        __proto__: null,
        SystemJS_RandomBytes: SystemJS_RandomBytes
    });

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    function registerDllBytes(bytes, asset) {
        const sp = Module.stackSave();
        try {
            const sizeOfPtr = 4;
            const ptrPtr = Module.stackAlloc(sizeOfPtr);
            if (Module._posix_memalign(ptrPtr, 16, bytes.length)) {
                throw new Error("posix_memalign failed");
            }
            const ptr = Module.HEAPU32[ptrPtr >> 2];
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
        if (!assembly) {
            return false;
        }
        Module.HEAPU32[outDataStartPtr >> 2] = assembly.ptr;
        // upper bits are cleared by the C caller
        Module.HEAPU32[outSize >> 2] = assembly.length;
        return true;
    }
    browserHostExternalAssemblyProbe["__deps"] = ["loadedAssemblies"];
    function browserHostResolveMain(exitCode) {
        loaderExports.browserHostResolveMain(exitCode);
    }
    function browserHostRejectMain(reason) {
        loaderExports.browserHostRejectMain(reason);
    }
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
    //setEnvironmentVariable["__deps"] = ["setenv"];

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
    async function initialize(internals) {
        const runtimeApiFunctions = {
            runMain,
            runMainAndExit,
            setEnvironmentVariable,
            exit,
            setHeapB32, setHeapB8, setHeapU8, setHeapU16, setHeapU32, setHeapI8, setHeapI16, setHeapI32, setHeapI52, setHeapU52, setHeapI64Big, setHeapF32, setHeapF64,
            getHeapB32, getHeapB8, getHeapU8, getHeapU16, getHeapU32, getHeapI8, getHeapI16, getHeapI32, getHeapI52, getHeapU52, getHeapI64Big, getHeapF32, getHeapF64,
            localHeapViewI8, localHeapViewI16, localHeapViewI32, localHeapViewI64Big, localHeapViewU8, localHeapViewU16, localHeapViewU32, localHeapViewF32, localHeapViewF64,
        };
        const nativeExports = {
            registerDllBytes,
            isSharedArrayBuffer
        };
        setInternals(internals);
        Object.assign(internals.runtimeApi, runtimeApiFunctions);
        Object.assign(nativeExports, nativeExports);
        internals.nativeExportsTable = [...nativeExportsToTable(nativeExports)];
        internals.updates.push(updateInternalsImpl);
        updateInternals();
    }

    var exchange = /*#__PURE__*/Object.freeze({
        __proto__: null,
        _zero_region: _zero_region,
        assert_int_in_range: assert_int_in_range,
        browserHostExternalAssemblyProbe: browserHostExternalAssemblyProbe,
        browserHostRejectMain: browserHostRejectMain,
        browserHostResolveMain: browserHostResolveMain,
        copyBytes: copyBytes,
        exit: exit,
        getHeapB32: getHeapB32,
        getHeapB8: getHeapB8,
        getHeapF32: getHeapF32,
        getHeapF64: getHeapF64,
        getHeapI16: getHeapI16,
        getHeapI16_local: getHeapI16_local,
        getHeapI32: getHeapI32,
        getHeapI32_local: getHeapI32_local,
        getHeapI52: getHeapI52,
        getHeapI64Big: getHeapI64Big,
        getHeapI8: getHeapI8,
        getHeapU16: getHeapU16,
        getHeapU16_local: getHeapU16_local,
        getHeapU32: getHeapU32,
        getHeapU32_local: getHeapU32_local,
        getHeapU52: getHeapU52,
        getHeapU8: getHeapU8,
        initialize: initialize,
        isSharedArrayBuffer: isSharedArrayBuffer,
        localHeapViewF32: localHeapViewF32,
        localHeapViewF64: localHeapViewF64,
        localHeapViewI16: localHeapViewI16,
        localHeapViewI32: localHeapViewI32,
        localHeapViewI64Big: localHeapViewI64Big,
        localHeapViewI8: localHeapViewI8,
        localHeapViewU16: localHeapViewU16,
        localHeapViewU32: localHeapViewU32,
        localHeapViewU8: localHeapViewU8,
        max_int64_big: max_int64_big,
        min_int64_big: min_int64_big,
        registerDllBytes: registerDllBytes,
        runMain: runMain,
        runMainAndExit: runMainAndExit,
        setEnvironmentVariable: setEnvironmentVariable,
        setHeapB32: setHeapB32,
        setHeapB8: setHeapB8,
        setHeapF32: setHeapF32,
        setHeapF64: setHeapF64,
        setHeapI16: setHeapI16,
        setHeapI32: setHeapI32,
        setHeapI32_unchecked: setHeapI32_unchecked,
        setHeapI52: setHeapI52,
        setHeapI64Big: setHeapI64Big,
        setHeapI8: setHeapI8,
        setHeapU16: setHeapU16,
        setHeapU16_local: setHeapU16_local,
        setHeapU16_unchecked: setHeapU16_unchecked,
        setHeapU32: setHeapU32,
        setHeapU32_unchecked: setHeapU32_unchecked,
        setHeapU52: setHeapU52,
        setHeapU8: setHeapU8,
        sharedArrayBufferDefined: sharedArrayBufferDefined
    });

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    /**
     * This is root of **Emscripten library** that would become part of `dotnet.native.js`
     * It implements PAL for the VM/runtime.
     * It also implements part of public JS API related to memory and runtime hosting.
     */
    // Exports that can be trimmed away by emscripten linker if not used.
    function dotnetLibFactory() {
        // Symbols that would be protected from emscripten linker
        const commonInfraTransformed = {
            Assert: Assert,
            JSEngine: JSEngine,
            Logger: Logger,
            Module: Module,
            runtimeApi: runtimeApi,
            runtimeExports: runtimeExports,
            nativeExports: nativeExports,
            interopExports: interopExports,
            loaderExports: loaderExports,
            loadedAssemblies: loadedAssemblies,
            loaderExportsFromTable: loaderExportsFromTable,
            runtimeExportsFromTable: runtimeExportsFromTable,
            nativeExportsFromTable: nativeExportsFromTable,
            interopExportsFromTable: interopExportsFromTable,
            nativeExportsToTable: nativeExportsToTable,
            interopExportsToTable: interopExportsToTable,
            setInternals: setInternals,
            getInternals: getInternals,
            updateInternals: updateInternals,
            updateInternalsImpl: updateInternalsImpl,
        };
        // Symbols that would be protected from emscripten linker
        const exchangeTransformed = {
            setHeapB32: setHeapB32,
            setHeapB8: setHeapB8,
            setHeapU8: setHeapU8,
            setHeapU16: setHeapU16,
            setHeapU32: setHeapU32,
            setHeapI8: setHeapI8,
            setHeapI16: setHeapI16,
            setHeapI32: setHeapI32,
            setHeapI52: setHeapI52,
            setHeapU52: setHeapU52,
            setHeapI64Big: setHeapI64Big,
            setHeapF32: setHeapF32,
            setHeapF64: setHeapF64,
            getHeapB32: getHeapB32,
            getHeapB8: getHeapB8,
            getHeapU8: getHeapU8,
            getHeapU16: getHeapU16,
            getHeapU32: getHeapU32,
            getHeapI8: getHeapI8,
            getHeapI16: getHeapI16,
            getHeapI32: getHeapI32,
            getHeapI52: getHeapI52,
            getHeapU52: getHeapU52,
            getHeapI64Big: getHeapI64Big,
            getHeapF32: getHeapF32,
            getHeapF64: getHeapF64,
            localHeapViewI8: localHeapViewI8,
            localHeapViewI16: localHeapViewI16,
            localHeapViewI32: localHeapViewI32,
            localHeapViewI64Big: localHeapViewI64Big,
            localHeapViewU8: localHeapViewU8,
            localHeapViewU16: localHeapViewU16,
            localHeapViewU32: localHeapViewU32,
            localHeapViewF32: localHeapViewF32,
            localHeapViewF64: localHeapViewF64,
            isSharedArrayBuffer: isSharedArrayBuffer,
            exit: exit,
            runMain: runMain,
            runMainAndExit: runMainAndExit,
            setEnvironmentVariable: setEnvironmentVariable,
            registerDllBytes: registerDllBytes,
            browserHostExternalAssemblyProbe: browserHostExternalAssemblyProbe,
            browserHostRejectMain: browserHostRejectMain,
            browserHostResolveMain: browserHostResolveMain,
            _zero_region: _zero_region,
            assert_int_in_range: assert_int_in_range,
            max_int64_big: max_int64_big,
            min_int64_big: min_int64_big,
            sharedArrayBufferDefined: sharedArrayBufferDefined,
        };
        const moduleDeps = ["$ENV"];
        for (const exportName of Reflect.ownKeys(commonInfraTransformed)) {
            const emName = "$" + exportName.toString();
            commonInfraTransformed[emName] = commonInfra[exportName];
            moduleDeps.push(emName);
        }
        for (const exportName of Reflect.ownKeys(exchangeTransformed)) {
            const emName = "$" + exportName.toString();
            exchangeTransformed[emName] = exchange[exportName];
            moduleDeps.push(emName);
        }
        const trimmableDeps = {};
        for (const exportName of Reflect.ownKeys(trimmableExports)) {
            const emName = exportName.toString() + "__deps";
            const deps = trimmableExports[exportName]["__deps"];
            if (deps) {
                trimmableDeps[emName] = deps;
            }
        }
        const lib = {
            $DOTNET: {
                selfInitialize: () => {
                    if (typeof dotnetInternals !== "undefined") {
                        DOTNET.dotnetInternals = dotnetInternals;
                        DOTNET.initialize(dotnetInternals);
                        if (dotnetInternals.config && dotnetInternals.config.environmentVariables) {
                            Object.assign(ENV, dotnetInternals.config.environmentVariables);
                        }
                    }
                },
                initialize: initialize,
            },
            "$DOTNET__postset": "DOTNET.selfInitialize();",
            "$DOTNET__deps": moduleDeps,
            ...commonInfraTransformed,
            ...exchangeTransformed,
            ...trimmableExports,
            ...trimmableDeps
        };
        autoAddDeps(lib, "$DOTNET");
        addToLibrary(lib);
    }
    dotnetLibFactory();

    exports.commonInfra = commonInfra;
    exports.exchange = exchange;
    exports.trimmableExports = trimmableExports;

    return exports;

})({});
