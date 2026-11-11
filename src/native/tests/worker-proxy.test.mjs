// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from "node:assert/strict";
import test from "node:test";
import { createContext, SourceTextModule, SyntheticModule } from "node:vm";
import { MessageChannel, Worker } from "node:worker_threads";
import { once } from "node:events";
import { readFile } from "node:fs/promises";
import ts from "typescript";
import { createWorkerModuleLoader, seedNativeRequestIds } from "./worker-transport-worker.mjs";

test("worker boot metadata is cloneable while providers and module namespaces remain realm-owned", async () => {
    const bytes = Promise.resolve(new ArrayBuffer(16));
    const response = Promise.resolve(new Response("asset"));
    const uiModule = { dotnetInitializeModule() {} };
    const workerModule = { dotnetInitializeModule() {} };
    const asset = { name: "test.dll", virtualPath: "test.dll", buffer: bytes, pendingDownload: { response } };
    const source = {
        runInWorker: true, environmentVariables: { TEST: "worker" },
        resources: {
            coreAssembly: [asset], assembly: [], wasmNative: [], jsModuleNative: [],
            jsModuleRuntime: [{ name: "runtime.js", moduleExports: uiModule }],
            modulesAfterConfigLoaded: [{ moduleExports: uiModule }],
            satelliteResources: { fr: [{ ...asset, name: "satellite.dll" }] },
            appsettings: [{ ...asset, name: "appsettings.json", virtualPath: "appsettings.json", loadIntoVfs: false }],
        },
    };
    const load = createWorkerModuleLoader(createContext({}), {});
    const module = await load("./worker-config");
    await module.evaluate();
    const boot = module.namespace.createWorkerBootConfig(source);
    const wire = structuredClone(boot.config);
    assert.equal(wire.resources.coreAssembly[0].buffer, undefined);
    assert.equal(wire.resources.coreAssembly[0].pendingDownload, undefined);
    assert.equal(wire.resources.jsModuleRuntime[0].moduleExports, undefined);
    assert.equal(wire.resources.modulesAfterConfigLoaded.length, 0);
    assert.equal(boot.assets.get(wire.resources.coreAssembly[0].workerAssetId), asset);
    assert.equal(asset.buffer, bytes);
    assert.equal(asset.pendingDownload.response, response);
    assert.equal(wire.resources.appsettings[0].buffer, undefined);
    assert.equal(wire.resources.appsettings[0].pendingDownload, undefined);
    assert.equal(boot.assets.get(wire.resources.appsettings[0].workerAssetId), source.resources.appsettings[0]);
    module.namespace.restoreWorkerModules(wire, { resources: { jsModuleRuntime: [{ name: "runtime.js", moduleExports: workerModule }] } });
    assert.equal(wire.resources.jsModuleRuntime[0].moduleExports, workerModule);
    assert.equal(source.resources.jsModuleRuntime[0].moduleExports, uiModule);
});

test("shared loader initialization snapshots ordered config callbacks and runs once across preload and create", async () => {
    const source = await readFile(new URL("../libs/Common/JavaScript/loader/run.ts", import.meta.url), "utf8");
    const declaration = source.match(/export function initializeLoaderConfig[\s\S]*?\n\}/)?.[0];
    assert.ok(declaration);
    const events = [];
    const config = { resources: { modulesAfterConfigLoaded: [] } };
    const late = () => events.push("late");
    const callbacks = { configLoaded: [
        async received => {
            assert.equal(received, config);
            events.push("first");
            callbacks.configLoaded.push(late);
            await Promise.resolve();
            events.push("first-done");
        },
        () => events.push("second"),
    ] };
    const context = createContext({
        console, loaderConfig: config, loaderCallbacks: callbacks, Module: {},
        validateEngineFeatures: async () => events.push("validate-engine"),
        validateLoaderConfig: () => events.push("validate-config"),
        normalizeCollection: value => value ?? [],
        callLibraryInitializerOnRuntimeConfigLoaded: () => {
            throw new Error("Unexpected initializer");
        },
        initPolyfillsLoader: async () => events.push("polyfills"),
    });
    const output = ts.transpileModule("let configInitialization; let modulesAfterConfigLoadedCache;\n" + declaration,
        { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext } }).outputText;
    const module = new SourceTextModule(output, { context });
    await module.link(() => {
        throw new Error("Unexpected import");
    });
    await module.evaluate();
    const first = module.namespace.initializeLoaderConfig();
    const second = module.namespace.initializeLoaderConfig();
    assert.equal(first, second);
    await first;
    await module.namespace.initializeLoaderConfig();
    assert.deepEqual(events, ["validate-engine", "first", "first-done", "second", "validate-config", "polyfills"]);
});

