// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { createRequire } from "node:module";
import { test } from "node:test";
import vm from "node:vm";

const require = createRequire(import.meta.url);
const ts = require(process.env.TYPESCRIPT_PATH || "typescript");
const source = readFileSync(new URL("./auxiliary-modules.ts", import.meta.url), "utf8");
const javascript = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
const { readAuxiliaryModuleMetadata, loadAuxiliaryModules } = await import(`data:text/javascript;base64,${Buffer.from(javascript).toString("base64")}`);
const corerunSource = readFileSync(new URL("../../../../../coreclr/hosts/corerun/wasm/libCorerun.js", import.meta.url), "utf8");

test("browser: auxiliary boot resources survive configuration merges", () => {
    const configSource = readFileSync(new URL("../loader/config.ts", import.meta.url), "utf8");
    const javascript = ts.transpileModule(configSource, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS } }).outputText;
    const context = { exports: {}, require: () => ({ browserVirtualAppBase: "/" }) };
    vm.runInNewContext(javascript, context);
    const first = { name: "app.entrypoints.hash.wasm", virtualPath: "app.entrypoints.wasm" };
    const second = { name: "app.1.hash.wasm", virtualPath: "app.1.wasm" };
    context.exports.mergeLoaderConfig({ resources: { auxiliaryAssembly: [first] } });
    context.exports.mergeLoaderConfig({ environmentVariables: { CUSTOM: "1" } });
    context.exports.mergeLoaderConfig({ resources: { auxiliaryAssembly: [second] } });
    assert.deepEqual([...context.exports.loaderConfig.resources.auxiliaryAssembly], [first, second]);
});

function uleb(value) {
    const bytes = [];
    do {
        const byte = value & 127;
        value >>>= 7;
        bytes.push(byte | (value ? 128 : 0));
    } while (value);
    return bytes;
}
function string(value) {
    const bytes = [...new TextEncoder().encode(value)];
    return [...uleb(bytes.length), ...bytes];
}
function section(id, bytes) {
    return [id, ...uleb(bytes.length), ...bytes];
}
function auxiliaryWasm(statusOffset, installTable) {
    const address = offset => [0x23, 0, 0x41, ...uleb(offset), 0x6a];
    const instructions = [
        ...address(statusOffset), 0x28, 2, 0, 0x04, 0x40, 0x0f, 0x0b,
        ...address(180 + statusOffset), 0x41, 0, 0x41, 4, 0xfc, 8, 0, 0,
    ];
    if (installTable) instructions.push(0x23, 1, 0x41, 4, 0x6a, 0x41, 0, 0x41, 1, 0xfc, 12, 0, 0);
    instructions.push(...address(statusOffset), 0x41, 1, 0x36, 2, 0, 0x0b);
    const activate = [0, ...instructions];
    const body = [0, 0x41, 42, 0x0b];
    return new Uint8Array([
        0, 97, 115, 109, 1, 0, 0, 0,
        ...section(1, [2, 0x60, 0, 0, 0x60, 0, 1, 0x7f]),
        ...section(2, [
            4,
            ...string("webcil"), ...string("memory"), 2, 0, 1,
            ...string("webcil"), ...string("__memory_base"), 3, 0x7f, 0,
            ...string("webcil"), ...string("__table_base"), 3, 0x7f, 0,
            ...string("webcil"), ...string("__indirect_function_table"), 1, 0x70, 0, 0,
        ]),
        ...section(3, [2, 0, 1]),
        ...section(7, [1, ...string("ActivateModule"), 0, 0]),
        ...section(9, [1, 1, 0, 1, 1]),
        ...section(12, [1]),
        ...section(10, [2, ...uleb(activate.length), ...activate, ...uleb(body.length), ...body]),
        ...section(11, [1, 1, 4, 101, 102, 103, 104]),
    ]);
}
function fixture() {
    const memory = new WebAssembly.Memory({ initial: 1 });
    const table = new WebAssembly.Table({ element: "anyfunc", initial: 12 });
    const payloadPtr = 256;
    const view = new DataView(memory.buffer);
    const values = [0, 4, 1, 64, 96, 4, 1, 0];
    values.forEach((value, i) => view.setUint32(payloadPtr + i * 4, value, true));
    new Uint8Array(memory.buffer).set(new TextEncoder().encode("app.entrypoints.wasm\0"), payloadPtr + 64);
    new Uint8Array(memory.buffer).set(new TextEncoder().encode("app.1.wasm\0"), payloadPtr + 96);
    const primary = { exports: { getAuxiliaryModuleMetadata: () => payloadPtr } };
    const imports = { webcil: {
        memory,
        __memory_base: new WebAssembly.Global({ value: "i32", mutable: false }, payloadPtr),
        __table_base: new WebAssembly.Global({ value: "i32", mutable: false }, 0),
        __indirect_function_table: table,
    } };
    let notifications = 0;
    const errors = [];
    const modules = readAuxiliaryModuleMetadata(primary, memory, payloadPtr, 512, 5);
    const bytes = { "app.entrypoints.wasm": auxiliaryWasm(0, false), "app.1.wasm": auxiliaryWasm(28, true) };
    const notify = () => {
        assert.equal(view.getUint32(payloadPtr, true), 1);
        notifications++;
    };
    return { memory, table, payloadPtr, view, primary, imports, errors, modules, bytes, notify, notifications: () => notifications };
}
async function until(predicate) {
    for (let i = 0; i < 100 && !predicate(); i++) await new Promise(resolve => setImmediate(resolve));
    assert.ok(predicate());
}

