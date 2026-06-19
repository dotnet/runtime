// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VfsAsset, VoidPtr, VoidPtrPtr } from "../types";
import { _ems_ } from "../ems-ambient";

import { browserVirtualAppBase, sizeOfPtr } from "../per-module";

const hasInstantiateStreaming = typeof WebAssembly !== "undefined" && typeof WebAssembly.instantiateStreaming === "function";
const loadedAssemblies: Map<string, { ptr: number, length: number }> = new Map();

export function registerPdbBytes(bytes: Uint8Array, virtualPath: string) {
    const lastSlash = virtualPath.lastIndexOf("/");
    let parentDirectory = lastSlash > 0
        ? virtualPath.substring(0, lastSlash)
        : browserVirtualAppBase;
    let fileName = lastSlash > 0 ? virtualPath.substring(lastSlash + 1) : virtualPath;
    if (fileName.startsWith("/")) {
        fileName = fileName.substring(1);
    }
    if (!parentDirectory.startsWith("/")) {
        parentDirectory = browserVirtualAppBase + parentDirectory;
    }

    _ems_.dotnetLogger.debug(`Registering PDB '${fileName}' in directory '${parentDirectory}'`);
    _ems_.FS.createPath("/", parentDirectory, true, true);
    _ems_.FS.createDataFile(parentDirectory, fileName, bytes, true /* canRead */, true /* canWrite */, true /* canOwn */);
}

export function registerDllBytes(bytes: Uint8Array, virtualPath: string, shortName: string) {
    const sp = _ems_.stackSave();
    try {
        const ptrPtr = _ems_.stackAlloc(sizeOfPtr);
        if (_ems_._posix_memalign(ptrPtr as any, 16, bytes.length)) {
            throw new Error("posix_memalign failed");
        }

        const ptr = _ems_.HEAPU32[ptrPtr as any >>> 2];
        _ems_.HEAPU8.set(bytes, ptr >>> 0);

        _ems_.dotnetLogger.debug(`Registered assembly '${virtualPath}' (shortName: '${shortName}') at ${ptr.toString(16)} length ${bytes.length}`);
        loadedAssemblies.set(virtualPath, { ptr, length: bytes.length });
        loadedAssemblies.set(shortName, { ptr, length: bytes.length });
    } finally {
        _ems_.stackRestore(sp);
    }
}

export async function instantiateWebcilModule(webcilPromise: Promise<Response>, memory: WebAssembly.Memory, virtualPath: string, tableSize?: number, payloadSize?: number): Promise<void> {
    // The boot config carries payloadSize for every webcil asset (and tableSize for R2R images), so
    // the loader never buffers the bytes, parses the data section or calls getWebcilSize. Assets
    // without a tableSize are plain (Webcil wrapper version 0) images.
    if (typeof payloadSize !== "number" || payloadSize === 0) {
        throw new Error(`Webcil asset '${virtualPath}' is missing payloadSize in the boot config.`);
    }
    const tableEntries = typeof tableSize === "number" ? tableSize : 0;

    const res = await checkWebcilResponse(webcilPromise, virtualPath);
    const payloadPtr = allocWebcilPayload(payloadSize);
    const imports: WebAssembly.Imports = { webcil: buildWebcilImports(memory, payloadPtr, tableEntries) };

    let instance: WebAssembly.Instance;
    const contentType = res.headers && res.headers.get ? res.headers.get("Content-Type") : undefined;
    const streamingOk = hasInstantiateStreaming && typeof globalThis.Response === "function" && res instanceof globalThis.Response && contentType === "application/wasm";
    if (streamingOk) {
        const instantiated = await WebAssembly.instantiateStreaming(res, imports);
        instance = instantiated.instance;
    } else {
        const data = await res.arrayBuffer();
        const instantiated = await WebAssembly.instantiate(data, imports);
        instance = instantiated.instance;
    }
    finishWebcilInstance(instance, payloadPtr, payloadSize, tableEntries, virtualPath);
}

async function checkWebcilResponse(webcilPromise: Promise<Response>, virtualPath: string): Promise<Response> {
    const res = await webcilPromise;
    if (!res || res.ok === false) {
        throw new Error(`Failed to load Webcil module '${virtualPath}'. HTTP status: ${(res as Response)?.status} ${(res as Response)?.statusText}`);
    }
    return res;
}