test("worker errors preserve built-in type, stack, cause, status and aggregate members", async () => {
    const load = createWorkerModuleLoader(createContext({ structuredClone }), {});
    const module = await load("./worker-errors");
    await module.evaluate();
    const error = new TypeError("native error", { cause: new RangeError("native cause") });
    error.stack = "native worker stack";
    error.status = 42;
    const received = module.namespace.deserializeWorkerError(structuredClone(module.namespace.serializeWorkerError(error)));
    assert.equal(received.name, "TypeError");
    assert.equal(received.message, "native error");
    assert.equal(received.stack, "native worker stack");
    assert.equal(received.cause.name, "RangeError");
    assert.equal(received.cause.message, "native cause");
    assert.equal(received.status, 42);
    const aggregateModule = await createWorkerModuleLoader(createContext({ Error, TypeError, RangeError, AggregateError, structuredClone }), {})("./worker-errors");
    await aggregateModule.evaluate();
    const aggregate = new AggregateError([error], "aggregate");
    const roundtrip = aggregateModule.namespace.deserializeWorkerError(aggregateModule.namespace.serializeWorkerError(aggregate));
    assert.equal(roundtrip.errors[0].message, "native error");
    const repeated = new AggregateError([error, error], "repeated");
    const repeatedResult = module.namespace.deserializeWorkerError(module.namespace.serializeWorkerError(repeated));
    assert.equal(repeatedResult.errors[0].message, "native error");
    assert.equal(repeatedResult.errors[1].message, "native error");
    const unsupported = new TypeError("original error", { cause: () => {} });
    const unsupportedResult = module.namespace.deserializeWorkerError(module.namespace.serializeWorkerError(unsupported));
    assert.equal(unsupportedResult.name, "TypeError");
    assert.equal(unsupportedResult.message, "original error");
    assert.equal(unsupportedResult.cause.name, "DataCloneError");
    const bigint = new Error("bigint", { cause: 42n });
    const clone = module.namespace.deserializeWorkerError(module.namespace.serializeWorkerError(bigint));
    assert.equal(clone.cause, 42n);
    const jsonError = module.namespace.deserializeWorkerError(JSON.parse(JSON.stringify(module.namespace.serializeWorkerError(bigint, true))));
    assert.equal(jsonError.cause.name, "DataCloneError");
});

test("all diagnostic collectors transfer aliased byte views and preserve structured failures", async () => {
    const load = createWorkerModuleLoader(createContext({ MessageChannel, ArrayBuffer, Uint8Array, structuredClone }), {});
    const module = await load("./worker-local");
    await module.evaluate();
    const operations = ["collectMetrics", "collectGcDump", "collectPgoTrace", "collectCpuSamples"];
    let backing, options;
    const local = Object.fromEntries(operations.map(name => [name, async value => {
        options = value;
        backing = new ArrayBuffer(16);
        new Uint8Array(backing).set([1, 2, 3, 4], 3);
        return [new Uint8Array(backing, 3, 2), new Uint8Array(backing, 5, 2)];
    }]));
    local.loadLazyAssembly = async () => {
        throw new TypeError("host failure", { cause: new RangeError("host cause") });
    };
    const worker = { postMessage: request => {
        void module.namespace.serveWorkerLocal(request, local);
    } };
    const proxy = module.namespace.createWorkerLocal(worker, new AbortController().signal, {});
    for (const name of operations) {
        const chunks = await proxy[name]({ durationMs: 1 });
        assert.deepEqual([...chunks[0]], [1, 2]);
        assert.deepEqual([...chunks[1]], [3, 4]);
        assert.equal(chunks[0].byteOffset, 3);
        assert.equal(chunks[1].byteOffset, 5);
        assert.equal(chunks[0].buffer, chunks[1].buffer);
        assert.equal(backing.byteLength, 0);
        assert.equal(options.skipDownload, true);
    }
    await assert.rejects(proxy.loadLazyAssembly("missing"), error => {
        assert.equal(error.name, "TypeError");
        assert.equal(error.message, "host failure");
        assert.equal(error.cause.name, "RangeError");
        assert.equal(error.cause.message, "host cause");
        return true;
    });
});

test("UI, worker and sidecar environment flags distinguish physical workers from runtime roles", async () => {
    for (const [globals, expected] of [
        [{ window: {} }, [false, false, false, true]],
        [{ importScripts() {} }, [true, false, true, true]],
        [{ importScripts() {}, dotnetSidecar: true }, [true, true, false, true]],
        [{ importScripts() {}, dotnetSidecar: false }, [true, true, false, true]],
    ]) {
        const load = createWorkerModuleLoader(createContext(globals), {});
        const module = await load("../per-module/index");
        await module.evaluate();
        const env = module.namespace;
        assert.deepEqual([env.ENVIRONMENT_IS_WEB_WORKER, env.ENVIRONMENT_IS_SIDECAR, env.ENVIRONMENT_IS_WORKER, env.ENVIRONMENT_IS_WEB], expected);
    }
});

test("realm-owned JS and virtual GC handles cannot collide and freed handles retain ownership", async () => {
    for (const worker of [false, true]) {
        const released = [];
        const load = createWorkerModuleLoader(createContext({}), {
            "consts:configuration": { default: "Debug" },
            "./cross-module": { dotnetAssert: { check: (condition, message) => assert.ok(condition, message) }, dotnetLogger: {}, dotnetLoaderExports: {} },
            "./types": { GCHandleNull: 0 },
            "./utils": { assertJsInterop() {}, isRuntimeRunning: () => true },
            "./weak-ref": { useWeakRef: true, createStrongRef: value => ({ deref: () => value }), createWeakRef: value => ({ deref: () => value }) },
            "./managed-exports": { releaseJsOwnedObjectByGcHandle: handle => released.push(handle) },
        });
        const module = await load("../../../System.Runtime.InteropServices.JavaScript.Native/interop/gc-handles");
        await module.evaluate();
        const gc = module.namespace;
        gc.initializeRealmHandles(worker);
        const first = gc.getJsHandleFromJSObject({});
        const second = gc.getJsHandleFromJSObject({});
        assert.equal(first, worker ? 2 : 1);
        assert.equal(second, first + 2);
        gc.releaseCSOwnedObject(first);
        assert.equal(gc.getJsHandleFromJSObject({}), first);
        assert.equal(gc.allocGcvHandle(), worker ? -2 : -3);
        assert.equal(gc.allocGcvHandle(), worker ? -4 : -5);
        assert.equal(gc.getProxyCounts()[0], 2);
    }
});

