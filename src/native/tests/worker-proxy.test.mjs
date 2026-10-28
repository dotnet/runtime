// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import { createContext, SourceTextModule, SyntheticModule } from "node:vm";
import { Worker as NodeWorker } from "node:worker_threads";
import { createWorkerModuleLoader } from "./worker-transport-worker.mjs";

for (const worker of [false, true]) {
    test(`library harness selects ${worker ? "worker" : "ordinary"} runtime and bootstraps UI imports`, async () => {
        const calls = [];
        let environment;
        const runtime = {
            runtimeBuildInfo: { productVersion: "test", gitHash: "test" },
            Module: worker ? {} : { HEAPU8: new Uint8Array(64) },
            INTERNAL: { workerMemoryViews: { refreshIfNeeded: () => ({ u8: new Uint8Array(128) }) } },
            runMain: async (assembly, args) => {
                calls.push(["runMain", assembly, Array.from(args)]);
                return 0;
            },
            runMainAndExit: async (assembly, args) => {
                calls.push(["runMainAndExit", assembly, Array.from(args)]);
                return 0;
            },
            exit: code => calls.push(["runtimeExit", code]),
        };
        const builder = {
            withVirtualWorkingDirectory() {
                return this;
            },
            withEnvironmentVariables(value) {
                environment = value;
                return this;
            },
            withEnvironmentVariable() {
                return this;
            },
            withDiagnosticTracing() {
                return this;
            },
            withConfig() {
                return this;
            },
            withWorker() {
                calls.push(["withWorker"]);
                return this;
            },
            async create() {
                calls.push(["create"]);
                return runtime;
            },
        };
        const args = [
            ...(worker ? ["--with-worker"] : []), "--test-module=./test-bootstrap.mjs",
            "--run", "WasmTestRunner.dll", "Test.dll",
        ];
        const location = new URL("https://example.test/");
        for (const arg of args) location.searchParams.append("arg", arg);
        const context = createContext({
            URL, window: {}, location, navigator: { userAgent: "Chrome" },
            document: {
                createElement: () => ({}),
                body: { appendChild: element => {
                    assert.equal(element.id, "tests_done");
                    calls.push(["testDone", Number(element.textContent)]);
                } },
            },
            fetch: async () => ({ ok: true, json: async () => ({}) }),
            console: { log() {}, info() {}, debug() {} },
        });
        const dotnetModule = new SyntheticModule(["dotnet", "exit"], function () {
            this.setExport("dotnet", builder);
            this.setExport("exit", code => calls.push(["exit", code]));
        }, { context });
        const bootstrapModule = new SyntheticModule(["setup"], function () {
            this.setExport("setup", async value => {
                assert.equal(value, runtime);
                calls.push(["setup"]);
            });
        }, { context });
        await bootstrapModule.link(() => {
            throw new Error("Unexpected bootstrap import");
        });
        await bootstrapModule.evaluate();
        const source = await readFile(new URL("../../mono/browser/test-main.mjs", import.meta.url), "utf8");
        const harness = new SourceTextModule(source, {
            context,
            initializeImportMeta: meta => meta.url = "https://example.test/test-main.mjs",
            importModuleDynamically: async specifier => {
                assert.equal(specifier, "https://example.test/test-bootstrap.mjs");
                return bootstrapModule;
            },
        });
        await harness.link(specifier => {
            assert.equal(specifier, "./_framework/dotnet.js");
            return dotnetModule;
        });
        await harness.evaluate();
        assert.equal(environment.DOTNET_TEST_WITH_WORKER, worker ? "1" : "0");
        assert.equal(environment.IsBrowserDomSupported, worker ? "false" : "true");
        assert.deepEqual(calls, [
            ...(worker ? [["withWorker"]] : []), ["create"], ["setup"],
            [worker ? "runMainAndExit" : "runMain", "WasmTestRunner.dll", ["Test.dll"]],
            ...(worker ? [["runtimeExit", 0], ["testDone", 0]] : [["exit", 0]]),
        ]);
    });
}

