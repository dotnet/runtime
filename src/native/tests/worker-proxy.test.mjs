// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { createContext, SourceTextModule, SyntheticModule } from "node:vm";
import ts from "typescript";

const sourceDirectory = new URL("../libs/Common/JavaScript/loader/", import.meta.url);

async function createFixture(options = {}) {
    const workers = [];
    const calls = [];
    const errors = [];
    const workerListeners = [];
    const imports = new Map();
    let uiApi;
    const atomics = Object.create(Atomics);
    atomics.wait = (array, index, expected) => {
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
    const context = createContext({
        AbortController, Atomics: atomics, SharedArrayBuffer, setTimeout, clearTimeout,
        self: {
            addEventListener: (_type, listener) => workerListeners.push(listener),
            postMessage: data => {
                const message = structuredClone(data);
                if (message.type === "ready" && options.invalidReadyMemory) {
                    message.wasmMemory = options.invalidReadyMemory === "missing"
                        ? undefined
                        : new WebAssembly.Memory({ initial: 1 });
                }
                queueMicrotask(() => workers[0].onmessage?.({ data: message }));
            },
            close: () => workers[0].closed = true,
        },
        Worker: class {
            constructor() {
                this.terminated = false;
                this.closed = false;
                workers.push(this);
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

    const cache = new Map();
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

    async function load(specifier) {
        if (cache.has(specifier)) return cache.get(specifier);
        let module;
        if (stubs[specifier]) {
            const exports = stubs[specifier];
            module = new SyntheticModule(Object.keys(exports), function () {
                for (const [key, value] of Object.entries(exports)) this.setExport(key, value);
            }, { context });
        } else {
            const source = await readFile(new URL(`${specifier}.ts`, sourceDirectory), "utf8");
            const output = ts.transpileModule(source, {
                compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext },
            }).outputText;
            module = new SourceTextModule(output, { context, identifier: specifier });
        }
        cache.set(specifier, module);
        await module.link(load);
        return module;
    }

    const workerModule = await load("./worker-runtime");
    await workerModule.evaluate();
    bootstrapWorkerRuntime = workerModule.namespace.bootstrapWorkerRuntime;
    const proxyModule = await load("./worker-proxy");
    await proxyModule.evaluate();
    return {
        workers, calls, errors, termination, memory, module, allocations, imports,
        create: async (config = { workerOptions: { timeoutMs: 1000 } }) => {
            uiApi = await proxyModule.namespace.createWorkerProxy(config);
            return uiApi;
        },
    };
}

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
                        : /synchronous Int32/;
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