test("worker imports, properties, object release and promise callbacks use the owning normal marshaler", async () => {
    const calls = [];
    let localBindingError = 0;
    let remoteBindingError = 0;
    const signatures = new Map([
        [100, { name: "INTERNAL.http_wasm_fetch", handle: 10 }],
        [200, { name: "INTERNAL.ws_wasm_send", handle: 20 }],
        [300, { name: "INTERNAL.TARGET.getProperty", handle: 30 }],
        [400, { name: "console.log", handle: 40 }],
        [500, { name: "INTERNAL.http_wasm_fetch", module: "application", handle: 50 }],
    ]);
    const record = name => (...args) => {
        calls.push([name, ...args]); return 0;
    };
    const api = { INTERNAL: {} };
    const load = createWorkerModuleLoader(createContext({}), {
        "../../Common/JavaScript/loader/worker-native": {
            NativeOperation: {},
            RuntimeOperation: { BindJSImport: 0, InvokeJSImport: 1, ReleaseCSOwnedObject: 2, ResolveOrRejectPromise: 3, CancelPromise: 4, InvokeJSFunction: 5, ForceDisposeProxies: 6, AbortInteropTimers: 7 },
        },
        "../../Common/JavaScript/loader/worker-errors": { serializeWorkerError() {}, deserializeWorkerError() {} },
        "./cross-module": { dotnetApi: api, dotnetBrowserUtilsExports: {}, Module: {} },
        "./invoke-js": {
            bindJSImportST: signature => {
                assert.ok(signatures.has(signature), "Binding must finish before the managed signature buffer is freed");
                record("bind")(signature);
                return localBindingError;
            },
            invokeJSImportST: record("invoke"), invokeJSFunction: record("function"),
            getProperty() {}, setProperty() {}, hasProperty() {}, getTypeOfProperty() {},
        },
        "./cancelable-promise": { cancelPromise: record("cancel") },
        "./gc-handles": {
            releaseCSOwnedObject: record("release"), getJSObjectFromJSHandle: handle => handle === -2 ? {} : undefined,
            lookupJsOwnedObject: handle => handle === 1024 ? {} : undefined, forceDisposeProxies: record("dispose"), getProxyCounts() {},
        },
        "./marshal-to-js": { resolveOrRejectPromise: record("complete") },
        "./marshal": {
            getSignatureFunctionName: signature => signatures.get(signature).name,
            getSignatureHandle: signature => signatures.get(signature).handle,
            getSignatureModuleName: signature => signatures.get(signature).module,
            getArg: (args, index) => {
                assert.equal(index, 2); return args;
            },
            getArgJsHandle: arg => arg,
        },
        "./scheduling": { abortInteropTimers: record("timers") },
    });
    const module = await load("../../../System.Runtime.InteropServices.JavaScript.Native/interop/worker-interop");
    await module.evaluate();
    const table = module.namespace.createWorkerInterop({ invoke: (...args) => {
        record("ui")(...args);
        return remoteBindingError;
    } });
    localBindingError = 1234;
    assert.equal(table[0](300), 1234);
    assert.deepEqual(calls.splice(0), [["bind", 300]]);
    localBindingError = 0;
    remoteBindingError = 5678;
    assert.equal(table[0](300), 5678);
    assert.deepEqual(calls.splice(0), [["bind", 300], ["ui", 0, 300]]);
    remoteBindingError = 0;
    for (const signature of signatures.keys()) assert.equal(table[0](signature), 0);
    assert.deepEqual(calls.splice(0), [
        ["bind", 100], ["bind", 200], ["bind", 300], ["ui", 0, 300], ["ui", 0, 400], ["ui", 0, 500],
    ]);
    // Managed code frees each native signature buffer after the initial bind returns.
    signatures.clear();
    table[1](10, 123);
    table[1](20, 456);
    table[1](30, 2);
    table[1](30, 4);
    table[1](30, 1);
    table[1](40, 789);
    table[1](50, 987);
    assert.deepEqual(calls.splice(0), [
        ["invoke", 10, 123], ["invoke", 20, 456], ["invoke", 30, 2],
        ["invoke", 30, 4], ["ui", 1, 30, 1], ["ui", 1, 40, 789], ["ui", 1, 50, 987],
    ]);
    for (const handle of [2, 1, -2, -3]) {
        table[2](handle); table[3](handle); table[5](handle, 99);
    }
    table[4](1024);
    table[4](2048);
    table[6](false, true);
    table[7]();
    assert.deepEqual(calls, [
        ["release", 2], ["complete", 2], ["function", 2, 99],
        ["ui", 2, 1], ["ui", 3, 1], ["ui", 5, 1, 99],
        ["release", -2], ["complete", -2], ["function", -2, 99],
        ["ui", 2, -3], ["ui", 3, -3], ["ui", 5, -3, 99],
        ["cancel", 1024], ["ui", 4, 2048],
        ["dispose", false, true], ["ui", 6, 0, 1], ["timers"], ["ui", 7],
    ]);
});