// Allocates a 16-byte-aligned buffer for the Webcil payload. The pointer is heap memory that
// outlives the stack frame, so it can be passed as the imageBase import.
function allocWebcilPayload(payloadSize: number): number {
    const sp = _ems_.stackSave();
    try {
        const ptrPtr = _ems_.stackAlloc(sizeOfPtr);
        if (_ems_._posix_memalign(ptrPtr as any, 16, payloadSize)) {
            throw new Error("posix_memalign failed for Webcil payload");
        }
        return _ems_.HEAPU32[ptrPtr as any >>> 2];
    } finally {
        _ems_.stackRestore(sp);
    }
}

// Builds the `webcil` import object. For R2R images (tableSize > 0) the module imports the runtime's
// stack pointer, exception tag, indirect-call table and base globals; this also grows the table.
// Mirror the corerun host (src/coreclr/hosts/corerun/wasm/libCorerun.js).
function buildWebcilImports(memory: WebAssembly.Memory, payloadPtr: number, tableSize: number): Record<string, WebAssembly.ImportValue> {
    const webcilImports: Record<string, WebAssembly.ImportValue> = { memory };
    if (tableSize > 0) {
        const stackPointer = _ems_.wasmExports?.__stack_pointer;
        const rtlRestoreContextTag = _ems_.wasmExports?.__coreclr_wasm_rtlrestorecontext_tag;
        if (!stackPointer) {
            throw new Error("__stack_pointer was not preserved by the linker or optimizer");
        }
        if (!rtlRestoreContextTag) {
            throw new Error("__coreclr_wasm_rtlrestorecontext_tag was not preserved by the linker or optimizer");
        }
        const tableStartIndex = _ems_.wasmTable.length;
        _ems_.wasmTable.grow(tableSize);
        webcilImports.stackPointer = stackPointer;
        webcilImports.rtlRestoreContextTag = rtlRestoreContextTag as unknown as WebAssembly.ImportValue;
        webcilImports.table = _ems_.wasmTable;
        webcilImports.tableBase = new WebAssembly.Global({ value: "i32", mutable: false }, tableStartIndex);
        webcilImports.imageBase = new WebAssembly.Global({ value: "i32", mutable: false }, payloadPtr);
    }
    return webcilImports;
}

// Copies the payload into the allocated buffer, fills the R2R table (if any) and registers the
// loaded image for BrowserHost_ExternalAssemblyProbe.
function finishWebcilInstance(instance: WebAssembly.Instance, payloadPtr: number, payloadSize: number, tableSize: number, virtualPath: string): void {
    const webcilVersion = (instance.exports.webcilVersion as WebAssembly.Global).value;
    if (webcilVersion > 1 || webcilVersion < 0) {
        throw new Error(`Unsupported Webcil version: ${webcilVersion}`);
    }

    const getWebcilPayload = instance.exports.getWebcilPayload as (ptr: number, size: number) => void;
    getWebcilPayload(payloadPtr, payloadSize);
    if (tableSize > 0) {
        const fillWebcilTable = instance.exports.fillWebcilTable as () => void;
        fillWebcilTable();
    }

    const name = virtualPath.startsWith(browserVirtualAppBase)
        ? virtualPath.substring(browserVirtualAppBase.length)
        : virtualPath.substring(virtualPath.lastIndexOf("/") + 1);
    _ems_.dotnetLogger.debug(`Registered Webcil assembly '${virtualPath}' (name: '${name}') at ${payloadPtr.toString(16)} length ${payloadSize}`);
    loadedAssemblies.set(virtualPath, { ptr: payloadPtr, length: payloadSize });
    loadedAssemblies.set(name, { ptr: payloadPtr, length: payloadSize });
}

