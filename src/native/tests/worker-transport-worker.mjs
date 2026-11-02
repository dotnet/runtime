// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { readFile } from "node:fs/promises";
import { createContext, SourceTextModule, SyntheticModule } from "node:vm";
import { isMainThread, parentPort, workerData } from "node:worker_threads";
import ts from "typescript";

const sourceDirectory = new URL("../libs/Common/JavaScript/loader/", import.meta.url);

export function seedNativeRequestIds(specifier, source, seed) {
    if (specifier !== "./worker-native" || seed === undefined) return source;
    const marker = "let nextId = 1;";
    if (!source.includes(marker) || source.indexOf(marker) !== source.lastIndexOf(marker)) {
        throw new Error("Native request-ID seed no longer matches implementation");
    }
    return source.replace(marker, `let nextId = ${seed};`);
}

export function createWorkerModuleLoader(context, stubs, transform = (_specifier, source) => source) {
    const cache = new Map();
    function getModule(specifier) {
        if (!cache.has(specifier)) cache.set(specifier, loadModule(specifier));
        return cache.get(specifier);
    }
    async function loadModule(specifier) {
        if (stubs[specifier]) {
            const exports = stubs[specifier];
            return new SyntheticModule(Object.keys(exports), function () {
                for (const [key, value] of Object.entries(exports)) this.setExport(key, value);
            }, { context });
        }
        const source = await readFile(new URL(`${specifier}.ts`, sourceDirectory), "utf8");
        const output = ts.transpileModule(transform(specifier, source), {
            compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext },
        }).outputText;
        return new SourceTextModule(output, { context, identifier: specifier });
    }
    return async function load(specifier) {
        const module = await getModule(specifier);
        if (module.status === "unlinked") await module.link(getModule);
        return module;
    };
}

if (!isMainThread) {
    const context = createContext({ Atomics, SharedArrayBuffer, Error, setTimeout, clearTimeout });
    const load = createWorkerModuleLoader(context, { "../types": {} }, (specifier, source) =>
        seedNativeRequestIds(specifier, source, workerData.seed));
    const shared = await load("./shared-signaling");
    await shared.evaluate();
    const native = await load("./worker-native");
    await native.evaluate();
    const signalBuffer = shared.namespace.createSignalingBuffer();
    const signal = new Int32Array(signalBuffer);
    const nativeBuffer = native.namespace.createNativeCallBuffer();
    if (workerData.queueSeed !== undefined) {
        const words = new Int32Array(nativeBuffer);
        const second = words.length / 2;
        for (const index of [0, 1, second, second + 1]) Atomics.store(words, index, workerData.queueSeed | 0);
    }
    const memory = new WebAssembly.Memory({ initial: 2, maximum: 64, shared: true });
    let callbackReady = false;
    const channel = native.namespace.createNativeChannel(signal, nativeBuffer, true, (operation, first, second) => {
        switch (operation) {
            case 2:
                if (!callbackReady) throw new Error("Task completed before native callback initialization");
                return first === 1024 ? new Int32Array(memory.buffer)[first / 4] : first;
            case 8:
                if (!callbackReady) throw new Error("Background work reentered a managed import");
                return first;
            case 10: return first + second;
            case 11: return channel.invoke(10, first, second);
            case 12: throw new Error(`Native failure ${first}`);
            case 13: return channel.invoke(11, first, second);
            case 14: return memory.grow(first);
            case 15:
                new BigInt64Array(memory.buffer)[first / 8] += 1n;
                return first;
            default: throw new Error("Unknown mock native operation");
        }
    });
    channel.listen(error => {
        throw error;
    });
    parentPort.on("message", message => {
        if (message === "async-prefix" || message === "async-cancel-prefix") {
            channel.invoke(12);
            if (message === "async-cancel-prefix") channel.invoke(13);
            callbackReady = true;
        } else if (message === "stop") {
            channel.stop(new Error("stopped"));
            parentPort.close();
        }
    });
    parentPort.postMessage({ signalBuffer, nativeBuffer, memory });
}