test("typed and generic JSObject marshaling reject foreign handles before assertions in worker hosting", async () => {
    const source = await readFile(new URL("../libs/System.Runtime.InteropServices.JavaScript.Native/interop/marshal-to-js.ts", import.meta.url), "utf8");
    const declarations = ["getJSObjectForMarshaling", "_marshalJsObjectToJs", "_marshalCsObjectToJs"].map(name => {
        const declaration = source.match(new RegExp(`function ${name}[\\s\\S]*?\\n\\}`))?.[0];
        assert.ok(declaration);
        return `export ${declaration}`;
    });
    const output = ts.transpileModule(declarations.join("\n"), {
        compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext },
    }).outputText;
    const internals = [];
    const objects = new Map();
    const types = { None: 0, JSObject: 1 };
    const module = new SourceTextModule(output, { context: createContext({
        Error,
        MarshalerType: types,
        dotnetInternals: internals,
        dotnetAssert: { fastCheck: (condition, message) => assert.ok(condition, condition ? undefined : message()) },
        InternalExchangeIndex: { WorkerRuntimeContext: 0 },
        getArgType: arg => arg.type,
        getArgJsHandle: arg => arg.handle,
        getJSObjectFromJSHandle: handle => objects.get(handle),
    }) });
    await module.link(() => {
        throw new Error("Unexpected converter import");
    });
    await module.evaluate();
    const converters = [module.namespace._marshalJsObjectToJs, module.namespace._marshalCsObjectToJs];
    const workerObject = {};
    const uiObject = {};
    for (const role of ["worker", "ui"]) {
        internals[0] = { role, getRemoteError: () => assert.fail("Objects must not use error snapshot transport") };
        const localHandle = role === "worker" ? 2 : 1;
        const foreignHandle = role === "worker" ? 1 : 2;
        const localObject = role === "worker" ? workerObject : uiObject;
        objects.clear();
        objects.set(localHandle, localObject);
        for (const convert of converters) {
            assert.equal(convert({ type: types.JSObject, handle: localHandle }), localObject);
            assert.equal(convert({ type: types.None }), null);
            assert.throws(() => convert({ type: types.JSObject, handle: foreignHandle }), {
                name: "Error",
                message: `JS object JSHandle ${foreignHandle} is not available in this realm; cross-realm JSObject marshaling is not supported.`,
            });
        }
    }
    internals[0] = undefined;
    assert.equal(module.namespace._marshalCsObjectToJs({ type: types.JSObject, handle: 2 }), undefined);
    assert.throws(() => module.namespace._marshalJsObjectToJs({ type: types.JSObject, handle: 2 }), {
        name: "AssertionError",
        message: "JS object JSHandle 2 was not found",
    });
    for (const convert of converters) {
        assert.equal(convert({ type: types.JSObject, handle: 1 }), uiObject);
        assert.equal(convert({ type: types.None }), null);
    }
});

test("Task result conversion failures reject completed and delayed Promises and release holders exactly once", async () => {
    const source = await readFile(new URL("../libs/System.Runtime.InteropServices.JavaScript.Native/interop/marshal-to-js.ts", import.meta.url), "utf8");
    const declarations = [
        "createTaskHolder", "tryMarshalSyncTaskToJs", "beginMarshalTaskToJs",
        "endMarshalTaskToJs", "releaseEagerTaskHolder", "resolveOrRejectPromise",
    ].map(name => {
        const declaration = source.match(new RegExp(`function ${name}[\\s\\S]*?\\n\\}`))?.[0];
        assert.ok(declaration);
        return `export ${declaration}`;
    });
    const holderDeclaration = source.match(/export class TaskHolder[\s\S]*?\n\}/)?.[0];
    assert.ok(holderDeclaration);
    const output = ts.transpileModule([holderDeclaration, ...declarations].join("\n"), {
        compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext },
    }).outputText;
    const types = { None: 0, Void: 1, JSObject: 2, TaskResolved: 3, TaskRejected: 4, TaskPreCreated: 5 };
    const holders = new Map();
    const releases = [];
    const converters = new Map();
    let nextHandle = 1;
    const check = (condition, message) => assert.ok(condition, condition ? undefined : typeof message === "function" ? message() : message);
    const module = new SourceTextModule(output, { context: createContext({
        Promise,
        BuildConfiguration: "Release",
        MarshalerType: types,
        dotnetAssert: { check, fastCheck: check },
        dotnetLoaderExports: { createPromiseCompletionSource: () => {
            let resolve, reject;
            const promise = new Promise((res, rej) => {
                resolve = res; reject = rej;
            });
            return { promise, resolve, reject };
        } },
        csToJsMarshalers: converters,
        eagerTaskHandleSymbol: Symbol("eagerTaskHandle"),
        jsinteropDoc: "test",
        getArg: (args, index) => args[index],
        getArgType: arg => arg.type,
        getArgElementType: arg => arg.elementType,
        getArgJsHandle: arg => arg.handle,
        setArgType: (arg, type) => {
            arg.type = type;
        },
        setJsHandle: (arg, handle) => {
            arg.handle = handle;
        },
        getJsHandleFromJSObject: holder => {
            const handle = nextHandle++;
            holders.set(handle, holder);
            return handle;
        },
        getJSObjectFromJSHandle: handle => holders.get(handle),
        releaseCSOwnedObject: handle => {
            assert.equal(holders.delete(handle), true, "Holder must be released exactly once");
            releases.push(handle);
        },
        fixupPointer: args => args,
        isReceiverShouldFree: () => false,
        isRuntimeRunning: () => true,
        assertRuntimeRunning() {},
        marshalExceptionToCs: () => assert.fail("Conversion failure must reject the owning Promise"),
    }) });
    await module.link(() => {
        throw new Error("Unexpected Task converter import");
    });
    await module.evaluate();
    const api = module.namespace;
    for (const delayed of [false, true]) {
        for (const suppliedConverter of [false, true]) {
            for (const fails of [false, true]) {
                const failure = new Error("Result conversion failed");
                const value = {};
                let conversions = 0;
                const convert = () => {
                    conversions++;
                    if (fails) throw failure;
                    return value;
                };
                converters.set(types.JSObject, convert);
                const converter = suppliedConverter ? convert : undefined;
                const res = {};
                const eagerPromise = api.beginMarshalTaskToJs(res, undefined, converter);
                const handle = res.handle;
                const releaseCount = releases.length;
                if (!delayed) {
                    res.type = types.TaskResolved;
                    res.elementType = types.JSObject;
                }
                let promise;
                assert.doesNotThrow(() => {
                    promise = api.endMarshalTaskToJs([{}, res], converter, eagerPromise);
                });
                assert.ok(promise instanceof Promise);
                const settled = fails ? assert.rejects(promise, error => error === failure) : promise.then(result => assert.equal(result, value));
                if (delayed) {
                    assert.equal(promise, eagerPromise);
                    assert.equal(holders.has(handle), true);
                    assert.doesNotThrow(() => api.resolveOrRejectPromise([
                        {}, {}, { type: types.TaskResolved, handle }, { type: types.JSObject },
                    ]));
                }
                await settled;
                assert.equal(conversions, 1);
                assert.deepEqual(releases.slice(releaseCount), [handle]);
                assert.equal(holders.size, 0);
            }
        }
    }
});

