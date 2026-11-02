// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from "node:assert/strict";
import test from "node:test";
import { createContext, SourceTextModule, SyntheticModule } from "node:vm";
import { Worker } from "node:worker_threads";
import { once } from "node:events";
import { readFile } from "node:fs/promises";
import ts from "typescript";
import { createWorkerModuleLoader, seedNativeRequestIds } from "./worker-transport-worker.mjs";

test("shared heap I53 reads preserve actual native conversion semantics at signed/unsigned boundaries", async () => {
    const buffer = new SharedArrayBuffer(64);
    const i64 = new BigInt64Array(buffer);
    const u32 = new Uint32Array(buffer);
    const i32 = new Int32Array(buffer);
    const load = createWorkerModuleLoader(createContext({}), {
        "./cross-module": { dotnetAssert: {}, dotnetLogger: {} },
        "../../Common/JavaScript/ems-ambient": { _ems_: {
            readI53FromI64: pointer => u32[pointer / 4] + i32[pointer / 4 + 1] * 4294967296,
            readI53FromU64: pointer => u32[pointer / 4] + u32[pointer / 4 + 1] * 4294967296,
        } },
    });
    const module = await load("../../../System.Native.Browser/utils/memory");
    await module.evaluate();
    const values = [-(2n ** 63n), -(2n ** 53n) - 1n, -1n, 0n, 1n, 2n ** 53n + 1n, 2n ** 63n - 1n];
    const expected = values.map(value => {
        i64[0] = value;
        return [module.namespace.getHeapI52(0), module.namespace.getHeapU52(0)];
    });
    module.namespace.installSharedMemoryViews(() => ({ i64 }));
    for (let index = 0; index < values.length; index++) {
        i64[0] = values[index];
        assert.deepEqual([module.namespace.getHeapI52(0), module.namespace.getHeapU52(0)], expected[index]);
    }
});

for (const callback of ["resource", "progress"]) {
    test(`unsupported ${callback} callback is rejected explicitly before spawning a native worker`, async () => {
        let configured = false;
        let spawned = 0;
        const callbacks = { configLoaded: async () => {
            configured = true;
            if (callback === "progress") callbacks.downloadResourceProgress = () => {};
        } };
        const load = createWorkerModuleLoader(createContext({ Worker: class {
            constructor() {
                spawned++; throw new Error("Worker must not start");
            }
        } }), {
            "../types": { InternalExchangeIndex: {} },
            "./bootstrap": { scriptUrl: "https://example.test/dotnet.js" },
            "./cross-module": { dotnetApi: {}, dotnetInternals: [], dotnetRuntimeExports: {}, dotnetLogger: {}, Module: {} },
            "./exit": { runtimeState: {} }, "./callbacks": { loaderCallbacks: callbacks },
            "./polyfills": { initPolyfillsLoader: async () => {} },
            "./assets": {
                hasCustomResourceLoader: () => configured && callback === "resource",
                loadDotnetModule() {}, loadJSModule() {}, callLibraryInitializerOnRuntimeConfigLoaded() {}, callLibraryInitializerOnRuntimeReady() {},
            },
        });
        const module = await load("./worker-proxy");
        await module.evaluate();
        await assert.rejects(module.namespace.createWorkerProxy({ resources: {} }), /resource loading\/progress callbacks/);
        assert.equal(configured, true);
        assert.equal(spawned, 0);
    });
}

test("native UTF16 error buffers are terminated even when the allocator returns dirty shared memory", async () => {
    const bytes = new Uint8Array(new SharedArrayBuffer(64));
    const words = new Uint16Array(bytes.buffer);
    const module = { _malloc: () => {
        bytes.fill(0xcc); return 8;
    } };
    const stubs = {
        "../utils/cross-module": { Module: module, dotnetApi: { localHeapViewU16: () => words } },
        "../../Common/JavaScript/ems-ambient": { _ems_: module },
        "./memory": {
            zeroRegion: (pointer, length) => bytes.fill(0, pointer, pointer + length),
            setU16Local: (view, pointer, value) => view[pointer / 2] = value,
            getU16Local: (view, pointer) => view[pointer / 2],
            arrayBufferNeedsCopy: () => true, viewOrCopy: (view, start, end) => view.slice(start, end),
        },
    };
    const load = createWorkerModuleLoader(createContext({}), stubs);
    const strings = await load("../../../System.Native.Browser/utils/strings");
    await strings.evaluate();
    assert.equal(strings.namespace.stringToUTF16Ptr("A\0B"), 8);
    assert.deepEqual(Array.from(words.subarray(4, 8)), [65, 0, 66, 0]);
});

test("thenable detection does not start user code or duplicate Task completion", async () => {
    const changed = await readFile(new URL("../libs/System.Runtime.InteropServices.JavaScript.Native/interop/cancelable-promise.ts", import.meta.url), "utf8");
    async function check(source) {
        const declaration = source.match(/export function isThenable[\s\S]*?\n\}/)?.[0];
        assert.ok(declaration);
        const output = ts.transpileModule(declaration, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext } }).outputText;
        const module = new SourceTextModule(output, { context: createContext({ Promise }) });
        await module.link(() => {
            throw new Error("Unexpected detector import");
        });
        await module.evaluate();
        let calls = 0;
        const value = { then(resolve) {
            calls++; resolve(42);
        } };
        assert.equal(module.namespace.isThenable(value), true);
        await Promise.resolve();
        return { calls, value, getCalls: () => calls };
    }
    const current = await check(changed);
    assert.equal(current.calls, 0);
    assert.equal(await Promise.resolve(current.value), 42);
    assert.equal(current.getCalls(), 1);
});