async function createFixture(options = {}) {
    const workers = [];
    const calls = [];
    const errors = [];
    const workerListeners = [];
    const workerErrorListeners = [];
    const imports = new Map();
    let uiApi;
    let workerWake;
    const atomics = Object.create(Atomics);
    atomics.waitAsync = (array, index, expected) => index === 3
        ? { async: true, value: { then: callback => workerWake = callback } }
        : Atomics.waitAsync(array, index, expected);
    atomics.notify = (array, index) => {
        const result = Atomics.notify(array, index);
        if (index === 3 && workerWake) {
            const callback = workerWake;
            workerWake = undefined;
            callback();
        }
        return result;
    };
    atomics.wait = (array, index, expected) => {
        options.onWait?.();
        uiApi.INTERNAL.workerImports.processPending();
        assert.notEqual(Atomics.load(array, index), expected, "UI dispatch must wake the synchronous worker wait");
        return "not-equal";
    };
    const memory = new WebAssembly.Memory({ initial: 1, maximum: 4, shared: options.sharedMemory ?? true });
    const allocations = new Set();
    let nextPointer = 1024;
    const module = {
        _malloc: size => {
            const pointer = nextPointer;
            const end = pointer + Math.ceil(size / 8) * 8;
            if (end > memory.buffer.byteLength) {
                memory.grow(Math.ceil((end - memory.buffer.byteLength) / 65536));
            }
            nextPointer = end;
            allocations.add(pointer);
            return pointer;
        },
        _free: pointer => {
            assert.equal(allocations.delete(pointer), true);
        },
    };
    let terminated;
    const termination = new Promise(resolve => terminated = resolve);
    const runtime = {
        runtimeBuildInfo: {},
        INTERNAL: {
            getWorkerAssemblyExports: async assemblyName => {
                calls.push(["exports", assemblyName]);
                if (options.getExports) await options.getExports();
                return [
                    { path: ["Sample", "Test", "Scalar"], handle: 1, supported: true },
                    { path: ["Sample", "Test", "Scalar.123"], handle: 1, supported: true },
                    { path: ["Sample", "Test", "Async"], handle: 2, supported: false },
                ];
            },
            invokeWorkerJSExport: (handle, pointer) => {
                assert.equal(handle, 1);
                const heap = new Int32Array(memory.buffer);
                const argument = heap[(pointer + 64) / 4];
                const result = options.exportFn ? options.exportFn(argument, imports, pointer) : argument + 1;
                heap[(pointer + 32) / 4] = result;
                new Uint8Array(memory.buffer)[pointer + 32 + 12] = 7;
            },
            registerWorkerJSImport: (moduleName, functionName, invoke) => {
                if (!imports.has(moduleName)) imports.set(moduleName, new Map());
                imports.get(moduleName).set(functionName, invoke);
            },
        },
        runMain: async (...args) => {
            calls.push(["runMain", ...args]);
            return options.runMain ? await options.runMain(imports) : 42;
        },
        runMainAndExit: async (...args) => {
            calls.push(["runMainAndExit", ...args]);
            return options.runMainAndExit ? await options.runMainAndExit() : 17;
        },
        exit: (code, reason) => {
            calls.push(["exit", code, reason]);
            throw { status: code };
        },
    };
    let bootstrapWorkerRuntime;
    let layout;
    class ThreadWorker {
        constructor() {
            this.listeners = new Set();
            this.terminated = false;
            this.closed = false;
            workers.push(this);
            this.thread = new NodeWorker(new URL("./worker-transport-worker.mjs", import.meta.url), {
                execArgv: ["--experimental-vm-modules"],
            });
            this.thread.on("message", data => {
                if (data.type === "ready") {
                    this.signal = new Int32Array(data.signalBuffer);
                    this.watchdog = new NodeWorker(`
                        const {parentPort, workerData: d} = require("node:worker_threads");
                        setTimeout(() => {
                            const signal = new Int32Array(d.buffer);
                            const message = "Nested worker transport test timed out";
                            for (let i=0; i<message.length; i++) signal[d.error+1+i]=message.charCodeAt(i);
                            Atomics.store(signal,d.error,message.length);
                            Atomics.store(signal,d.state,3);
                            Atomics.add(signal,d.wakeup,1);
                            Atomics.notify(signal,d.wakeup);
                            parentPort.postMessage(message);
                        }, 10000);
                    `, {
                        eval: true, execArgv: [],
                        workerData: {
                            buffer: data.signalBuffer, state: layout.OFFSET_WORKER_STATE,
                            error: layout.OFFSET_WORKER_STATE - 1 - layout.JSIMPORT_ERROR_CAPACITY,
                            wakeup: layout.OFFSET_WAKEUP_MAIN,
                        },
                    });
                    this.watchdog.on("message", message => this.onerror?.({ message }));
                }
                const event = { data };
                this.onmessage?.(event);
                for (const listener of this.listeners) listener(event);
            });
            this.thread.on("error", error => this.onerror?.({ message: error.message }));
            this.thread.on("exit", () => {
                this.closed = true;
                if (!this.terminated) void Promise.resolve(this.watchdog?.terminate()).then(terminated);
            });
        }
        postMessage(message) {
            this.thread.postMessage(message);
        }
        addEventListener(_type, listener) {
            this.listeners.add(listener);
        }
        removeEventListener(_type, listener) {
            this.listeners.delete(listener);
        }
        terminate() {
            this.terminated = true;
            void Promise.all([this.thread.terminate(), this.watchdog?.terminate()]).then(terminated);
        }
    }
    const context = createContext({
        AbortController, Atomics: options.threaded ? Atomics : atomics, Error, SharedArrayBuffer, setTimeout, clearTimeout,
        self: {
            addEventListener: (type, listener) => {
                if (type === "message") workerListeners.push(listener);
                if (type === "error") workerErrorListeners.push(listener);
            },
            postMessage: data => {
                const message = structuredClone(data);
                if (message.type === "ready" && options.invalidReadyMemory) {
                    message.wasmMemory = options.invalidReadyMemory === "missing"
                        ? undefined
                        : new WebAssembly.Memory({ initial: 1 });
                }
                if (message.type === "ready" && options.invalidSignaling === "version") {
                    Atomics.store(new Int32Array(message.signalBuffer), 0, -1);
                }
                if (message.type === "ready" && options.invalidSignaling === "length") {
                    message.signalBuffer = new SharedArrayBuffer(4);
                }
                if (message.type === "ready" && options.invalidSignaling === "non-shared") {
                    message.signalBuffer = new ArrayBuffer(message.signalBuffer.byteLength);
                }
                if (message.type === "ready") workers[0].signal = new Int32Array(message.signalBuffer);
                queueMicrotask(() => {
                    const event = { data: message };
                    workers[0].onmessage?.(event);
                    for (const listener of workers[0].listeners) listener(event);
                });
            },
            close: () => workers[0].closed = true,
        },
        Worker: options.threaded ? ThreadWorker : class {
            constructor() {
                this.terminated = false;
                this.closed = false;
                this.listeners = new Set();
                workers.push(this);
            }
            addEventListener(_type, listener) {
                this.listeners.add(listener);
            }
            removeEventListener(_type, listener) {
                this.listeners.delete(listener);
            }
            postMessage(message) {
                const data = structuredClone(message);
                if (data.type === "init") {
                    if (options.startupError) throw new Error("Cannot send init");
                    if (!options.hangStartup) void bootstrapWorkerRuntime(data);
                } else {
                    const dispatch = () => {
                        if (!this.terminated) workerListeners.forEach(listener => listener({ data }));
                    };
                    if (options.delayExit && data.commandId === 2) {
                        setTimeout(dispatch, 10);
                    } else {
                        queueMicrotask(dispatch);
                    }
                }
            }
            terminate() {
                this.terminated = true;
                terminated();
            }
        },
    });

    const stubs = {
        "../types": {},
        "./bootstrap": { scriptUrl: "https://example.test/dotnet.js" },
        "./config": { mergeLoaderConfig: () => {} },
        "./host-builder": { HostBuilder: class { async create() { return runtime; } } },
        "./cross-module": {
            Module: module,
            dotnetNativeBrowserExports: { getWasmMemory: () => memory },
            dotnetLogger: { error: (...args) => errors.push(args) },
        },
    };

    const load = createWorkerModuleLoader(context, stubs, (specifier, source) => {
        if (specifier === "./shared-signaling" && options.seedPromiseIds !== undefined) {
            const marker = "let nextPromiseId = 1;";
            assert.ok(source.includes(marker), "Completion ID seed must match the implementation");
            return source.replace(marker, `let nextPromiseId = ${options.seedPromiseIds};`);
        }
        return source;
    });

    const workerModule = await load("./worker-runtime");
    await workerModule.evaluate();
    layout = (await load("./shared-signaling")).namespace;
    bootstrapWorkerRuntime = workerModule.namespace.bootstrapWorkerRuntime;
    const proxyModule = await load("./worker-proxy");
    await proxyModule.evaluate();
    return {
        workers, calls, errors, termination, memory, module, allocations, imports, layout,
        importTransport: (await load("./worker-imports")).namespace,
        promiseHelpers: (await load("./promise-completion-source")).namespace,
        promiseTransport: (await load("./worker-promises")).namespace,
        valueTransport: (await load("./worker-values")).namespace,
        failWorker: error => workerErrorListeners.forEach(listener => listener({ error, message: error.message })),
        create: async (config = { workerOptions: { timeoutMs: options.threaded ? 10000 : 1000 } }) => {
            uiApi = await proxyModule.namespace.createWorkerProxy(config);
            return uiApi;
        },
    };
}