for (const first of ["app.entrypoints.wasm", "app.1.wasm"]) {
    test(`browser: independent activation, ${first} first`, async () => {
        const f = fixture();
        const pending = new Map();
        const completion = loadAuxiliaryModules(f.modules, f.memory, f.imports,
            name => new Promise(resolve => pending.set(name, resolve)), f.notify, error => f.errors.push(error));
        assert.equal(f.view.getUint32(f.payloadPtr, true), 0);
        assert.equal(f.view.getUint32(f.payloadPtr + 28, true), 0);
        assert.equal(f.table.get(4), null);
        pending.get(first)(new Response(f.bytes[first]));
        await until(() => f.view.getUint32(f.payloadPtr + (first === "app.1.wasm" ? 28 : 0), true) === 1);
        assert.equal(f.view.getUint32(f.payloadPtr + (first === "app.1.wasm" ? 0 : 28), true), 0);
        const second = first === "app.1.wasm" ? "app.entrypoints.wasm" : "app.1.wasm";
        pending.get(second)(new Response(f.bytes[second]));
        await completion;
        assert.equal(f.notifications(), 1);
        assert.equal(f.table.get(4)(), 42);
        assert.equal(f.table.length, 12);
        assert.deepEqual([...new Uint8Array(f.memory.buffer, f.payloadPtr + 208, 4)], [101, 102, 103, 104]);
        await loadAuxiliaryModules(f.modules, f.memory, f.imports, () => assert.fail("duplicate download"), f.notify, error => f.errors.push(error));
        assert.equal(f.notifications(), 1);
        assert.deepEqual(f.errors, []);
        const duplicate = await WebAssembly.instantiate(f.bytes["app.1.wasm"], f.imports);
        new Uint8Array(f.memory.buffer)[f.payloadPtr + 208] = 77;
        duplicate.instance.exports.ActivateModule();
        assert.equal(new Uint8Array(f.memory.buffer)[f.payloadPtr + 208], 77);
    });
}

test("browser: all-hot image activates an empty auxiliary body range", async () => {
    const f = fixture();
    f.view.setUint32(f.payloadPtr + 4, 5, true);
    f.view.setUint32(f.payloadPtr + 20, 5, true);
    f.view.setUint32(f.payloadPtr + 24, 0, true);
    f.bytes["app.1.wasm"] = auxiliaryWasm(28, false);
    const modules = readAuxiliaryModuleMetadata(f.primary, f.memory, f.payloadPtr, 512, 5);
    await loadAuxiliaryModules(modules, f.memory, f.imports,
        async name => new Response(f.bytes[name]), f.notify, error => f.errors.push(error));
    assert.equal(f.view.getUint32(f.payloadPtr, true), 1);
    assert.equal(f.view.getUint32(f.payloadPtr + 28, true), 1);
    assert.equal(f.notifications(), 1);
    assert.equal(f.table.get(4), null);
    assert.equal(f.table.length, 12);
    assert.deepEqual(f.errors, []);
});