test("native-worker role initializes only forwarding callbacks without application handle ownership", async () => {
    const changed = await readFile(new URL("../libs/System.Runtime.InteropServices.JavaScript.Native/interop/index.ts", import.meta.url), "utf8");
    async function initialize(source) {
        const initialized = [];
        const forwarded = [];
        const state = {};
        const api = { INTERNAL: {}, runtimeBuildInfo: { gitHash: "proof" } };
        const context = createContext({ performance: { measure() {} } });
        const output = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext } }).outputText;
        const module = new SourceTextModule(output, { context });
        const syntax = ts.createSourceFile("index.ts", output, ts.ScriptTarget.ES2022, true);
        const imports = new Map();
        for (const statement of syntax.statements) {
            if (!ts.isImportDeclaration(statement)) continue;
            const names = statement.importClause?.namedBindings?.elements?.map(element => element.propertyName?.text ?? element.name.text) ?? ["default"];
            imports.set(statement.moduleSpecifier.text, names);
        }
        await module.link(specifier => {
            const names = imports.get(specifier);
            return new SyntheticModule(names, function () {
                for (const name of names) {
                    const value = name === "default" ? "proof"
                        : name === "InternalExchangeIndex" ? { RuntimeAPI: 0, RuntimeExportsTable: 4, WorkerRuntimeContext: 10 }
                            : name === "jsInteropState" ? state
                                : (...args) => {
                                    initialized.push(name);
                                    if (name === "getProxyCounts") return [0, 0, 0, 0, 0];
                                    return args;
                                };
                    this.setExport(name, value);
                }
            }, { context });
        });
        await module.evaluate();
        const internals = [api];
        internals[10] = { role: "worker", invoke: (...args) => {
            forwarded.push(args);
            return 42;
        } };
        module.namespace.dotnetInitializeModule(internals);
        return { api, initialized, forwarded, table: internals[4] };
    }
    const current = await initialize(changed);
    assert.equal(current.initialized.includes("initializeMarshalersToCs"), false);
    assert.equal(current.initialized.includes("initializeMarshalersToJs"), false);
    assert.equal(current.api.getAssemblyExports, undefined);
    assert.equal(current.api.INTERNAL.dynamicImport, undefined);
    assert.equal(current.table.length, 8);
    assert.equal(current.table[0](1024), 42);
    current.table[5](7, 2048);
    assert.deepEqual(current.forwarded, [[0, 1024], [5, 7, 2048]]);
});

async function fixture(options = {}) {
    const worker = new Worker(new URL("./worker-transport-worker.mjs", import.meta.url), {
        execArgv: ["--experimental-vm-modules"], workerData: options,
    });
    const [{ signalBuffer, nativeBuffer, memory }] = await once(worker, "message");
    const context = createContext({ Atomics, SharedArrayBuffer, Error, setTimeout, clearTimeout });
    const load = createWorkerModuleLoader(context, { "../types": {} }, (specifier, source) =>
        seedNativeRequestIds(specifier, source, options.seed));
    const module = await load("./worker-native");
    await module.evaluate();
    const signal = new Int32Array(signalBuffer);
    let callback = (operation, first, second) => first + second;
    const channel = module.namespace.createNativeChannel(signal, nativeBuffer, false, (...args) => callback(...args));
    const errors = [];
    channel.listen(error => errors.push(error));
    return {
        worker, memory, channel, signal, nativeBuffer, errors,
        setCallback: value => callback = value,
        stop: async () => {
            channel.stop(new Error("stopped"));
            worker.postMessage("stop");
            await once(worker, "exit");
        },
    };
}

test("native operations carry scalar parameters and existing shared frames, not application values", async () => {
    const f = await fixture();
    try {
        for (let index = 0; index < 65; index++) assert.equal(f.channel.invoke(10, index, 1), index + 1);
        assert.equal(f.channel.invoke(10, -2147483648, 1), -2147483647);
        const frame = 1024;
        const heap = new BigInt64Array(f.memory.buffer);
        heap[frame / 8] = 9007199254740993n;
        assert.equal(f.channel.invoke(15, frame), frame);
        assert.equal(heap[frame / 8], 9007199254740994n);
        assert.equal(f.channel.getStats().sent, 67);
        assert.deepEqual(f.errors, []);
    } finally {
        await f.stop();
    }
});

