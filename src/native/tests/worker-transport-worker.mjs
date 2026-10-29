// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { readFile } from "node:fs/promises";
import { createContext, SourceTextModule, SyntheticModule } from "node:vm";
import { isMainThread, parentPort } from "node:worker_threads";
import ts from "typescript";

const sourceDirectory = new URL("../libs/Common/JavaScript/loader/", import.meta.url);

export function createWorkerModuleLoader(context, stubs, transform = (_specifier, source) => source) {
    const cache = new Map();
    function getModule(specifier) {
        if (cache.has(specifier)) return cache.get(specifier);
        const pending = loadModule(specifier);
        cache.set(specifier, pending);
        return pending;
    }
    async function loadModule(specifier) {
        let module;
        if (stubs[specifier]) {
            const exports = stubs[specifier];
            module = new SyntheticModule(Object.keys(exports), function () {
                for (const [key, value] of Object.entries(exports)) this.setExport(key, value);
            }, { context });
        } else {
            const source = await readFile(new URL(`${specifier}.ts`, sourceDirectory), "utf8");
            const output = ts.transpileModule(transform(specifier, source), {
                compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext },
            }).outputText;
            module = new SourceTextModule(output, { context, identifier: specifier });
        }
        return module;
    }
    return async function load(specifier) {
        const module = await getModule(specifier);
        if (module.status === "unlinked") await module.link(getModule);
        return module;
    };
}

