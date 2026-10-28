// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { readFile } from "node:fs/promises";
import { createContext, SourceTextModule, SyntheticModule } from "node:vm";
import { isMainThread, parentPort } from "node:worker_threads";
import ts from "typescript";

const sourceDirectory = new URL("../libs/Common/JavaScript/loader/", import.meta.url);

export function createWorkerModuleLoader(context, stubs, transform = (_specifier, source) => source) {
    const cache = new Map();
    async function getModule(specifier) {
        if (cache.has(specifier)) return cache.get(specifier);
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
        cache.set(specifier, module);
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
    const module = {
        _malloc: size => {
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
    function readPrimitive(pointer, type) {
        if (new Uint8Array(memory.buffer)[pointer + typeOffset] === 0) return null;
        if (type === primitiveTypes[0]) return new Uint8Array(memory.buffer)[pointer] !== 0;
        if (type === primitiveTypes[1]) return new Float64Array(memory.buffer)[pointer / 8];
        if (type === primitiveTypes[2]) return new Float32Array(memory.buffer)[pointer / 4];
        return new Int32Array(memory.buffer)[pointer / 4];
    }
    function writePrimitive(pointer, type, value) {
        new Uint8Array(memory.buffer)[pointer + typeOffset] = value === null || value === undefined ? 0 : type;
        if (value === null || value === undefined) return;
        if (type === primitiveTypes[0]) new Uint8Array(memory.buffer)[pointer] = value ? 1 : 0;
        else if (type === primitiveTypes[1]) new Float64Array(memory.buffer)[pointer / 8] = value;
        else if (type === primitiveTypes[2]) new Float32Array(memory.buffer)[pointer / 4] = value;
        else new Int32Array(memory.buffer)[pointer / 4] = value;
    }
    function invokeImport(value, isAsync = false, argumentType = int32Type, returnType = int32Type) {
        const pointer = module._malloc(3 * argumentSize);
        try {
            new Uint8Array(memory.buffer).fill(0, pointer, pointer + 3 * argumentSize);
            writePrimitive(pointer + 2 * argumentSize, argumentType === stringType ? int32Type : argumentType, argumentType === stringType ? 0 : value);
            const frame = new Uint8Array(memory.buffer, pointer + argumentSize, argumentSize);
            if (isAsync) frame.fill(0x5a);
            const result = imports.get("main.js").get("update")(pointer, isAsync, value, argumentType, returnType);
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
                    path: ["Sample", "Test", name], handle, supported: true, isAsync, argumentType, returnType,
                })),
                ...["Boolean", "Double", "Single"].flatMap((name, index) => ["", "Import", "Async", "AsyncImport"].map((suffix, mode) => ({
                    path: ["Sample", "Test", name + suffix], handle: 20 + index * 4 + mode,
                    supported: true, isAsync: mode >= 2, argumentType: primitiveTypes[index], returnType: primitiveTypes[index],
                }))),
            ],
            invokeWorkerJSExport: (handle, pointer, stringArgument) => {
                if (handle >= 20 && handle <= 31) {
                    const type = primitiveTypes[Math.floor((handle - 20) / 4)];
                    const mode = (handle - 20) % 4;
                    const value = readPrimitive(pointer + 2 * argumentSize, type) ?? (type === primitiveTypes[0] ? false : 0);
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
        AbortController, Atomics, Error, SharedArrayBuffer, setTimeout, clearTimeout,
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
    primitiveTypes = [types.MarshalerType.Boolean, types.MarshalerType.Double, types.MarshalerType.Single];
    parentPort.once("message", message => {
        if (message.type !== "init") throw new Error("Expected worker initialization");
        void workerModule.namespace.bootstrapWorkerRuntime(message);
    });
    parentPort.on("message", message => {
        if (message.type === "test-crash") throw new Error("M5 pending worker failure");
    });
}

if (!isMainThread) await runWorker();