test("boot runInWorker selects hosting without a builder method or timeout-options opt-in", async () => {
    for (const [runInWorker, inWorker, expected] of [
        [true, false, "worker"], [false, false, "ordinary"], [undefined, false, "ordinary"], [true, true, "ordinary"],
    ]) {
        const config = { runInWorker, workerOptions: { timeoutMs: 1 } };
        const api = {};
        const calls = [];
        const load = createWorkerModuleLoader(createContext({}), {
            "./cross-module": { Module: {}, dotnetApi: api },
            "./callbacks": { extractLegacyModuleCallbacks: value => value, loaderCallbacks: {} },
            "./config": {
                loaderConfig: config, mergeLoaderConfig: source => Object.assign(config, source), validateLoaderConfig() {},
            },
            "./run": { createRuntime: async () => calls.push("ordinary") },
            "./exit": { exit: (_code, error) => {
                throw error;
            } },
            "./assets": { setLoadBootResourceCallback() {} },
            "./worker-proxy": { createWorkerProxy: async () => {
                calls.push("worker"); return api;
            } },
            "./per-module": { ENVIRONMENT_IS_WEB_WORKER: inWorker, ENVIRONMENT_IS_SIDECAR: false },
        });
        const module = await load("./host-builder");
        await module.evaluate();
        const builder = new module.namespace.HostBuilder();
        assert.equal(builder.withWorker, undefined);
        assert.equal(await builder.create(), api);
        assert.deepEqual(calls, [expected]);
    }
});

test("runInWorker config preserves boot selection across unrelated merges and honors explicit false", async () => {
    const load = createWorkerModuleLoader(createContext({}), { "./per-module": { browserVirtualAppBase: "/" } });
    const module = await load("./config");
    await module.evaluate();
    const { mergeLoaderConfig, loaderConfig } = module.namespace;
    mergeLoaderConfig({ runInWorker: true });
    mergeLoaderConfig({ diagnosticTracing: true });
    assert.equal(loaderConfig.runInWorker, true);
    mergeLoaderConfig({ runInWorker: false });
    mergeLoaderConfig({ workerOptions: { timeoutMs: 100 } });
    assert.equal(loaderConfig.runInWorker, false);
});

test("native host operations retain INTERNAL names while UI helpers are isolated in TARGET", async () => {
    const bytes = new Uint8Array(64);
    const load = createWorkerModuleLoader(createContext({}), {
        "../../Common/JavaScript/ems-ambient": { _ems_: { _GetDotNetRuntimeContractDescriptor: () => 16, HEAPU8: bytes } },
    });
    const module = await load("../../../System.Native.Browser/utils/cdac");
    await module.evaluate();
    const runtime = { INTERNAL: { TARGET: { getProperty: () => 42 } } };
    module.namespace.registerCDAC(runtime);
    assert.deepEqual(Object.keys(runtime.INTERNAL), ["TARGET", "GetDotNetRuntimeContractDescriptor", "GetDotNetRuntimeHeap"]);
    assert.equal(runtime.INTERNAL.getProperty, undefined);
    assert.equal(runtime.INTERNAL.TARGET.getProperty(), 42);
    assert.equal(runtime.INTERNAL.GetDotNetRuntimeContractDescriptor(), 16);
    const view = runtime.INTERNAL.GetDotNetRuntimeHeap(16, 8);
    assert.equal(view.buffer, bytes.buffer);
    bytes[16] = 68;
    assert.equal(view[0], 68);
});