for (const failure of ["missing", "invalid", "no-export"]) {
    test(`browser: ${failure} sidecar is reported and awaitable`, async () => {
        const f = fixture();
        const completion = loadAuxiliaryModules(f.modules, f.memory, f.imports, async name => {
            if (name === "app.entrypoints.wasm") return new Response(f.bytes[name]);
            if (failure === "missing") return new Response(null, { status: 404 });
            return new Response(failure === "invalid" ? new Uint8Array([0]) : new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0]));
        }, f.notify, error => f.errors.push(error));
        await assert.rejects(completion, /app\.1\.wasm/);
        assert.equal(f.errors.length, 1);
        assert.equal(f.notifications(), 1);
        assert.equal(f.view.getUint32(f.payloadPtr + 28, true), 0);
    });
}

test("browser: rejects corrupt metadata before loading sidecars", () => {
    const f = fixture();
    assert.deepEqual(readAuxiliaryModuleMetadata({ exports: {} }, f.memory, 256, 512, 5), []);
    assert.throws(() => readAuxiliaryModuleMetadata({ exports: { getAuxiliaryModuleMetadata: 1 } }, f.memory, 256, 512, 5), /export/);
    f.view.setUint32(f.payloadPtr + 8, 0, true);
    f.view.setUint32(f.payloadPtr + 4, 6, true);
    assert.throws(() => readAuxiliaryModuleMetadata(f.primary, f.memory, 256, 512, 5), /table range/);
    f.view.setUint32(f.payloadPtr + 4, 4, true);
    f.view.setUint32(f.payloadPtr + 8, 1000, true);
    assert.throws(() => readAuxiliaryModuleMetadata(f.primary, f.memory, 256, 512, 5), /truncated/);
    f.view.setUint32(f.payloadPtr + 8, 1, true);
    new Uint8Array(f.memory.buffer).set(new TextEncoder().encode("../bad.wasm\0"), f.payloadPtr + 96);
    assert.throws(() => readAuxiliaryModuleMetadata(f.primary, f.memory, 256, 512, 5), /filename/);
});

for (const stage of ["eager", "missing", "delay-entrypoints", "delay-body", "all-hot"]) {
    test(`corerun: activation is deferred until probe returns; ${stage}`, async () => {
        const f = fixture();
        if (stage === "all-hot") {
            f.view.setUint32(f.payloadPtr + 4, 5, true);
            f.view.setUint32(f.payloadPtr + 20, 5, true);
            f.view.setUint32(f.payloadPtr + 24, 0, true);
            f.bytes["app.1.wasm"] = auxiliaryWasm(28, false);
        }
        let probed = false;
        let library;
        const timers = [];
        const env = stage === "delay-entrypoints"
            ? { DOTNET_WasmAuxiliaryEntryPointsDelayMilliseconds: "100" }
            : stage === "delay-body" ? { DOTNET_WasmAuxiliaryModuleDelayMilliseconds: "100" } : {};
        const context = {
            LibraryManager: { library: {} }, autoAddDeps: () => {}, addToLibrary: value => library = value,
            WebAssembly, TextDecoder, Error, DataView, Uint8Array, Set, Promise, ENV: env,
            setTimeout: callback => timers.push(callback),
            wasmMemory: f.memory,
            wasmExports: { dotnet_wasm_notify_auxiliary_entrypoints_activated: f.notify },
            FS: { readFile: path => {
                assert.ok(probed);
                const name = path.substring(path.lastIndexOf("/") + 1);
                if (stage === "missing" && name === "app.1.wasm") throw new Error("Missing file");
                return f.bytes[name];
            } },
            err: message => f.errors.push(message),
        };
        vm.runInNewContext(corerunSource, context);
        context.CORERUN = library.$CORERUN;
        context.CORERUN.queueAuxiliaryModules(f.primary, f.imports, f.payloadPtr, 512, 5, "/app/app.wasm");
        assert.equal(f.view.getUint32(f.payloadPtr, true), 0);
        probed = true;
        if (stage === "missing") {
            await assert.rejects(context.CORERUN.waitForAuxiliaryModules(), /Missing file/);
            assert.equal(f.errors.length, 1);
        } else {
            if (stage.startsWith("delay")) {
                const pendingOffset = stage === "delay-body" ? 28 : 0;
                await until(() => f.view.getUint32(f.payloadPtr + (28 - pendingOffset), true) === 1);
                assert.equal(f.view.getUint32(f.payloadPtr + pendingOffset, true), 0);
                assert.equal(timers.length, 1);
                timers[0]();
            }

            await context.CORERUN.waitForAuxiliaryModules();
            assert.equal(f.view.getUint32(f.payloadPtr + 28, true), 1);
            if (stage === "all-hot")
                assert.equal(f.table.get(4), null);
            else
                assert.equal(f.table.get(4)(), 42);
        }
        assert.equal(f.notifications(), 1);
    });
}