test("normal callback dispatch supports nested native calls and sibling queue reuse", async () => {
    const f = await fixture();
    try {
        f.setCallback((operation, first, second) => {
            if (operation === 10) {
                for (let index = 0; index < 65; index++) assert.equal(f.channel.invoke(10, index, 1), index + 1);
                return first + second;
            }
            return first === 0 ? 1 : f.channel.invoke(13, first - 1, second) + 1;
        });
        assert.equal(f.channel.invoke(11, 40, 2), 42);
        assert.equal(f.channel.invoke(13, 4, 0), 5);
        assert.equal(f.channel.getStats().dispatchDepth, 0);
    } finally {
        await f.stop();
    }
});

test("native capacity exhaustion preserves outer ownership and recovers", async () => {
    const f = await fixture();
    try {
        f.setCallback((_operation, depth) => depth === 0 ? 1 : f.channel.invoke(13, depth - 1) + 1);
        assert.equal(f.channel.invoke(13, 15), 16);
        assert.throws(() => f.channel.invoke(13, 16), /native call capacity exceeded/);
        assert.equal(f.channel.invoke(13, 4), 5);
        assert.equal(f.channel.invoke(10, 41, 1), 42);
    } finally {
        await f.stop();
    }
});

test("native and callback errors remain per-call and recover without handle leases", async () => {
    const f = await fixture();
    try {
        assert.throws(() => f.channel.invoke(12, 42), /Native failure 42/);
        f.setCallback(() => {
            throw new Error("UI callback failure");
        });
        assert.throws(() => f.channel.invoke(11), /UI callback failure/);
        assert.equal(f.channel.invoke(10, 41, 1), 42);
    } finally {
        await f.stop();
    }
});

for (const seed of [2147483646, -2]) {
    test(`native request ownership crosses signed identity rollover from ${seed}`, async () => {
        const f = await fixture({ seed });
        try {
            for (let index = 0; index < 20; index++) assert.equal(f.channel.invoke(11, index, 1), index + 1);
        } finally {
            await f.stop();
        }
    });
}

for (const queueSeed of [2147483646, 4294967294]) {
    test(`native dispatch counters cross rollover from ${queueSeed} without queue aliasing`, async () => {
        const f = await fixture({ queueSeed });
        try {
            const words = new Int32Array(f.nativeBuffer);
            for (let index = 0; index < 65; index++) assert.equal(f.channel.invoke(11, index, 1), index + 1);
            const expected = (queueSeed + 65) >>> 0;
            for (const base of [0, words.length / 2]) {
                assert.equal(Atomics.load(words, base) >>> 0, expected);
                assert.equal(Atomics.load(words, base + 1) >>> 0, expected);
            }
        } finally {
            await f.stop();
        }
    });
}

for (const nativeOperation of [2, 8]) {
    test(`native root operation ${nativeOperation} is serviced only after the worker leaves the initial import`, async () => {
        const f = await fixture();
        try {
            let resolve;
            const completed = new Promise(value => resolve = value);
            f.setCallback(operation => {
                assert.equal(operation, 12);
                queueMicrotask(() => resolve(f.channel.invoke(nativeOperation, 42)));
                return 0;
            });
            f.worker.postMessage("async-prefix");
            assert.equal(await completed, 42);
            assert.equal(f.channel.getStats().deferred, 0);
        } finally {
            await f.stop();
        }
    });
}

test("native completion preparation acknowledges cancellation before committing a Task", async () => {
    const f = await fixture();
    try {
        let cancelled = false;
        let prepareCount = 0;
        let resolve;
        const completed = new Promise(value => resolve = value);
        f.setCallback(operation => {
            if (operation === 13) cancelled = true;
            else {
                assert.equal(operation, 12);
                queueMicrotask(() => resolve(f.channel.invoke(2, 1024, 0, () => {
                    prepareCount++;
                    new Int32Array(f.memory.buffer)[1024 / 4] = cancelled ? -1 : 42;
                })));
            }
            return 0;
        });
        f.worker.postMessage("async-cancel-prefix");
        assert.equal(await completed, -1);
        assert.equal(prepareCount, 1);
        assert.equal(f.channel.invoke(10, 41, 1), 42);
    } finally {
        await f.stop();
    }
});

test("failed Task preparation waits for native acknowledgment before releasing its slot", async () => {
    const f = await fixture();
    try {
        let resolve;
        const completed = new Promise(value => resolve = value);
        f.setCallback(() => {
            queueMicrotask(() => {
                assert.throws(() => f.channel.invoke(2, 42, 0, () => {
                    throw new Error("preparation failure");
                }), /preparation failure/);
                resolve();
            });
            return 0;
        });
        f.worker.postMessage("async-prefix");
        await completed;
        assert.equal(f.channel.invoke(10, 41, 1), 42);
        assert.equal(f.channel.invoke(2, 43), 43);
    } finally {
        await f.stop();
    }
});

test("shared-memory growth preserves native frame contents and shutdown rejects calls", async () => {
    const f = await fixture();
    const before = f.memory.buffer;
    try {
        new Uint8Array(before)[1024] = 42;
        assert.equal(f.channel.invoke(14, 1), 2);
        assert.equal(f.memory.buffer.byteLength, before.byteLength + 65536);
        assert.equal(new Uint8Array(f.memory.buffer)[1024], 42);
    } finally {
        await f.stop();
    }
    assert.throws(() => f.channel.invoke(10), /stopped/);
});