test("diagnostic downloads forward from the native worker without DOM access and preserve returned trace data", async () => {
    const chunks = [new Uint8Array([78, 101, 116, 116, 114, 97, 99, 101])];
    let forwarded;
    const internals = [];
    internals[10] = { role: "worker", downloadDiagnosticData: messages => forwarded = messages };
    const globals = {};
    Object.defineProperty(globals, "document", { get() {
        throw new Error("Worker attempted a DOM operation");
    } });
    const load = createWorkerModuleLoader(createContext(globals), {
        "../types": { InternalExchangeIndex: { WorkerRuntimeContext: 10 } },
        "./cross-module": { dotnetApi: {}, dotnetLogger: {}, dotnetInternals: internals },
    });
    const module = await load("../../../System.Native.Browser/diagnostics/common");
    await module.evaluate();
    module.namespace.downloadBlob(chunks);
    assert.equal(forwarded, chunks);
    assert.equal(chunks[0].byteLength, 8);
    delete internals[10].downloadDiagnosticData;
    assert.throws(() => module.namespace.downloadBlob(chunks), /forwarding is not initialized/);
});

test("native host requests preserve UI promises while native-owned assembly and diagnostics work executes remotely", async () => {
    let adapter;
    const calls = [];
    let complete;
    const local = {
        loadLazyAssembly: async name => {
            calls.push(name);
            if (name === "missing.dll") throw new Error("local assembly missing");
            if (name === "pending.dll") return new Promise(resolve => complete = resolve);
            return true;
        },
        loadSatelliteAssemblies: async cultures => calls.push(cultures),
        collectMetrics: async options => {
            assert.equal(options.skipDownload, true);
            return [new Uint8Array([78, 101, 116]), new Uint8Array([116, 114, 97, 99, 101])];
        },
    };
    const worker = { postMessage(request, transfers) {
        void adapter.serveWorkerLocal(structuredClone(request, { transfer: transfers }), local);
    } };
    const load = createWorkerModuleLoader(createContext({ Error, MessageChannel, Uint8Array, ArrayBuffer }), {});
    const module = await load("./worker-local");
    await module.evaluate();
    adapter = module.namespace;
    const abort = new AbortController();
    const client = adapter.createWorkerLocal(worker, abort.signal, {
        GetDotNetRuntimeContractDescriptor: () => 128,
        GetDotNetRuntimeHeap: () => new Uint8Array([1, 2]),
        connectDSRouter: url => calls.push(url),
    });
    assert.equal(await client.loadLazyAssembly("fixture.dll"), true);
    await client.loadSatelliteAssemblies(["fr-FR"]);
    await assert.rejects(client.loadLazyAssembly("missing.dll"), /local assembly missing/);
    const chunks = await client.collectMetrics({ skipDownload: false, durationSeconds: 0.1 });
    assert.deepEqual(chunks.map(chunk => Array.from(chunk)), [[78, 101, 116], [116, 114, 97, 99, 101]]);
    assert.equal(client.GetDotNetRuntimeContractDescriptor(), 128);
    client.connectDSRouter("ws://localhost:9000");
    assert.deepEqual(calls, ["fixture.dll", ["fr-FR"], "missing.dll", "ws://localhost:9000"]);
    const pending = client.loadLazyAssembly("pending.dll");
    while (!complete) await new Promise(resolve => setImmediate(resolve));
    abort.abort(new Error("LOCAL host exited"));
    await assert.rejects(pending, /LOCAL host exited/);
    complete(true);
    await assert.rejects(client.loadLazyAssembly("late.dll"), /LOCAL host exited/);
});

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

async function loadAssetHelpers(fetch = async () => new Response("fallback")) {
    const callbacks = {};
    const internals = [];
    const load = createWorkerModuleLoader(createContext({ Response, Uint8Array, URL }), {
        "../types": { InternalExchangeIndex: { WorkerRuntimeContext: 10 } },
        "./cross-module": {
            dotnetAssert: { check: (condition, message) => assert.ok(condition, message) },
            dotnetLogger: { debug() {}, warn() {} }, dotnetInternals: internals,
            dotnetBrowserHostExports: { loadIcuData() {} }, dotnetUpdateInternals() {},
            dotnetDiagnosticsExports: {}, dotnetNativeBrowserExports: {}, dotnetApi: {},
        },
        "./per-module": { ENVIRONMENT_IS_SHELL: false, ENVIRONMENT_IS_NODE: false, ENVIRONMENT_IS_WEB: true, browserVirtualAppBase: "/" },
        "./bootstrap": {
            locateFile: name => "https://example.test/" + name,
            makeURLAbsoluteWithApplicationBase: url => new URL(url, "https://example.test/").href,
        },
        "./polyfills": { fetchLike: fetch, responseLike: (_url, body, options) => new Response(body, options) },
        "./config": { loaderConfig: {} }, "./callbacks": { loaderCallbacks: callbacks },
    });
    const module = await load("./assets");
    await module.evaluate();
    return { assets: module.namespace, callbacks, internals };
}

