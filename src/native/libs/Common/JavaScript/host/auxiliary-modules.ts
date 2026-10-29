// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

type AuxiliaryModule = { name: string, statusAddress: number, entrypoints: boolean };
export type AuxiliaryModuleSource = Response | WebAssembly.Module;

export function readAuxiliaryModuleMetadata(instance: WebAssembly.Instance, memory: WebAssembly.Memory, payloadPtr: number, payloadSize: number, tableSize: number): AuxiliaryModule[] {
    const getMetadata = instance.exports.getAuxiliaryModuleMetadata;
    if (getMetadata === undefined) {
        return [];
    }
    if (typeof getMetadata !== "function") {
        throw new Error("Invalid getAuxiliaryModuleMetadata export.");
    }
    const address = getMetadata() >>> 0;
    if (address === 0) {
        return [];
    }
    const heap = new Uint8Array(memory.buffer);
    const payloadEnd = payloadPtr + payloadSize;
    if (payloadEnd > heap.length || address < payloadPtr || address % 4 !== 0 || address + 16 > payloadEnd) {
        throw new Error("Auxiliary R2R module metadata is outside the primary payload.");
    }
    const view = new DataView(memory.buffer);
    const count = view.getUint32(address + 8, true);
    if (count > (payloadEnd - address - 16) / 16) {
        throw new Error("Auxiliary R2R module metadata is truncated.");
    }
    const names = new Set<string>();
    function readModule(filenameRva: number, statusAddress: number, entrypoints: boolean): AuxiliaryModule {
        const status = view.getUint32(statusAddress, true);
        const start = payloadPtr + filenameRva;
        if (status > 1 || start < payloadPtr || start >= payloadEnd) {
            throw new Error("Invalid auxiliary R2R module metadata.");
        }
        let end = start;
        while (end < payloadEnd && heap[end] !== 0) end++;
        if (end === payloadEnd) {
            throw new Error("Unterminated auxiliary R2R module filename.");
        }
        const name = new TextDecoder("utf-8", { fatal: true }).decode(heap.subarray(start, end));
        if (!/^[^./\\][^/\\?#:]*\.wasm$/.test(name) || names.has(name)) {
            throw new Error(`Invalid or duplicate auxiliary R2R module filename '${name}'.`);
        }
        names.add(name);
        return { name, statusAddress, entrypoints };
    }
    const modules = [readModule(view.getUint32(address + 12, true), address, true)];
    let nextTableIndex = view.getUint32(address + 4, true);
    if (nextTableIndex > tableSize) {
        throw new Error("Invalid primary R2R module table range.");
    }
    for (let i = 0; i < count; i++) {
        const entry = address + 16 + i * 16;
        const tableBase = view.getUint32(entry + 4, true);
        const methodCount = view.getUint32(entry + 8, true);
        if (tableBase !== nextTableIndex || tableBase + methodCount > tableSize) {
            throw new Error("Invalid auxiliary R2R module table range.");
        }
        nextTableIndex += methodCount;
        modules.push(readModule(view.getUint32(entry, true), entry + 12, false));
    }
    return modules;
}

// Each operation settles independently: EntryPoints must not wait for a cold body (or vice versa).
// Settled errors are retained for test/host waiters and reported immediately, without unhandled rejections.
export function loadAuxiliaryModules(modules: AuxiliaryModule[], memory: WebAssembly.Memory, imports: WebAssembly.Imports, prepare: (name: string) => Promise<AuxiliaryModuleSource>, notifyEntryPoints: () => void, reportError: (error: Error) => void): Promise<void> {
    const operations = modules.map(async module => {
        try {
            if (new DataView(memory.buffer).getUint32(module.statusAddress, true) === 1) {
                return;
            }
            const source = await prepare(module.name);
            let instance: WebAssembly.Instance;
            if (source instanceof WebAssembly.Module) {
                // Chromium limits the synchronous constructor to modules below 8 MB, even when compiled.
                instance = await WebAssembly.instantiate(source, imports);
            } else {
                if (!source.ok) {
                    throw new Error(`HTTP status: ${source.status} ${source.statusText}`);
                }
                instance = (await WebAssembly.instantiate(await source.arrayBuffer(), imports)).instance;
            }
            const activate = instance.exports.ActivateModule;
            if (typeof activate !== "function") {
                throw new Error("Missing ActivateModule export.");
            }
            // A second load may have completed while this instance was being compiled.
            if (new DataView(memory.buffer).getUint32(module.statusAddress, true) === 0) {
                activate();
                if (new DataView(memory.buffer).getUint32(module.statusAddress, true) !== 1) {
                    throw new Error("ActivateModule did not publish its activation status.");
                }
                if (module.entrypoints) {
                    notifyEntryPoints();
                }
            }
        } catch (reason) {
            const error = new Error(`Failed to load auxiliary R2R module '${module.name}': ${reason instanceof Error ? reason.message : String(reason)}`, { cause: reason });
            reportError(error);
            return error;
        }
    });
    const completion = Promise.all(operations).then(errors => {
        const error = errors.find(error => error !== undefined);
        if (error) throw error;
    });
    // Keep the rejecting completion awaitable while marking background failures as observed.
    void completion.catch(() => {});
    return completion;
}
