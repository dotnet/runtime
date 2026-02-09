// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VfsAsset, VoidPtr, VoidPtrPtr } from "./types";
import { _ems_ } from "../../../libs/Common/JavaScript/ems-ambient";

import { browserVirtualAppBase, ENVIRONMENT_IS_WEB } from "./per-module";

const hasInstantiateStreaming = typeof WebAssembly !== "undefined" && typeof WebAssembly.instantiateStreaming === "function";
const loadedAssemblies: Map<string, { ptr: number, length: number }> = new Map();
// eslint-disable-next-line @typescript-eslint/no-unused-vars
let wasmMemory: WebAssembly.Memory = undefined as any;
// eslint-disable-next-line @typescript-eslint/no-unused-vars
let wasmMainTable: WebAssembly.Table = undefined as any;

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function registerPdbBytes(bytes: Uint8Array, virtualPath: string) {
    // WASM-TODO: https://github.com/dotnet/runtime/issues/122921
}

export function registerDllBytes(bytes: Uint8Array, virtualPath: string) {
    const sp = _ems_.stackSave();
    try {
        const sizeOfPtr = 4;
        const ptrPtr = _ems_.stackAlloc(sizeOfPtr);
        if (_ems_._posix_memalign(ptrPtr as any, 16, bytes.length)) {
            throw new Error("posix_memalign failed");
        }

        const ptr = _ems_.HEAPU32[ptrPtr as any >>> 2];
        _ems_.HEAPU8.set(bytes, ptr >>> 0);
        const name = virtualPath.substring(virtualPath.lastIndexOf("/") + 1);

        _ems_.dotnetLogger.debug(`Registered assembly '${virtualPath}' (name: '${name}') at ${ptr.toString(16)} length ${bytes.length}`);
        loadedAssemblies.set(virtualPath, { ptr, length: bytes.length });
        loadedAssemblies.set(name, { ptr, length: bytes.length });
    } finally {
        _ems_.stackRestore(sp);
    }
}

export function BrowserHost_ExternalAssemblyProbe(pathPtr: CharPtr, outDataStartPtr: VoidPtrPtr, outSize: VoidPtr) {
    const path = _ems_.UTF8ToString(pathPtr);
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
        const sizeOfPtr = 4;
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

export async function instantiateWasm(wasmPromise: Promise<Response>, imports: WebAssembly.Imports, isStreaming: boolean, isMainModule: boolean): Promise<{ instance: WebAssembly.Instance; module: WebAssembly.Module; }> {
    let instance: WebAssembly.Instance;
    let module: WebAssembly.Module;
    const res = await checkResponseOk(wasmPromise);
    if (!hasInstantiateStreaming || !isStreaming || !res.isStreamingOk) {
        const data = await res.arrayBuffer();
        module = await WebAssembly.compile(data);
        instance = await WebAssembly.instantiate(module, imports);
    } else {
        const instantiated = await WebAssembly.instantiateStreaming(wasmPromise, imports);
        instance = instantiated.instance;
        module = instantiated.module;
    }
    if (isMainModule) {
        wasmMemory = instance.exports.memory as WebAssembly.Memory;
        wasmMainTable = instance.exports.__indirect_function_table as WebAssembly.Table;
    }
    return { instance, module };

    async function checkResponseOk(wasmPromise: Promise<Response> | undefined): Promise<Response & { isStreamingOk?: boolean }> {
        _ems_.dotnetAssert.check(wasmPromise, "WASM binary promise was not initialized");
        const res = (await wasmPromise) as Response & { isStreamingOk?: boolean };
        if (!res || res.ok === false) {
            throw new Error(`Failed to load WebAssembly module. HTTP status: ${res?.status} ${res?.statusText}`);
        }
        res.isStreamingOk = typeof globalThis.Response === "function" && res instanceof globalThis.Response;
        const contentType = res.headers && res.headers.get ? res.headers.get("Content-Type") : undefined;
        if (ENVIRONMENT_IS_WEB && contentType !== "application/wasm") {
            _ems_.dotnetLogger.warn("WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.");
            res.isStreamingOk = false;
        }
        return res;
    }
}