test("resource callbacks preserve defaults, URLs, response promises, fetch policy and failures", async () => {
    const fetches = [];
    const { assets, internals } = await loadAssetHelpers(async (url, options) => {
        fetches.push({ url, options }); return new Response("fallback");
    });
    const asset = { name: "test.wasm", resolvedUrl: "https://example.test/test.wasm", behavior: "webcil", hash: "sha256-test" };
    for (const result of [undefined, null, "redirect.wasm"]) {
        assets.setLoadBootResourceCallback((...args) => {
            assert.deepEqual(args, ["assembly", asset.name, asset.resolvedUrl, asset.hash, "webcil"]);
            return result;
        });
        const request = { ...asset };
        assert.equal(await (await assets.loadResourceFetch(request)).text(), "fallback");
        assert.equal(request.resolvedUrl, result ? "https://example.test/redirect.wasm" : asset.resolvedUrl);
        assert.equal(fetches.at(-1).options.integrity, asset.hash);
        assert.equal(fetches.at(-1).options.cache, "no-cache");
    }
    const abort = new AbortController();
    assets.setLoadBootResourceCallback(undefined);
    await assets.loadResourceFetch({ ...asset, cache: "reload", useCredentials: true }, abort.signal);
    assert.equal(fetches.at(-1).options.credentials, "include");
    assert.equal(fetches.at(-1).options.integrity, undefined);
    assert.equal(fetches.at(-1).options.cache, "reload");
    assert.equal(fetches.at(-1).options.signal, abort.signal);
    const response = new Response("custom");
    assets.setLoadBootResourceCallback(() => Promise.resolve(response));
    assert.equal(await assets.loadResourceFetch({ ...asset }), response);
    assets.setLoadBootResourceCallback(() => {
        throw new Error("custom resource failed");
    });
    await assert.rejects(assets.loadResourceFetch({ ...asset }), /custom resource failed/);
    internals[10] = { loadResource: async () => response };
    assert.equal(await assets.loadResourceFetch({ ...asset }), response);
});

test("module resource callbacks preserve default URLs and reject non-URL overrides", async () => {
    const { assets } = await loadAssetHelpers();
    for (const result of [undefined, null, "redirect.js"]) {
        assets.setLoadBootResourceCallback(() => result);
        assert.equal(assets.resolveModuleResource({ name: "native.js" }),
            "https://example.test/" + (result || "native.js"));
    }
    assets.setLoadBootResourceCallback(() => Promise.resolve(new Response("not a module URL")));
    assert.throws(() => assets.resolveModuleResource({ name: "native.js" }), /must return string URL/);
});

test("UI and worker progress share monotonic counters and finish at the exact combined total", async () => {
    const { assets, callbacks } = await loadAssetHelpers();
    const progress = [];
    callbacks.downloadResourceProgress = (loaded, total) => progress.push([loaded, total]);
    assets.updateWorkerDownloadProgress(2, 5);
    assets.nativeModulePromiseController.resolve({});
    await assets.fetchIcu({ name: "icu.dat", buffer: new ArrayBuffer(1) });
    assets.updateWorkerDownloadProgress(5, 5);
    assert.deepEqual(progress, [[2, 5], [3, 6], [6, 6]]);
    assets.verifyAllAssetsDownloaded();
});

test("native resource messages transfer streams and metadata without application value serialization", async () => {
    let adapter;
    let requestAsset;
    let status = 206;
    const abort = new AbortController();
    const load = createWorkerModuleLoader(createContext({
        Error, Response, MessageChannel,
        self: { postMessage(request, options) {
            const cloned = structuredClone(request, options);
            void adapter.serveWorkerResource(cloned, abort.signal);
        } },
    }), {
        "./assets": {
            resolveModuleResource: () => "https://example.test/redirect.js",
            loadResourceFetch: async asset => {
                requestAsset = asset;
                if (status === 0) return Response.error();
                if (status === 204) return new Response(null, { status });
                asset.resolvedUrl = "https://example.test/redirect.wasm";
                const response = new Response(new Uint8Array([0, 97, 115, 109]), {
                    status: 206, statusText: "Partial Content", headers: { "Content-Type": "application/wasm", "X-Test": "native" },
                });
                Object.defineProperties(response, {
                    url: { value: asset.resolvedUrl }, redirected: { value: true }, type: { value: "cors" },
                });
                return response;
            },
        },
    });
    const module = await load("./worker-assets");
    await module.evaluate();
    adapter = module.namespace;
    const asset = { name: "test.wasm", behavior: "webcil", buffer: Promise.resolve(new ArrayBuffer(1)) };
    assert.equal(await adapter.resolveWorkerModule(asset), "https://example.test/redirect.js");
    const response = await adapter.loadWorkerResource(asset);
    assert.equal(requestAsset.buffer, undefined);
    assert.equal(response.status, 206);
    assert.equal(response.statusText, "Partial Content");
    assert.equal(response.url, "https://example.test/redirect.wasm");
    assert.equal(asset.resolvedUrl, response.url);
    assert.equal(response.redirected, true);
    assert.equal(response.type, "cors");
    assert.equal(response.headers.get("x-test"), "native");
    assert.equal(response.bodyUsed, false);
    assert.deepEqual(Array.from(new Uint8Array(await response.arrayBuffer())), [0, 97, 115, 109]);
    for (status of [0, 204]) {
        const empty = await adapter.loadWorkerResource(asset);
        assert.equal(empty.status, status);
        assert.equal(empty.body, null);
        assert.equal(empty.type, status === 0 ? "error" : "default");
    }
});