test("worker exports return primitives synchronously and preserve aliases across ring wraparound", { timeout: 3000 }, async () => {
    const fixture = await createFixture();
    const api = await fixture.create();
    try {
        const exports = await api.getAssemblyExports("Test.dll");
        assert.equal(await api.getAssemblyExports("Test"), exports);
        const scalar = exports.Sample.Test.Scalar;
        assert.equal(exports.Sample.Test["Scalar.123"], scalar);
        const allocations = fixture.allocations.size;
        for (let i = 0; i < 65; i++) {
            const result = scalar(i);
            assert.equal(typeof result, "number");
            assert.equal(result, i + 1);
        }
        assert.equal(scalar(null), 1);
        assert.equal(scalar(undefined), 1);
        assert.equal(scalar(-2147483648), -2147483647);
        assert.equal(scalar(2147483646), 2147483647);
        assert.throws(() => scalar(2147483648), /Int32/);
        assert.throws(() => exports.Sample.Test.Async(1), /primitive.*String.*Task/);
        assert.equal(fixture.allocations.size, allocations);
        assert.equal(fixture.calls.filter(call => call[0] === "exports").length, 1);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("real worker threads support repeated nesting and many siblings in one outer call", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    let exports;
    let record = false;
    const events = [];
    try {
        api.setModuleImports("main.js", {
            update: value => {
                if (record) {
                    events.push(`import:${value}`);
                    Promise.resolve().then(() => events.push(`microtask:${value}`));
                }
                if (value === 99) {
                    let sum = 0;
                    for (let i = 0; i < 65; i++) sum += exports.Scalar(i);
                    return sum;
                }
                return value === 0 ? 1 : exports.ThroughImport(value - 1) + 1;
            },
        });

        exports = (await api.getAssemblyExports("Test")).Sample.Test;
        for (let i = 0; i < 32; i++) assert.equal(exports.ThroughImport(4), 5);
        assert.equal(exports.ThroughImport(99), 2145);
        record = true;
        events.push("before");
        assert.equal(exports.ThroughImport(3), 4);
        events.push("after");
        assert.deepEqual(events, ["before", "import:3", "import:2", "import:1", "import:0", "after"]);
        record = false;
        await Promise.resolve();
        assert.deepEqual(events.slice(-4), ["microtask:3", "microtask:2", "microtask:1", "microtask:0"]);
        assert.equal(exports.ActiveAllocations(0), 16);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("an import completed during the nested pump does not park afterward", { timeout: 3000 }, async () => {
    const fixture = await createFixture({
        onWait: () => {
            throw new Error("Unexpected wait after completion");
        },
    });
    const api = await fixture.create();
    const pointer = fixture.module._malloc(3 * 32);
    try {
        api.setModuleImports("main.js", { update: value => value + 1 });
        new Int32Array(fixture.memory.buffer)[(pointer + 64) / 4] = 41;
        new Uint8Array(fixture.memory.buffer)[pointer + 64 + 12] = 7;
        const invoke = fixture.importTransport.createWorkerJSImportInvoker(
            fixture.workers[0].signal, api.INTERNAL.workerMemoryViews, 1, api.INTERNAL.workerImports.processPending);
        assert.equal(invoke(pointer), 42);
    } finally {
        fixture.module._free(pointer);
        api.exit(0);
        await fixture.termination;
    }
});

test("real worker threads isolate nested errors and recover after both capacity limits", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    let exports;
    let failure = "none";
    try {
        api.setModuleImports("main.js", {
            update: value => {
                if (failure !== "none" && value === 2) throw new Error("Inner UI import failure \u03bb");
                if (failure === "recover" && value === 3) {
                    let first;
                    try {
                        exports.ThroughImport(2);
                    } catch (error) {
                        first = error;
                    }
                    assert.match(first.message, /Inner UI import failure/);
                    assert.throws(() => exports.Scalar(-1), /Inner managed export failure/);
                    assert.match(first.message, /Inner UI import failure/);
                    return 90;
                }
                return value === 0 ? 1 : exports.ThroughImport(value - 1) + 1;
            },
        });
        exports = (await api.getAssemblyExports("Test")).Sample.Test;
        failure = "propagate";
        assert.throws(() => exports.ThroughImport(4), /Inner UI import failure \u03bb/);
        failure = "recover";
        assert.equal(exports.ThroughImport(4), 91);
        failure = "none";
        assert.equal(exports.ThroughImport(15), 16);
        assert.throws(() => exports.ThroughImport(16), /export queue is full/);
        assert.equal(exports.ActiveAllocations(0), 16);
        assert.equal(exports.ThroughImport(4), 5);
        assert.equal(await api.runMain("Test", ["16", "catch-capacity"]), 77);
        assert.equal(exports.ActiveAllocations(0), 16);
        assert.equal(await api.runMain("Test", ["3"]), 4);
        assert.equal(exports.ThroughImport(4), 5);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

for (const invalidSignaling of ["version", "length", "non-shared"]) {
    test(`incompatible ${invalidSignaling} signaling rejects startup`, { timeout: 3000 }, async () => {
        const fixture = await createFixture({ invalidSignaling });
        let api;
        try {
            await assert.rejects(fixture.create().then(result => {
                api = result;
                return result;
            }), /signaling protocol/);
            assert.equal(fixture.workers[0].terminated, true);
        } finally {
            if (api) {
                api.exit(0);
                await fixture.termination;
            }
        }
    });
}

test("worker export errors reclaim frames and permit the next call", { timeout: 3000 }, async () => {
    const fixture = await createFixture({
        exportFn: value => {
            if (value === -1) throw new Error("Managed export failure \u03bb");
            return value + 1;
        },
    });
    const api = await fixture.create();
    try {
        const { Scalar } = (await api.getAssemblyExports("Test")).Sample.Test;
        assert.throws(() => Scalar(-1), /Managed export failure \u03bb/);
        assert.equal(Scalar(42), 43);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("export import servicing preserves microtask ordering during nested exports", { timeout: 30000 }, async () => {
    const events = [];
    let scalar;
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    try {
        api.setModuleImports("main.js", {
            update: value => {
                events.push("import");
                assert.equal(scalar(0), 1);
                Promise.resolve().then(() => events.push("microtask"));
                return value + 1;
            },
        });
        const exports = (await api.getAssemblyExports("Test")).Sample.Test;
        scalar = exports.Scalar;
        events.push("before");
        assert.equal(exports.ThroughImport(41), 42);
        events.push("after");
        assert.deepEqual(events, ["before", "import", "after"]);
        await Promise.resolve();
        assert.deepEqual(events, ["before", "import", "after", "microtask"]);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("failed export discovery can be retried", { timeout: 3000 }, async () => {
    let fail = true;
    const fixture = await createFixture({ getExports: () => { if (fail) throw new Error("Export discovery failed"); } });
    const api = await fixture.create();
    try {
        await assert.rejects(api.getAssemblyExports("Test"), /Export discovery failed/);
        fail = false;
        assert.equal((await api.getAssemblyExports("Test")).Sample.Test.Scalar(1), 2);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("exports called from a runMain import complete synchronously", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    let scalar;
    const api = await fixture.create();
    try {
        api.setModuleImports("main.js", {
            update: () => {
                assert.equal(scalar(1), 2);
                return 42;
            },
        });
        scalar = (await api.getAssemblyExports("Test")).Sample.Test.Scalar;
        assert.equal(await api.runMain("Test", ["41"]), 42);
        assert.equal(scalar(1), 2);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("runMainAndExit permits nested exports until Main completes", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    let scalar;
    try {
        api.setModuleImports("main.js", { update: value => scalar(value) });
        scalar = (await api.getAssemblyExports("Test")).Sample.Test.Scalar;
        assert.equal(await api.runMainAndExit("Test", ["41"]), 42);
        assert.throws(() => scalar(1), /exited/);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("string transport preserves UTF16, nulls and mixed signatures without retaining native allocations", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    try {
        const exports = (await api.getAssemblyExports("Test")).Sample.Test;
        const values = [null, undefined, "", "hello", "I\u0131\u0130i", "a\0b", "\ud800", "\udfff", "a\ud83d\ude00z", "x".repeat(400000)];
        for (const value of values) {
            assert.equal(exports.String(value), value ?? null);
            assert.equal(exports.StringLength(value), value?.length ?? -1);
            assert.equal(await exports.AsyncString(value), value ?? null);
            assert.equal(exports.ActiveAllocations(0), 16);
        }
        assert.equal(exports.StringRepeat(400000), "x".repeat(400000));
        assert.throws(() => exports.String({}), /String/);
        assert.throws(() => exports.StringFail("failure"), /String managed failure/);
        assert.equal(exports.String("recovered"), "recovered");
        assert.equal(exports.ActiveAllocations(0), 16);
        assert.ok(fixture.workers[0].signal[fixture.layout.OFFSET_MEMORY_GENERATION] > 0);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("UI string results allocate on the worker, support nesting and release frames before asynchronous completion", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    let exports;
    let mode = "echo";
    const deferred = [];
    try {
        api.setModuleImports("main.js", { update: value => {
            if (mode === "async") return new Promise((resolve, reject) => deferred.push({ resolve, reject, value }));
            if (mode === "invalid") return 42;
            if (mode === "throw") throw new Error("UI string failure");
            if (mode === "nested" && value?.length) return exports.StringImport(value.slice(1)) + value[0];
            if (mode === "large") return "z".repeat(400000);
            if (mode === "undefined") return undefined;
            if (mode === "allocationFailure") {
                exports.FailAllocation(0);
                return "worker must allocate this";
            }
            return exports.String(value);
        } });
        exports = (await api.getAssemblyExports("Test")).Sample.Test;
        for (const value of [null, "", "a\0\ud800b", "c".repeat(400000)]) {
            assert.equal(exports.StringImport(value), value);
            assert.equal(exports.ActiveAllocations(0), 16);
        }
        mode = "nested";
        assert.equal(exports.StringImport("abcd"), "dcba");
        mode = "large";
        assert.equal(exports.StringImport("small"), "z".repeat(400000));
        mode = "undefined";
        assert.equal(exports.StringImport("value"), null);
        mode = "invalid";
        assert.throws(() => exports.StringImport("value"), /String/);
        mode = "throw";
        assert.throws(() => exports.StringImport("value"), /UI string failure/);
        mode = "allocationFailure";
        assert.throws(() => exports.StringImport("value"), /unable to allocate/);
        exports.FailAllocation(0);
        assert.throws(() => exports.String("value"), /unable to allocate/);
        assert.equal(exports.ActiveAllocations(0), 16);
        mode = "async";
        const tasks = Array.from({ length: 70 }, (_, index) => exports.AsyncStringImport("task:" + index));
        assert.equal(exports.ActiveAllocations(0), 16);
        const all = Promise.all(tasks.map(promise => promise.then(value => value, error => error.message)));
        for (const item of deferred.reverse()) {
            if (item.value === "task:33") item.reject(new Error("Async string failure"));
            else item.resolve(item.value + "\0\udfff");
        }
        assert.deepEqual(await all, Array.from({ length: 70 }, (_, index) => index === 33 ? "Async string failure" : "task:" + index + "\0\udfff"));
        mode = "echo";
        assert.equal(exports.StringImport("recovered"), "recovered");
        assert.equal(exports.ActiveAllocations(0), 16);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("string transport rejects invalid ranges and allocation failures without leaking ownership", async () => {
    const fixture = await createFixture();
    const api = await fixture.create();
    const transport = fixture.valueTransport;
    try {
        const initial = fixture.allocations.size;
        const memory = api.INTERNAL.workerMemoryViews;
        const pointer = transport.allocateWorkerString(3);
        transport.writeWorkerString(memory, pointer, "\ud800\0a");
        assert.equal(transport.readWorkerString(memory, pointer, 3), "\ud800\0a");
        transport.freeWorkerString(pointer);
        assert.equal(fixture.allocations.size, initial);
        assert.throws(() => transport.freeWorkerString(pointer), /not owned/);
        for (const [pointer, length] of [[1, 1], [0, 1], [4, -2], [65536, 1], [4, 0x80000000]]) {
            assert.throws(() => transport.readWorkerString(memory, pointer, length), /range/);
        }
        for (const length of [-1, 0.5, 0x80000000]) {
            assert.throws(() => transport.allocateWorkerString(length), /length/);
        }
        const malloc = fixture.module._malloc;
        fixture.module._malloc = () => 0;
        assert.throws(() => transport.allocateWorkerString(4), /unable to allocate/);
        fixture.module._malloc = malloc;
        assert.equal(fixture.allocations.size, initial);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("pending string Tasks release their buffers on exit and ignore late string completion", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    let resolve;
    api.setModuleImports("main.js", { update: () => new Promise(done => resolve = done) });
    const exports = (await api.getAssemblyExports("Test")).Sample.Test;
    const pending = exports.AsyncStringImport("pending\0\ud800");
    assert.equal(exports.ActiveAllocations(0), 16);
    const rejected = assert.rejects(pending, /exit/i);
    api.exit(0);
    await Promise.all([rejected, fixture.termination]);
    resolve("late");
    await new Promise(done => setTimeout(done, 10));
});

for (const name of ["Boolean", "Double", "Single"]) {
    test(`${name} preserves synchronous and Task values across both realms`, { timeout: 30000 }, async () => {
        const fixture = await createFixture({ threaded: true });
        const api = await fixture.create();
        let mode = "echo";
        try {
            api.setModuleImports("main.js", { update: value => {
                if (mode === "null") return null;
                if (mode === "invalid") return {};
                if (mode === "asyncInvalid") return Promise.resolve({});
                if (mode === "async") return Promise.resolve(value);
                return value;
            } });
            const exports = (await api.getAssemblyExports("Test")).Sample.Test;
            const values = name === "Boolean" ? [false, true, null, undefined]
                : [0, -0, 1, -1, Math.PI, NaN, Infinity, -Infinity, Number.MAX_VALUE, Number.MIN_VALUE, -Number.MIN_VALUE, 1 + 2 ** -24, null, undefined];
            for (const value of values) {
                const expected = value === null || value === undefined ? (name === "Boolean" ? false : 0)
                    : name === "Single" ? Math.fround(value) : value;
                mode = "echo";
                assert.ok(Object.is(exports[name](value), expected));
                assert.ok(Object.is(exports[name + "Import"](value), expected));
                assert.ok(Object.is(await exports[name + "Async"](value), expected));
                mode = "async";
                assert.ok(Object.is(await exports[name + "AsyncImport"](value), expected));
            }
            const message = name === "Boolean" ? /Boolean/ : /Number/;
            assert.throws(() => exports[name]("wrong"), message);
            mode = "invalid";
            assert.throws(() => exports[name + "Import"](name === "Boolean" ? false : 1), message);
            mode = "asyncInvalid";
            await assert.rejects(exports[name + "AsyncImport"](name === "Boolean" ? true : 1), message);
            mode = "echo";
            assert.ok(Object.is(exports[name + "Import"](name === "Boolean" ? true : -0), name === "Boolean" ? true : -0));
            assert.equal(exports.ActiveAllocations(0), 16);
        } finally {
            api.exit(0);
            await fixture.termination;
        }
    });
}

for (const [seed, expected] of [
    [2147483646, [2147483646, 2147483647, -2147483648, -2147483647]],
    [-2, [-2, -1, 0, 1]],
]) {
    test(`completion IDs stay signed across rollover from ${seed}`, { timeout: 3000 }, async () => {
        const fixture = await createFixture({ seedPromiseIds: seed });
        assert.deepEqual(Array.from({ length: 4 }, () => fixture.layout.allocatePromiseId()), expected);
    });
}

test("cancelled promises release asynchronous request ownership", { timeout: 3000 }, async () => {
    const fixture = await createFixture();
    const { getPromiseCompletionSource } = fixture.promiseHelpers;
    const promises = fixture.promiseTransport.createWorkerPromises();
    const id = promises.reserve();
    const promise = promises.promise(id);
    const rejected = assert.rejects(promise, /Cancelled/);
    getPromiseCompletionSource(promise).reject(new Error("Cancelled"));
    await rejected;
    assert.throws(() => promises.promise(id), /invalid asynchronous request/);
    promises.complete({ type: "async-import", requestId: id, result: 42 });
    promises.stop(new Error("Stopped"));
});

for (const seed of [2147483646, -2]) {
    test(`dispatch counters and completion IDs wrap without changing ownership from ${seed}`, { timeout: 3000 }, async () => {
        let fixture;
        fixture = await createFixture({
            seedPromiseIds: seed,
            runMain: imports => {
                const pointer = fixture.module._malloc(3 * 32);
                try {
                    for (let value = 0; value < 32; value++) {
                        new Int32Array(fixture.memory.buffer)[(pointer + 64) / 4] = value;
                        new Uint8Array(fixture.memory.buffer)[pointer + 64 + 12] = 7;
                        assert.equal(imports.get("main.js").get("update")(pointer), value + 1);
                    }
                } finally {
                    fixture.module._free(pointer);
                }
                return 0;
            },
        });
        const api = await fixture.create();
        try {
            api.setModuleImports("main.js", { update: value => value + 1 });
            const { Scalar } = (await api.getAssemblyExports("Test")).Sample.Test;
            const signal = fixture.workers[0].signal;
            const layout = fixture.layout;
            for (const offset of [layout.OFFSET_JSIMPORT_HEAD, layout.OFFSET_JSIMPORT_TAIL,
                layout.OFFSET_JSEXPORT_HEAD, layout.OFFSET_JSEXPORT_TAIL]) {
                Atomics.store(signal, offset, 0xffff_fffe);
            }
            for (let value = 0; value < 32; value++) assert.equal(Scalar(value), value + 1);
            assert.equal(await api.runMain(), 0);
            for (const offset of [layout.OFFSET_JSIMPORT_HEAD, layout.OFFSET_JSIMPORT_TAIL,
                layout.OFFSET_JSEXPORT_HEAD, layout.OFFSET_JSEXPORT_TAIL]) {
                assert.equal(Atomics.load(signal, offset) >>> 0, 30);
            }
        } finally {
            api.exit(0);
            await fixture.termination;
        }
    });
}

test("worker failure rejects pending export discovery", { timeout: 3000 }, async () => {
    const fixture = await createFixture({ getExports: () => new Promise(() => {}) });
    const api = await fixture.create();
    const discovering = api.getAssemblyExports("Test");
    fixture.workers[0].onerror({ message: "Export worker crashed" });
    await assert.rejects(discovering, /Export worker crashed/);
    assert.equal(fixture.workers[0].terminated, true);
    assert.equal(fixture.workers[0].listeners.size, 0);
});

test("shared worker failure is visible before the UI error event can run", { timeout: 3000 }, async () => {
    const fixture = await createFixture();
    const api = await fixture.create();
    try {
        const { Scalar } = (await api.getAssemblyExports("Test")).Sample.Test;
        fixture.failWorker(new Error("Synchronous worker failure"));
        assert.throws(() => Scalar(1), /Synchronous worker failure/);
        assert.equal(fixture.workers[0].terminated, true);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("oversized export errors are explicit and do not poison the next call", { timeout: 3000 }, async () => {
    const fixture = await createFixture({
        exportFn: value => {
            if (value === -1) throw new Error("x".repeat(5000));
            return value + 1;
        },
    });
    const api = await fixture.create();
    try {
        const { Scalar } = (await api.getAssemblyExports("Test")).Sample.Test;
        assert.throws(() => Scalar(-1), /managed export error message exceeds/);
        assert.equal(Scalar(42), 43);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("scalar UI imports reuse shared frames across ring wraparound", { timeout: 3000 }, async () => {
    let fixture;
    let invocations = 0;
    fixture = await createFixture({
        runMain: imports => {
            const invoke = imports.get("main.js").get("Test.update");
            const pointer = fixture.module._malloc(3 * 32);
            try {
                for (let value = 0; value < 65; value++) {
                    const heap = new Int32Array(fixture.memory.buffer);
                    heap[(pointer + 2 * 32) / 4] = value;
                    new Uint8Array(fixture.memory.buffer)[pointer + 64 + 12] = 7;
                    assert.equal(invoke(pointer), value + 1);
                    assert.equal(heap[(pointer + 32) / 4], value + 1);
                }
            } finally {
                fixture.module._free(pointer);
            }
            return 0;
        },
    });
    const api = await fixture.create();
    try {
        api.setModuleImports("main.js", {
            Test: {
                increment: 1,
                update(value) {
                    invocations++;
                    return value + this.increment;
                },
            },
        });
        assert.equal(await api.runMain(), 0);
        assert.equal(invocations, 65);
        assert.equal(fixture.imports.get("main.js").size, 1);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

for (const failure of ["exception", "long-exception", "async-result"]) {
    test(`UI import ${failure} reports an error and reclaims its slot`, { timeout: 3000 }, async () => {
        let fixture;
        let shouldFail = true;
        fixture = await createFixture({
            runMain: imports => {
                const invoke = imports.get("main.js").get("update");
                const pointer = fixture.module._malloc(3 * 32);
                try {
                    const expected = failure === "exception" ? /UI import failed \u03bb/
                        : failure === "long-exception" ? /error message exceeds/
                        : /Int32/;
                    assert.throws(() => invoke(pointer), expected);
                    shouldFail = false;
                    assert.equal(invoke(pointer), 42);
                } finally {
                    fixture.module._free(pointer);
                }
                return 0;
            },
        });
        const api = await fixture.create();
        try {
            api.setModuleImports("main.js", {
                update: () => {
                    if (!shouldFail) return 42;
                    if (failure === "async-result") return Promise.resolve(42);
                    throw new Error(failure === "long-exception" ? "x".repeat(5000) : "UI import failed \u03bb");
                },
            });
            assert.equal(await api.runMain(), 0);
        } finally {
            api.exit(0);
            await fixture.termination;
        }
    });
}

for (const result of [null, undefined]) {
    test(`UI import preserves the ST ${String(result)} result`, { timeout: 3000 }, async () => {
        let fixture;
        fixture = await createFixture({
            runMain: imports => {
                const pointer = fixture.module._malloc(3 * 32);
                try {
                    assert.equal(imports.get("main.js").get("update")(pointer), null);
                    assert.equal(new Uint8Array(fixture.memory.buffer)[pointer + 32 + 12], 0);
                } finally {
                    fixture.module._free(pointer);
                }
                return 0;
            },
        });
        const api = await fixture.create();
        try {
            api.setModuleImports("main.js", { update: () => result });
            assert.equal(await api.runMain(), 0);
        } finally {
            api.exit(0);
            await fixture.termination;
        }
    });
}

for (const grow of [false, true]) {
    test(`UI views share the worker WASM heap${grow ? " after growth" : ""}`, { timeout: 3000 }, async () => {
        const fixture = await createFixture();
        const api = await fixture.create();
        const scratch = fixture.module._malloc(4);
        let grownAllocation;
        try {
            const manager = api.INTERNAL.workerMemoryViews;
            assert.ok(manager, "The ready handshake must initialize internal UI memory views");
            const initialViews = manager.refreshIfNeeded();
            const initialLength = initialViews.u8.byteLength;
            new Uint32Array(fixture.memory.buffer)[scratch / 4] = 0x12345678;
            assert.equal(initialViews.u32[scratch / 4], 0x12345678);
            initialViews.u32[scratch / 4] = 0x24681357;
            assert.equal(new Uint32Array(fixture.memory.buffer)[scratch / 4], 0x24681357);

            if (grow) {
                grownAllocation = fixture.module._malloc(initialLength + 8);
                const grownOffset = grownAllocation + initialLength;
                new Uint32Array(fixture.memory.buffer)[grownOffset / 4] = 0x11223344;
                const refreshed = manager.refreshIfNeeded();
                assert.notEqual(refreshed, initialViews);
                assert.ok(refreshed.u8.byteLength > initialLength);
                assert.equal(initialViews.u8.byteLength, initialLength);
                assert.equal(refreshed.u32[scratch / 4], 0x24681357);
                assert.equal(refreshed.u32[grownOffset / 4], 0x11223344);
                refreshed.u32[grownOffset / 4] = 0x55667788;
                assert.equal(new Uint32Array(fixture.memory.buffer)[grownOffset / 4], 0x55667788);
            } else {
                assert.equal(manager.refreshIfNeeded(), initialViews);
            }
            assert.throws(() => api.getHeapU8(scratch), /not supported/);
        } finally {
            if (grownAllocation !== undefined) fixture.module._free(grownAllocation);
            fixture.module._free(scratch);
            api.exit(0);
            await fixture.termination;
        }
        assert.equal(fixture.allocations.has(scratch), false);
        if (grownAllocation !== undefined) assert.equal(fixture.allocations.has(grownAllocation), false);
    });
}

for (const invalidReadyMemory of ["missing", "non-shared"]) {
    test(`a ready message with ${invalidReadyMemory} WASM memory rejects startup`, { timeout: 3000 }, async () => {
        const fixture = await createFixture({ invalidReadyMemory });
        await assert.rejects(fixture.create(), /WASM memory.*shared/);
        assert.equal(fixture.workers[0].terminated, true);
    });
}

test("non-shared worker WASM memory rejects startup before allocation", { timeout: 3000 }, async () => {
    const fixture = await createFixture({ sharedMemory: false });
    await assert.rejects(fixture.create(), /WASM memory.*shared/);
    assert.equal(fixture.allocations.size, 0);
    assert.equal(fixture.workers[0].terminated, true);
});

test("runMain forwards the assembly and arguments without exiting", { timeout: 3000 }, async () => {
    const fixture = await createFixture();
    const api = await fixture.create();
    assert.equal(await api.runMain("Other.dll", ["", "two words", "\u03bb"]), 42);
    assert.deepEqual(fixture.calls[0], ["runMain", "Other.dll", ["", "two words", "\u03bb"]]);
    assert.equal(fixture.workers[0].terminated, false);
    assert.equal(fixture.workers[0].closed, false);
    api.exit(0);
    await fixture.termination;
});

test("runMainAndExit forwards arguments and terminates the worker after completion", { timeout: 3000 }, async () => {
    const fixture = await createFixture();
    const api = await fixture.create();
    assert.equal(await api.runMainAndExit("App.dll", ["argument"]), 17);
    assert.deepEqual(fixture.calls[0], ["runMainAndExit", "App.dll", ["argument"]]);
    assert.equal(fixture.workers[0].terminated, true);
    await assert.rejects(api.runMain(), /exited/);
});

test("concurrent run commands are rejected without overwriting the first", { timeout: 3000 }, async () => {
    let complete;
    const fixture = await createFixture({ runMain: () => new Promise(resolve => complete = resolve) });
    const api = await fixture.create();
    const first = api.runMain();
    await assert.rejects(api.runMainAndExit(), /already running/);
    complete(9);
    assert.equal(await first, 9);
    assert.equal(fixture.workers[0].terminated, false);
    api.exit(0);
    await fixture.termination;
});

for (const method of ["runMain", "runMainAndExit"]) {
    test(`exit interrupts ${method} and runs runtime shutdown`, { timeout: 3000 }, async () => {
        const fixture = await createFixture({ [method]: () => new Promise(() => {}) });
        const api = await fixture.create();
        const running = api[method]();
        api.exit(7, "stop");
        await assert.rejects(running, /exited with code 7/);
        await fixture.termination;
        assert.deepEqual(fixture.calls.at(-1), ["exit", 7, "stop"]);
        assert.equal(fixture.workers[0].terminated, true);
        assert.equal(fixture.errors.length, 0);
    });
}

for (const failure of ["timeout", "postMessage", "error", "messageerror"]) {
    test(`startup ${failure} terminates the worker`, { timeout: 3000 }, async () => {
        const fixture = await createFixture({
            hangStartup: failure !== "postMessage",
            startupError: failure === "postMessage",
        });
        const creating = fixture.create({ workerOptions: { timeoutMs: 20 } });
        if (failure === "error") fixture.workers[0].onerror({ message: "Startup crashed" });
        if (failure === "messageerror") fixture.workers[0].onmessageerror();
        await assert.rejects(creating, /respond|Cannot send init|Startup crashed|deserialize/);
        assert.equal(fixture.workers[0].terminated, true);
    });
}

test("exit waits for its own acknowledgement when Main finishes concurrently", { timeout: 3000 }, async () => {
    const fixture = await createFixture({ delayExit: true });
    const api = await fixture.create();
    const running = api.runMain();
    api.exit(7);
    await assert.rejects(running, /exited with code 7/);
    await fixture.termination;
    assert.deepEqual(fixture.calls.at(-1), ["exit", 7, undefined]);
});

test("worker errors reject an in-flight command", { timeout: 3000 }, async () => {
    const fixture = await createFixture({ runMain: () => new Promise(() => {}) });
    const api = await fixture.create();
    const running = api.runMain();
    fixture.workers[0].onerror({ message: "Worker crashed" });
    await assert.rejects(running, /Worker crashed/);
    assert.equal(fixture.workers[0].terminated, true);
    await assert.rejects(api.runMain(), /Worker crashed/);
});

test("managed command failures preserve the error and terminate the worker", { timeout: 3000 }, async () => {
    const fixture = await createFixture({ runMain: async () => { throw new Error("Managed entry point failed"); } });
    const api = await fixture.create();
    await assert.rejects(api.runMain(), /Managed entry point failed/);
    assert.equal(fixture.workers[0].terminated, true);
});

test("scalar Task exports preserve their synchronous prefix, nulls, throws and asynchronous failures", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    const events = [];
    try {
        api.setModuleImports("main.js", { update: value => {
            events.push("import");
            queueMicrotask(() => events.push("microtask"));
            return value + 1;
        } });
        const exports = (await api.getAssemblyExports("Test")).Sample.Test;
        events.push("before");
        const result = exports.AsyncPrefix(41);
        assert.equal(typeof result.then, "function");
        events.push("after");
        assert.deepEqual(events, ["before", "import", "after"]);
        assert.equal(await result, 42);
        assert.deepEqual(events, ["before", "import", "after", "microtask"]);
        assert.equal(await exports.Async(17), 18);
        assert.equal(exports.Async(-2), null);
        assert.throws(() => exports.Async(-1), /Synchronous Task export failure/);
        assert.throws(() => exports.Async("1"), /Int32/);
        await assert.rejects(exports.Async(-3), /Asynchronous Task export failure/);
        assert.equal(exports.Scalar(9), 10);
        assert.equal(exports.ActiveAllocations(0), 16);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("asynchronous UI imports release frames across 70 outstanding Tasks and preserve nested synchronous prefixes", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    const deferred = [];
    let exports;
    try {
        api.setModuleImports("main.js", { update: value => {
            assert.equal(exports.Scalar(value), value + 1);
            return new Promise((resolve, reject) => deferred.push({ value, resolve, reject }));
        } });
        exports = (await api.getAssemblyExports("Test")).Sample.Test;
        const promises = [];
        for (let value = 0; value < 70; value++) {
            promises.push(exports.AsyncImport(value));
            assert.equal(deferred.length, value + 1, "Import prefix must finish before the export returns");
            for (let slot = 0; slot < 16; slot++) {
                const signal = fixture.workers[0].signal;
                assert.equal(Atomics.load(signal, fixture.layout.jsimportEntryOffset(slot)), 0);
                assert.equal(Atomics.load(signal, fixture.layout.jsexportEntryOffset(slot)), 0);
            }
        }
        const results = Promise.all(promises.map(promise => promise.then(value => value, error => error.message)));
        for (const request of deferred.reverse()) {
            if (request.value === 33) request.reject(new Error("Deferred UI failure"));
            else request.resolve(request.value + 10);
        }
        assert.deepEqual(await results, Array.from({ length: 70 }, (_, value) => value === 33 ? "Deferred UI failure" : value + 10));
        assert.equal(exports.ActiveAllocations(0), 16);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("asynchronous UI imports preserve nulls and errors and reject unsupported thenables", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    try {
        api.setModuleImports("main.js", { update: value => {
            if (value === 0) return null;
            if (value === 1) return undefined;
            if (value === 2) return 42;
            if (value === 3) throw new Error("Immediate UI failure");
            if (value === 4) return { then: resolve => {
                assert.equal(api.INTERNAL.workerImports.isDispatching(), true);
                resolve(45);
            } };
            if (value === 5) return { then: (_resolve, reject) => reject(new Error("Immediate thenable failure")) };
            if (value === 6) return Promise.resolve(2147483648);
            return { then: resolve => setTimeout(() => resolve(48), 1) };
        } });
        const exports = (await api.getAssemblyExports("Test")).Sample.Test;
        assert.equal(exports.AsyncImport(0), null);
        assert.equal(exports.AsyncImport(1), null);
        assert.throws(() => exports.AsyncImport(2), /Value is not a Promise/);
        assert.throws(() => exports.AsyncImport(3), /Immediate UI failure/);
        assert.throws(() => exports.AsyncImport(4), /Promise/);
        assert.throws(() => exports.AsyncImport(5), /Promise/);
        await assert.rejects(exports.AsyncImport(6), /Int32/);
        assert.throws(() => exports.AsyncImport(7), /Promise/);
        assert.equal(exports.ActiveAllocations(0), 16);
    } finally {
        api.exit(0);
        await fixture.termination;
    }
});

test("exit rejects pending Task exports and ignores late UI import completion", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    let resolve;
    api.setModuleImports("main.js", { update: () => new Promise(done => resolve = done) });
    const exports = (await api.getAssemblyExports("Test")).Sample.Test;
    const pending = exports.AsyncImport(1);
    const never = exports.Async(-4);
    const rejected = assert.rejects(pending, /exit/i);
    const neverRejected = assert.rejects(never, /exit/i);
    api.exit(0);
    await Promise.all([rejected, neverRejected, fixture.termination]);
    resolve(42);
    await new Promise(done => setTimeout(done, 10));
    assert.throws(() => exports.Async(1), /exit/i);
});

test("worker failure rejects pending Task exports and removes pending UI completions", { timeout: 30000 }, async () => {
    const fixture = await createFixture({ threaded: true });
    const api = await fixture.create();
    let reject;
    api.setModuleImports("main.js", { update: () => new Promise((_resolve, fail) => reject = fail) });
    const exports = (await api.getAssemblyExports("Test")).Sample.Test;
    const pending = assert.rejects(exports.AsyncImport(1), /M5 pending worker failure/);
    const never = assert.rejects(exports.Async(-4), /M5 pending worker failure/);
    fixture.workers[0].postMessage({ type: "test-crash" });
    await Promise.all([pending, never, fixture.termination]);
    reject(new Error("Late UI failure"));
    await new Promise(done => setTimeout(done, 10));
});

for (const seed of [2147483646, -2]) {
    test(`outstanding asynchronous exports cross signed ID rollover from ${seed}`, { timeout: 30000 }, async () => {
        const fixture = await createFixture({ threaded: true, seedPromiseIds: seed });
        const api = await fixture.create();
        const deferred = [];
        try {
            api.setModuleImports("main.js", { update: value => new Promise(resolve => deferred.push(() => resolve(value + 1))) });
            const exports = (await api.getAssemblyExports("Test")).Sample.Test;
            const pending = Array.from({ length: 8 }, (_, value) => exports.AsyncImport(value));
            deferred.reverse().forEach(resolve => resolve());
            assert.deepEqual(await Promise.all(pending), [1, 2, 3, 4, 5, 6, 7, 8]);
        } finally {
            api.exit(0);
            await fixture.termination;
        }
    });
}