for (const [first, rawFs] of [["ENTRYPOINTS", false], ["MODULES", false], ["ENTRYPOINTS", true], ["MODULES", true]]) {
    test(`corerun: ${rawFs ? "raw FS" : "stdout"} gate releases ${first} first on a later event-loop turn`, async () => {
        const f = fixture();
        const timers = [];
        const printed = [];
        const reads = [];
        let finishFirstCompile;
        const firstCompile = new Promise(resolve => finishFirstCompile = resolve);
        const firstFilename = first === "MODULES" ? "app.1.wasm" : "app.entrypoints.wasm";
        let library;
        const context = {
            LibraryManager: { library: {} }, autoAddDeps: () => {}, addToLibrary: value => library = value,
            WebAssembly: { instantiate: async (bytes, imports) => {
                if (bytes === f.bytes[firstFilename]) await firstCompile;
                return WebAssembly.instantiate(bytes, imports);
            } },
            TextDecoder, Error, DataView, Uint8Array, Set, Promise,
            ENV: { DOTNET_WasmAuxiliaryManualActivation: "1" },
            Module: { preRun: [] },
            setTimeout: callback => timers.push(callback),
            out: text => printed.push(text),
            wasmMemory: f.memory,
            wasmExports: { dotnet_wasm_notify_auxiliary_entrypoints_activated: f.notify },
            FS: {
                write: (stream, buffer, offset, length) => {
                    printed.push(new TextDecoder().decode(buffer.subarray(offset, offset + length)));
                    return length;
                },
                readFile: path => {
                    const name = path.substring(path.lastIndexOf("/") + 1);
                    reads.push(name);
                    return f.bytes[name];
                },
            },
            err: message => f.errors.push(message),
        };
        vm.runInNewContext(corerunSource, context);
        context.CORERUN = library.$CORERUN;
        context.CORERUN.installAuxiliaryModuleGates();
        context.Module.preRun.forEach(callback => callback());
        context.CORERUN.queueAuxiliaryModules(f.primary, f.imports, f.payloadPtr, 512, 5, "/app/app.wasm");
        await new Promise(resolve => setImmediate(resolve));
        assert.equal(f.view.getUint32(f.payloadPtr, true), 0);
        assert.equal(f.view.getUint32(f.payloadPtr + 28, true), 0);
        const emit = stage => {
            const marker = "AUXILIARY_R2R_ACTIVATE_" + stage;
            if (rawFs) {
                const bytes = new TextEncoder().encode("xx" + marker + "\n");
                context.FS.write({ fd: 1 }, bytes, 2, 9);
                context.FS.write({ fd: 1 }, bytes, 11, bytes.length - 11);
            } else {
                context.out(marker);
            }
        };
        emit(first);
        assert.equal(timers.length, 1);
        assert.equal(f.view.getUint32(f.payloadPtr + (first === "MODULES" ? 28 : 0), true), 0);
        timers.shift()();
        const second = first === "MODULES" ? "ENTRYPOINTS" : "MODULES";
        emit(second);
        timers.shift()();
        await new Promise(resolve => setImmediate(resolve));
        assert.deepEqual(reads, [firstFilename]);
        assert.equal(f.view.getUint32(f.payloadPtr, true), 0);
        assert.equal(f.view.getUint32(f.payloadPtr + 28, true), 0);
        finishFirstCompile();
        await context.CORERUN.waitForAuxiliaryModules();
        assert.deepEqual(reads, [firstFilename, second === "MODULES" ? "app.1.wasm" : "app.entrypoints.wasm"]);
        assert.equal(f.notifications(), 1);
        assert.equal(f.table.get(4)(), 42);
        const newline = rawFs ? "\n" : "";
        assert.equal(printed.join(""), "AUXILIARY_R2R_ACTIVATE_" + first + newline + "AUXILIARY_R2R_ACTIVATE_" + second + newline);
    });
}