test("native resource failures reject and aborted late responses are canceled rather than transferred", async () => {
    let adapter;
    let complete;
    const abort = new AbortController();
    const load = createWorkerModuleLoader(createContext({
        Error, Response, MessageChannel,
        self: { postMessage(request, options) {
            void adapter.serveWorkerResource(structuredClone(request, options), abort.signal);
        } },
    }), {
        "./assets": {
            resolveModuleResource: () => {
                throw new Error("custom module failed");
            },
            loadResourceFetch: () => new Promise(resolve => complete = resolve),
        },
    });
    const module = await load("./worker-assets");
    await module.evaluate();
    adapter = module.namespace;
    await assert.rejects(adapter.resolveWorkerModule({ name: "native.js" }), /custom module failed/);
    let canceled = 0;
    let closed = 0;
    const pending = adapter.serveWorkerResource({
        asset: {}, module: false, port: { postMessage() {
            assert.fail("Late response was transferred");
        }, close() {
            closed++;
        } },
    }, abort.signal);
    abort.abort(new Error("worker terminated"));
    complete(new Response(new ReadableStream({ cancel() {
        canceled++;
    } })));
    await pending;
    assert.equal(canceled, 1);
    assert.equal(closed, 1);
});

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

test("UTF8 fallback preserves complete native host properties without TextEncoder", async () => {
    for (const encoder of [undefined, TextEncoder]) {
        const bytes = new Uint8Array(512);
        const encode = value => new TextEncoder().encode(value);
        const emscripten = {
            lengthBytesUTF8: value => encode(value).length,
            stringToUTF8Array(value, output, start, capacity) {
                const encoded = encode(value);
                const count = Math.min(encoded.length, capacity - 1);
                output.set(encoded.subarray(0, count), start);
                output[start + count] = 0;
                return count;
            },
        };
        const load = createWorkerModuleLoader(createContext({ TextEncoder: encoder }), {
            "../utils/cross-module": {
                Module: { _malloc: () => {
                    bytes.fill(0xcc); return 8;
                } },
                dotnetApi: { localHeapViewU8: () => bytes },
            },
            "../../Common/JavaScript/ems-ambient": { _ems_: emscripten },
            "./memory": { getU16Local() {}, arrayBufferNeedsCopy() {}, setU16Local() {}, viewOrCopy() {}, zeroRegion() {} },
        });
        const strings = await load("../../../System.Native.Browser/utils/strings");
        await strings.evaluate();
        strings.namespace.stringsInit();
        for (const value of ["", "HOST_RUNTIME_CONTRACT", "TRUSTED_PLATFORM_ASSEMBLIES", "/System.Private.CoreLib.dll", "A\0B", "\u6f22\u5b57\u{10437}"]) {
            const expected = encode(value);
            assert.deepEqual([...strings.namespace.stringToUTF8(value)], [...expected]);
            assert.equal(strings.namespace.stringToUTF8Ptr(value), 8);
            assert.deepEqual([...bytes.subarray(8, 8 + expected.length)], [...expected]);
            assert.equal(bytes[8 + expected.length], 0);
            assert.equal(bytes[9 + expected.length], 0xcc);
        }
    }
});

test("browser capacity probe unwinds its enclosing managed completion before saturating the entry budget", async () => {
    const module = await import("../../libraries/System.Runtime.InteropServices.JavaScript/tests/System.Runtime.InteropServices.JavaScript.UnitTests/System/Runtime/InteropServices/JavaScript/JavaScriptWorkerTest.mjs");
    let enclosingCompletion = true;
    let imports;
    const depths = [];
    const exports = {
        Nested(depth) {
            assert.equal(enclosingCompletion, false, "The enclosing completion still owns an entry slot");
            depths.push(depth);
            if (depth >= 16) throw new Error("runInWorker: native call capacity exceeded");
            return depth;
        },
        Echo: value => value,
    };
    await module.setup({
        INTERNAL: { nativeBoundary: { getScratchCount: () => 0 } },
        getConfig: () => ({ runInWorker: true }),
        getAssemblyExports: async () => ({ System: { Runtime: { InteropServices: { JavaScript: { Tests: { WorkerInteropTest: exports } } } } } }),
        setModuleImports: (_name, value) => {
            imports = value;
        },
    });
    const result = imports.nativeBoundaryCapacity();
    assert.equal(typeof result.then, "function");
    enclosingCompletion = false;
    assert.equal(await result, 42);
    assert.deepEqual(depths, [15, 16, 4]);
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

test("native-worker role reuses marshalers and delegates owner-aware dispatch without exposing application APIs", async () => {
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
                    const value = name === "createWorkerInterop" ? () => Array.from({ length: 8 }, (_, operation) => (...args) => {
                        forwarded.push([operation, ...args]); return 42;
                    }) : name === "default" ? "proof"
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
    assert.equal(current.initialized.includes("initializeMarshalersToCs"), true);
    assert.equal(current.initialized.includes("initializeMarshalersToJs"), true);
    assert.equal(current.api.getAssemblyExports, undefined);
    assert.equal(current.api.INTERNAL.TARGET, undefined);
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