async function runWorker() {
    const memory = new WebAssembly.Memory({ initial: 2, maximum: 64, shared: true });
    const imports = new Map();
    const allocations = new Map();
    const free = new Map();
    let nextPointer = 1024;
    let failNextAllocation = false;
    let allocationsUntilFailure = -1;
    const module = {
        _malloc: size => {
            if (allocationsUntilFailure === 0) {
                allocationsUntilFailure = -1;
                return 0;
            }
            if (allocationsUntilFailure > 0) allocationsUntilFailure--;
            if (failNextAllocation) {
                failNextAllocation = false;
                return 0;
            }
            const bytes = Math.ceil(size / 8) * 8;
            let pointer = free.get(bytes)?.pop();
            if (pointer === undefined) {
                pointer = nextPointer;
                nextPointer += bytes;
                if (nextPointer > memory.buffer.byteLength) {
                    memory.grow(Math.ceil((nextPointer - memory.buffer.byteLength) / 65536));
                }
            }
            allocations.set(pointer, bytes);
            return pointer;
        },
        _free: pointer => {
            const bytes = allocations.get(pointer);
            if (bytes === undefined) throw new Error("Unknown scratch allocation");
            allocations.delete(pointer);
            if (!free.has(bytes)) free.set(bytes, []);
            free.get(bytes).push(pointer);
        },
    };
    let argumentSize;
    let typeOffset;
    let int32Type;
    let stringType;
    let primitiveTypes;
    let bufferedTypes;
    let taskType;
    let voidCalls = 0;
    let lastVoidValue;
    function readPrimitive(pointer, type) {
        if (new Uint8Array(memory.buffer)[pointer + typeOffset] === 0) return null;
        if (type === primitiveTypes[0]) return new Uint8Array(memory.buffer)[pointer] !== 0;
        if (type === primitiveTypes[1]) return new Float64Array(memory.buffer)[pointer / 8];
        if (type === primitiveTypes[2]) return new Float32Array(memory.buffer)[pointer / 4];
        if (type === primitiveTypes[3]) return new Uint8Array(memory.buffer)[pointer];
        if (type === primitiveTypes[4]) return new Uint16Array(memory.buffer)[pointer / 2];
        if (type === primitiveTypes[5]) return new Int16Array(memory.buffer)[pointer / 2];
        if (type === primitiveTypes[6]) return new Float64Array(memory.buffer)[pointer / 8];
        if (type === primitiveTypes[7]) return new BigInt64Array(memory.buffer)[pointer / 8];
        if (type === primitiveTypes[8]) return new Uint32Array(memory.buffer)[pointer / 4];
        return new Int32Array(memory.buffer)[pointer / 4];
    }
    function writePrimitive(pointer, type, value) {
        new Uint8Array(memory.buffer)[pointer + typeOffset] = value === null || value === undefined ? 0 : type;
        if (value === null || value === undefined) return;
        if (type === primitiveTypes[0]) new Uint8Array(memory.buffer)[pointer] = value ? 1 : 0;
        else if (type === primitiveTypes[1]) new Float64Array(memory.buffer)[pointer / 8] = value;
        else if (type === primitiveTypes[2]) new Float32Array(memory.buffer)[pointer / 4] = value;
        else if (type === primitiveTypes[3]) new Uint8Array(memory.buffer)[pointer] = value;
        else if (type === primitiveTypes[4]) new Uint16Array(memory.buffer)[pointer / 2] = value;
        else if (type === primitiveTypes[5]) new Int16Array(memory.buffer)[pointer / 2] = value;
        else if (type === primitiveTypes[6]) new Float64Array(memory.buffer)[pointer / 8] = value;
        else if (type === primitiveTypes[7]) new BigInt64Array(memory.buffer)[pointer / 8] = BigInt(value);
        else if (type === primitiveTypes[8]) new Uint32Array(memory.buffer)[pointer / 4] = value;
        else new Int32Array(memory.buffer)[pointer / 4] = value;
    }
    function invokeImport(value, isAsync = false, argumentType = int32Type, returnType = int32Type) {
        return invokeImports(argumentType === 0 ? [] : [value], isAsync, argumentType === 0 ? [] : [argumentType], returnType);
    }
    function invokeImports(values, isAsync, argumentTypes, returnType) {
        const pointer = module._malloc((argumentTypes.length + 2) * argumentSize);
        try {
            new Uint8Array(memory.buffer).fill(0, pointer, pointer + (argumentTypes.length + 2) * argumentSize);
            for (let index = 0; index < argumentTypes.length; index++) {
                const buffered = argumentTypes[index] === stringType || bufferedTypes.includes(argumentTypes[index]) || argumentTypes[index] === taskType;
                writePrimitive(pointer + (index + 2) * argumentSize, buffered ? int32Type : argumentTypes[index], buffered ? 0 : values[index]);
            }
            const frame = new Uint8Array(memory.buffer, pointer + argumentSize, argumentSize);
            if (isAsync) frame.fill(0x5a);
            const result = imports.get("main.js").get("update")(pointer, isAsync, values, argumentTypes, returnType);
            if (isAsync && !frame.every(value => value === 0x5a)) throw new Error("Async import overwrote the native Task frame");
            return result;
        } finally {
            module._free(pointer);
        }
    }
    const runtime = {
        runtimeBuildInfo: {},
        INTERNAL: {
            registerWorkerJSImport: (moduleName, name, invoke) => {
                if (!imports.has(moduleName)) imports.set(moduleName, new Map());
                imports.get(moduleName).set(name, invoke);
            },
            getWorkerAssemblyExports: async () => [
                { path: ["Sample", "Test", "Scalar"], handle: 1, supported: true },
                { path: ["Sample", "Test", "ThroughImport"], handle: 2, supported: true },
                { path: ["Sample", "Test", "ActiveAllocations"], handle: 3, supported: true },
                { path: ["Sample", "Test", "Async"], handle: 4, supported: true, isAsync: true },
                { path: ["Sample", "Test", "AsyncImport"], handle: 5, supported: true, isAsync: true },
                { path: ["Sample", "Test", "AsyncPrefix"], handle: 6, supported: true, isAsync: true },
                { path: ["Sample", "Test", "FailAllocation"], handle: 14, supported: true },
                ...[
                    ["String", 7, false, stringType, stringType],
                    ["StringImport", 8, false, stringType, stringType],
                    ["AsyncString", 9, true, stringType, stringType],
                    ["AsyncStringImport", 10, true, stringType, stringType],
                    ["StringLength", 11, false, stringType, int32Type],
                    ["StringRepeat", 12, false, int32Type, stringType],
                    ["StringFail", 13, false, stringType, stringType],
                ].map(([name, handle, isAsync, argumentType, returnType]) => ({
                    path: ["Sample", "Test", name], handle, supported: true, isAsync, argumentTypes: [argumentType], returnType,
                })),
                ...["Boolean", "Double", "Single"].flatMap((name, index) => ["", "Import", "Async", "AsyncImport"].map((suffix, mode) => ({
                    path: ["Sample", "Test", name + suffix], handle: 20 + index * 4 + mode,
                    supported: true, isAsync: mode >= 2, argumentTypes: [primitiveTypes[index]], returnType: primitiveTypes[index],
                }))),
                ...["Byte", "Char", "Int16", "Int52", "BigInt64", "IntPtr"].flatMap((name, index) => ["", "Import", "Async", "AsyncImport"].map((suffix, mode) => ({
                    path: ["Sample", "Test", name + suffix], handle: 80 + index * 4 + mode,
                    supported: true, isAsync: mode >= 2, argumentTypes: [primitiveTypes[index + 3]], returnType: primitiveTypes[index + 3],
                }))),
                ...["DateTime", "DateTimeOffset", "Nullable", "Array", "Object", "JSObject", "Exception", "Function"].flatMap((name, index) =>
                    ["", "Import", "Async", "AsyncImport"].map((suffix, mode) => ({
                        path: ["Sample", "Test", name + suffix], handle: 120 + index * 4 + mode,
                        supported: true, isAsync: mode >= 2, argumentTypes: [bufferedTypes[index]], returnType: bufferedTypes[index],
                    }))),
                { path: ["Sample", "Test", "AwaitPromise"], handle: 200, supported: true, isAsync: true, argumentTypes: [taskType], returnType: int32Type },
                { path: ["Sample", "Test", "ImportPromise"], handle: 201, supported: true, isAsync: true, argumentTypes: [taskType], returnType: int32Type },
                ...[
                    ["NoArgument", 40, false, 0, int32Type],
                    ["NoArgumentAsync", 41, true, 0, int32Type],
                    ["Void", 42, false, 0, 1],
                    ["VoidArgument", 43, false, stringType, 1],
                    ["VoidAsync", 44, true, 0, 1],
                    ["VoidAsyncArgument", 45, true, stringType, 1],
                    ["VoidCalls", 46, false, 0, int32Type],
                    ["VoidLastValue", 47, false, 0, stringType],
                    ["VoidThrow", 48, false, 0, 1],
                    ["VoidAsyncFault", 49, true, 0, 1],
                    ["VoidAsyncNull", 50, true, 0, 1],
                ].map(([name, handle, isAsync, argumentType, returnType]) => ({
                    path: ["Sample", "Test", name], handle, supported: true, isAsync, argumentTypes: argumentType === 0 ? [] : [argumentType], returnType,
                })),
                ...[
                    ["Mixed", 60, false, [stringType, int32Type, primitiveTypes[1], primitiveTypes[0], primitiveTypes[2], stringType], stringType],
                    ["MixedImport", 61, false, [stringType, int32Type, primitiveTypes[1], primitiveTypes[0], primitiveTypes[2], stringType], stringType],
                    ["MixedAsync", 62, true, [stringType, int32Type, primitiveTypes[1], primitiveTypes[0], primitiveTypes[2], stringType], stringType],
                    ["MixedAsyncImport", 63, true, [stringType, int32Type, primitiveTypes[1], primitiveTypes[0], primitiveTypes[2], stringType], stringType],
                    ["MixedVoid", 64, false, [stringType, int32Type, primitiveTypes[1], primitiveTypes[0], primitiveTypes[2], stringType], 1],
                    ["SixStrings", 65, false, Array(6).fill(stringType), stringType],
                    ["SixStringsImport", 66, false, Array(6).fill(stringType), stringType],
                    ["UnsupportedSeven", 67, false, Array(7).fill(int32Type), int32Type],
                    ["NestedMultiple", 68, false, [stringType, int32Type, stringType], stringType],
                    ["FailAfterAllocations", 69, false, [int32Type], int32Type],
                    ["ImportAllocationFailure", 70, false, Array(6).fill(stringType), stringType],
                ].map(([name, handle, isAsync, argumentTypes, returnType]) => ({
                    path: ["Sample", "Test", name], handle, supported: handle !== 67, isAsync, argumentTypes, returnType,
                })),
            ].map(descriptor => ({ argumentTypes: [int32Type], returnType: int32Type, ...descriptor })),
            invokeWorkerJSExport: (handle, pointer, values) => {
                if (handle === 200) return Promise.resolve(values[0]);
                if (handle === 201) return invokeImports(values, true, [taskType], int32Type);
                if (handle >= 120 && handle < 152) {
                    const type = bufferedTypes[Math.floor((handle - 120) / 4)];
                    const mode = (handle - 120) % 4;
                    const result = mode % 2 ? invokeImport(values[0], mode >= 2, type, type) : values[0];
                    return mode >= 2 ? Promise.resolve(result) : result;
                }
                const stringArgument = values[0];
                if (handle >= 60 && handle <= 68) {
                    if (handle === 60 || handle === 65) return JSON.stringify(values);
                    if (handle === 62) return Promise.resolve(JSON.stringify(values));
                    const argumentTypes = handle === 66 ? Array(6).fill(stringType)
                        : handle === 68 ? [stringType, int32Type, stringType]
                            : [stringType, int32Type, primitiveTypes[1], primitiveTypes[0], primitiveTypes[2], stringType];
                    return invokeImports(values, handle === 63, argumentTypes, handle === 64 ? 1 : stringType);
                }
                if ((handle >= 20 && handle <= 31) || (handle >= 80 && handle <= 103)) {
                    const index = handle >= 80 ? Math.floor((handle - 80) / 4) + 3 : Math.floor((handle - 20) / 4);
                    const type = primitiveTypes[index];
                    const mode = (handle - (handle >= 80 ? 80 : 20)) % 4;
                    const value = readPrimitive(pointer + 2 * argumentSize, type) ?? (type === primitiveTypes[0] ? false : type === primitiveTypes[7] ? 0n : 0);
                    const result = mode % 2 ? invokeImport(value, mode >= 2, type, type) : value;
                    if (mode >= 2) return Promise.resolve(result);
                    writePrimitive(pointer + argumentSize, type, result);
                    return;
                }
                const value = new Int32Array(memory.buffer)[(pointer + 2 * argumentSize) / 4];
                let result;
                if (handle === 1) {
                    if (value === -1) throw new Error("Inner managed export failure");
                    result = value + 1;
                } else if (handle === 2) {
                    result = invokeImport(value);
                } else if (handle === 3) {
                    result = allocations.size;
                } else if (handle === 4) {
                    if (value === -1) throw new Error("Synchronous Task export failure");
                    if (value === -2) return null;
                    if (value === -3) return Promise.reject(new Error("Asynchronous Task export failure"));
                    if (value === -4) return new Promise(() => {});
                    return new Promise(resolve => setTimeout(() => resolve(value + 1), 5));
                } else if (handle === 5) {
                    const promise = invokeImport(value, true);
                    return promise === null ? null : Promise.resolve(promise);
                } else if (handle === 6) {
                    return Promise.resolve(invokeImport(value));
                } else if (handle === 7) {
                    return stringArgument;
                } else if (handle === 8) {
                    return invokeImport(stringArgument, false, stringType, stringType);
                } else if (handle === 9) {
                    return Promise.resolve(stringArgument);
                } else if (handle === 10) {
                    return invokeImport(stringArgument, true, stringType, stringType);
                } else if (handle === 11) {
                    result = stringArgument?.length ?? -1;
                } else if (handle === 12) {
                    return "x".repeat(value);
                } else if (handle === 13) {
                    throw new Error("String managed failure");
                } else if (handle === 14) {
                    failNextAllocation = true;
                    result = 0;
                } else if (handle === 40 || handle === 41) {
                    const response = invokeImport(undefined, handle === 41, 0, int32Type);
                    if (handle === 41) return response;
                    result = response;
                } else if (handle >= 42 && handle <= 45) {
                    voidCalls++;
                    lastVoidValue = handle % 2 ? stringArgument : "zero arguments";
                    return invokeImport(stringArgument, handle >= 44, handle % 2 ? stringType : 0, 1);
                } else if (handle === 46) {
                    result = voidCalls;
                } else if (handle === 47) {
                    return lastVoidValue;
                } else if (handle === 48) {
                    throw new Error("Synchronous void failure");
                } else if (handle === 49) {
                    return Promise.reject(new Error("Asynchronous void failure"));
                } else if (handle === 50) {
                    return null;
                } else if (handle === 69) {
                    allocationsUntilFailure = value;
                    result = 0;
                } else if (handle === 70) {
                    allocationsUntilFailure = 2;
                    return invokeImports(values, false, Array(6).fill(stringType), stringType);
                } else {
                    throw new Error("Unknown test export");
                }
                new Int32Array(memory.buffer)[(pointer + argumentSize) / 4] = result;
                new Uint8Array(memory.buffer)[pointer + argumentSize + typeOffset] = int32Type;
            },
        },
        runMain: async (_assemblyName, args) => {
            try {
                return invokeImport(Number(args[0]));
            } catch (error) {
                if (args[1] === "catch-capacity" && /queue is full/.test(String(error))) return 77;
                throw error;
            }
        },
        runMainAndExit: async (...args) => runtime.runMain(...args),
        exit: code => {
            throw { status: code };
        },
    };
    const context = createContext({
        AbortController, Atomics, Error, TypeError, Date, Promise, Uint8Array, Int32Array, Float32Array, Float64Array, SharedArrayBuffer, setTimeout, clearTimeout,
        self: {
            addEventListener: (type, listener) => {
                if (type === "message") parentPort.on("message", data => listener({ data }));
            },
            postMessage: message => parentPort.postMessage(message),
            close: () => parentPort.close(),
        },
    });
    const load = createWorkerModuleLoader(context, {
        "../types": {},
        "./config": { mergeLoaderConfig: () => {} },
        "./host-builder": { HostBuilder: class {
            async create() {
                return runtime;
            }
        } },
        "./cross-module": { Module: module, dotnetNativeBrowserExports: { getWasmMemory: () => memory } },
    });
    const workerModule = await load("./worker-runtime");
    await workerModule.evaluate();
    const types = (await load("../../../System.Runtime.InteropServices.JavaScript.Native/interop/types")).namespace;
    argumentSize = types.JavaScriptMarshalerArgSize;
    typeOffset = types.JSMarshalerArgumentOffsets.Type;
    int32Type = types.MarshalerType.Int32;
    stringType = types.MarshalerType.String;
    primitiveTypes = [types.MarshalerType.Boolean, types.MarshalerType.Double, types.MarshalerType.Single,
        types.MarshalerType.Byte, types.MarshalerType.Char, types.MarshalerType.Int16, types.MarshalerType.Int52, types.MarshalerType.BigInt64, types.MarshalerType.IntPtr];
    bufferedTypes = ["DateTime", "DateTimeOffset", "Nullable", "Array", "Object", "JSObject", "Exception", "Function"].map(name => types.MarshalerType[name]);
    taskType = types.MarshalerType.Task;
    parentPort.once("message", message => {
        if (message.type !== "init") throw new Error("Expected worker initialization");
        void workerModule.namespace.bootstrapWorkerRuntime(message);
    });
    parentPort.on("message", message => {
        if (message.type === "test-crash") throw new Error("M5 pending worker failure");
    });
}

if (!isMainThread) await runWorker();