export function BrowserHost_ExternalAssemblyProbe(pathPtr: CharPtr, outDataStartPtr: VoidPtrPtr, outSize: VoidPtr): boolean {
    const path = _ems_.UTF8ArrayToString(_ems_.dotnetApi.localHeapViewU8(), pathPtr as any);
    const assembly = loadedAssemblies.get(path);
    if (assembly) {
        _ems_.HEAPU32[outDataStartPtr as any >>> 2] = assembly.ptr;
        // int64_t target
        _ems_.HEAPU32[outSize as any >>> 2] = assembly.length;
        _ems_.HEAPU32[((outSize as any) + 4) >>> 2] = 0;
        return true;
    }
    _ems_.dotnetLogger.debug(`Assembly not found: '${path}'`);
    _ems_.HEAPU32[outDataStartPtr as any >>> 2] = 0;
    _ems_.HEAPU32[outSize as any >>> 2] = 0;
    _ems_.HEAPU32[((outSize as any) + 4) >>> 2] = 0;
    return false;
}

export function loadIcuData(bytes: Uint8Array) {
    const sp = _ems_.stackSave();
    try {
        const ptrPtr = _ems_.stackAlloc(sizeOfPtr);
        if (_ems_._posix_memalign(ptrPtr as any, 16, bytes.length)) {
            throw new Error("posix_memalign failed for ICU data");
        }

        const ptr = _ems_.HEAPU32[ptrPtr as any >>> 2];
        _ems_.HEAPU8.set(bytes, ptr >>> 0);

        const result = _ems_._wasm_load_icu_data(ptr as unknown as VoidPtr);
        if (!result) {
            throw new Error("Failed to initialize ICU data");
        }
    } finally {
        _ems_.stackRestore(sp);
    }
}

export function installVfsFile(bytes: Uint8Array, asset: VfsAsset) {
    const virtualName: string = typeof (asset.virtualPath) === "string"
        ? asset.virtualPath
        : asset.name;
    const lastSlash = virtualName.lastIndexOf("/");
    let parentDirectory = (lastSlash > 0)
        ? virtualName.substring(0, lastSlash)
        : browserVirtualAppBase;
    let fileName = (lastSlash > 0)
        ? virtualName.substring(lastSlash + 1)
        : virtualName;
    if (fileName.startsWith("/")) {
        fileName = fileName.substring(1);
    }
    if (!parentDirectory.startsWith("/")) {
        parentDirectory = browserVirtualAppBase + parentDirectory;
    }

    _ems_.dotnetLogger.debug(`Creating file '${fileName}' in directory '${parentDirectory}'`);
    _ems_.FS.createPath("/", parentDirectory, true, true);
    _ems_.FS.createDataFile(parentDirectory, fileName, bytes, true /* canRead */, true /* canWrite */, true /* canOwn */);
}

export async function instantiateWasm(wasmPromise: Promise<Response>, imports: WebAssembly.Imports): Promise<{ instance: WebAssembly.Instance; module: WebAssembly.Module; }> {
    let instance: WebAssembly.Instance;
    let module: WebAssembly.Module;
    try {
        const res = await checkResponseOk(wasmPromise);
        if (!hasInstantiateStreaming || !res.isStreamingOk) {
            const data = await res.arrayBuffer();
            module = await WebAssembly.compile(data);
            instance = await WebAssembly.instantiate(module, imports);
        } else {
            const instantiated = await WebAssembly.instantiateStreaming(wasmPromise, imports);
            instance = instantiated.instance;
            module = instantiated.module;
        }
        return { instance, module };
    } catch (err) {
        _ems_.dotnetApi.exit(1, err);
        throw err;
    }

    async function checkResponseOk(wasmPromise: Promise<Response> | undefined): Promise<Response & { isStreamingOk?: boolean }> {
        _ems_.dotnetAssert.check(wasmPromise, "WASM binary promise was not initialized");
        const res = (await wasmPromise) as Response & { isStreamingOk?: boolean };
        if (!res || res.ok === false) {
            throw new Error(`Failed to load WebAssembly module. HTTP status: ${res?.status} ${res?.statusText}`);
        }
        res.isStreamingOk = typeof globalThis.Response === "function" && res instanceof globalThis.Response;
        const contentType = res.headers && res.headers.get ? res.headers.get("Content-Type") : undefined;
        if (_ems_.ENVIRONMENT_IS_WEB && contentType !== "application/wasm") {
            _ems_.dotnetLogger.warn("WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.");
            res.isStreamingOk = false;
        }
        return res;
    }
}
