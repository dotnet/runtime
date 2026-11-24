// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VfsAsset, VoidPtr, VoidPtrPtr } from "../types";
import { _ems_ } from "../ems-ambient";

import { browserVirtualAppBase, sizeOfPtr } from "../per-module";

const hasInstantiateStreaming = typeof WebAssembly !== "undefined" && typeof WebAssembly.instantiateStreaming === "function";
const loadedAssemblies: Map<string, { ptr: number, length: number }> = new Map();
const bundledAssemblyPaths: string[] = [];
// Self-installing wrapper version; keep in sync with WebcilConstants.WASM_WRAPPER_VERSION_SELF_INSTALLING.
const webcilWrapperVersion = 2;

export function registerPdbBytes(bytes: Uint8Array, virtualPath: string, shortName: string) {
    const sp = _ems_.stackSave();
    try {
        const ptrPtr = _ems_.stackAlloc(sizeOfPtr);
        if (_ems_._posix_memalign(ptrPtr as any, 16, bytes.length)) {
            throw new Error("posix_memalign failed for PDB");
        }

        const ptr = _ems_.HEAPU32[ptrPtr as any >>> 2];
        _ems_.HEAPU8.set(bytes, ptr >>> 0);

        _ems_.dotnetLogger.debug(`Registered PDB '${virtualPath}' (shortName: '${shortName}') at ${ptr.toString(16)} length ${bytes.length}`);
        loadedAssemblies.set(virtualPath, { ptr, length: bytes.length });
        loadedAssemblies.set(shortName, { ptr, length: bytes.length });
    } finally {
        _ems_.stackRestore(sp);
    }
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

export async function instantiateWebcilModule(webcilPromise: Promise<Response>, memory: WebAssembly.Memory, virtualPath: string, tableSize?: number, payloadSize?: number, isBundle?: boolean): Promise<void> {
    // Boot-config sizes let us reserve the payload and table ranges before streaming instantiation.
    // Assets without a tableSize are IL-only images.
    if (typeof payloadSize !== "number" || payloadSize === 0) {
        throw new Error(`Webcil asset '${virtualPath}' is missing payloadSize in the boot config.`);
    }
    const tableEntries = typeof tableSize === "number" ? tableSize : 0;
    if (isBundle && tableEntries !== 0) {
        throw new Error(`Webcil bundle '${virtualPath}' cannot require a function table.`);
    }

    const res = await checkWebcilResponse(webcilPromise, virtualPath);
    let payloadPtr = 0;
    try {
        payloadPtr = allocWebcilPayload(payloadSize);
        const imports: WebAssembly.Imports = { webcil: buildWebcilImports(memory, payloadPtr, tableEntries) };
        let instance: WebAssembly.Instance;
        const contentType = res.headers && res.headers.get ? res.headers.get("Content-Type") : undefined;
        const streamingOk = hasInstantiateStreaming && typeof globalThis.Response === "function" && res instanceof globalThis.Response && contentType === "application/wasm";
        if (streamingOk) {
            instance = (await WebAssembly.instantiateStreaming(res, imports)).instance;
        } else {
            const data = await res.arrayBuffer();
            instance = (await WebAssembly.instantiate(data, imports)).instance;
        }
        finishWebcilInstance(instance, payloadPtr, payloadSize, tableEntries, virtualPath, isBundle);
    } catch (err) {
        // Instantiation failed after the payload buffer was allocated; free it to avoid leaking
        // unmanaged memory. (A grown R2R table cannot be shrunk back, but a failed R2R instantiate is fatal.)
        if (payloadPtr !== 0) {
            _ems_._free(payloadPtr as any);
        }
        throw err;
    }
}

async function checkWebcilResponse(webcilPromise: Promise<Response>, virtualPath: string): Promise<Response> {
    const res = await webcilPromise;
    if (!res || res.ok === false) {
        throw new Error(`Failed to load Webcil module '${virtualPath}'. HTTP status: ${(res as Response)?.status} ${(res as Response)?.statusText}`);
    }
    return res;
}

// Allocates a 16-byte-aligned buffer for the Webcil payload. The pointer is heap memory that
// outlives the stack frame, so it can be passed as the __memory_base import.
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

// Builds the `webcil` import object. Every image imports the runtime's memory and the __memory_base
// global its active payload segment is placed at. R2R images (tableSize > 0) also import the runtime's
// stack pointer, exception tag, indirect-call table and __table_base; this also grows the table.
// These import names and the webcilVersion/patchWebcilHeader handshake in finishWebcilInstance are the
// self-installing Webcil-in-Wasm host ABI defined by crossgen's WebCilObjectWriter
// (src/coreclr/tools/Common/Compiler/ObjectWriter/WebCilObjectWriter.cs) and the IL-only wrapper
// (src/tasks/Microsoft.NET.WebAssembly.Webcil/WebcilWasmWrapper.cs). Keep in sync with the corerun host
// (src/coreclr/hosts/corerun/wasm/libCorerun.js, BrowserHost_ExternalAssemblyProbe). The browser
// loader receives payloadSize/tableSize from boot config rather than parsing the wrapper.
function buildWebcilImports(memory: WebAssembly.Memory, payloadPtr: number, tableSize: number): Record<string, WebAssembly.ImportValue> {
    const webcilImports: Record<string, WebAssembly.ImportValue> = {
        memory,
        __memory_base: new WebAssembly.Global({ value: "i32", mutable: false }, payloadPtr),
    };
    if (tableSize > 0) {
        const stackPointer = _ems_.wasmExports?.__stack_pointer;
        const rtlRestoreContextTag = _ems_.wasmExports?.__coreclr_wasm_rtlrestorecontext_tag;
        const asyncContinuation = _ems_.wasmExports?.__async_continuation;
        if (!stackPointer) {
            throw new Error("__stack_pointer was not preserved by the linker or optimizer");
        }
        if (!rtlRestoreContextTag) {
            throw new Error("__coreclr_wasm_rtlrestorecontext_tag was not preserved by the linker or optimizer");
        }
        if (!asyncContinuation) {
            throw new Error("__async_continuation was not preserved by the linker or optimizer");
        }
        const tableStartIndex = _ems_.wasmTable.length;
        _ems_.wasmTable.grow(tableSize);
        webcilImports.__stack_pointer = stackPointer;
        webcilImports.__coreclr_wasm_rtlrestorecontext_tag = rtlRestoreContextTag as unknown as WebAssembly.ImportValue;
        webcilImports.__async_continuation = asyncContinuation as unknown as WebAssembly.ImportValue;
        webcilImports.__indirect_function_table = _ems_.wasmTable;
        webcilImports.__table_base = new WebAssembly.Global({ value: "i32", mutable: false }, tableStartIndex);
    }
    return webcilImports;
}

// Records the table base for R2R images and registers the loaded image for
// BrowserHost_ExternalAssemblyProbe. The engine has already installed the payload (and table slice)
// from the module's active segments during instantiation.
function finishWebcilInstance(instance: WebAssembly.Instance, payloadPtr: number, payloadSize: number, tableSize: number, virtualPath: string, isBundle?: boolean): void {
    const webcilVersion = (instance.exports.webcilVersion as WebAssembly.Global).value;
    if (webcilVersion !== webcilWrapperVersion) {
        throw new Error(`Webcil asset '${virtualPath}' has unsupported Webcil wrapper version ${webcilVersion}; expected ${webcilWrapperVersion}.`);
    }

    if (tableSize > 0) {
        // The header's tableBase field lives in linear memory, so no segment can supply it.
        const patchWebcilHeader = instance.exports.patchWebcilHeader as ((ptr: number, size: number) => void) | undefined;
        if (typeof patchWebcilHeader !== "function") {
            throw new Error(`Webcil R2R asset '${virtualPath}' does not export patchWebcilHeader.`);
        }
        patchWebcilHeader(payloadPtr, payloadSize);
    }

    if (isBundle) {
        registerWebcilBundle(payloadPtr, payloadSize, virtualPath);
        return;
    }

    const name = virtualPath.startsWith(browserVirtualAppBase)
        ? virtualPath.substring(browserVirtualAppBase.length)
        : virtualPath.substring(virtualPath.lastIndexOf("/") + 1);
    _ems_.dotnetLogger.debug(`Registered Webcil assembly '${virtualPath}' (name: '${name}') at ${payloadPtr.toString(16)} length ${payloadSize}`);
    loadedAssemblies.set(virtualPath, { ptr: payloadPtr, length: payloadSize });
    loadedAssemblies.set(name, { ptr: payloadPtr, length: payloadSize });
}

export function getBundledAssemblyPaths(): readonly string[] {
    return bundledAssemblyPaths;
}

function findLoadedAssembly(path: string): { ptr: number, length: number } | undefined {
    const lastSlash = path.lastIndexOf("/");
    return loadedAssemblies.get(path) ?? (lastSlash >= 0 ? loadedAssemblies.get(path.substring(lastSlash + 1)) : undefined);
}

export function getLoadedAssemblyBytes(path: string): Uint8Array | undefined {
    const assembly = findLoadedAssembly(path);
    return assembly === undefined ? undefined : _ems_.HEAPU8.slice(assembly.ptr, assembly.ptr + assembly.length);
}

function registerWebcilBundle(payloadPtr: number, payloadSize: number, virtualPath: string): void {
    const invalid = () => new Error(`Invalid Webcil bundle '${virtualPath}'.`);
    if (payloadSize < 20) throw invalid();

    const view = new DataView(_ems_.HEAPU8.buffer, payloadPtr, payloadSize);
    if (view.getUint32(0, true) !== 0x42496257 || view.getUint16(4, true) !== 1 || view.getUint16(6, true) !== 0) throw invalid();

    const count = view.getUint32(8, true);
    const stringTableOffset = view.getUint32(12, true);
    const stringTableSize = view.getUint32(16, true);
    if (count > Math.floor((payloadSize - 20) / 16) ||
        stringTableOffset < 20 + count * 16 ||
        stringTableOffset > payloadSize ||
        stringTableSize > payloadSize - stringTableOffset) throw invalid();

    const stringsEnd = stringTableOffset + stringTableSize;
    const decoder = new TextDecoder("utf-8", { fatal: true });
    const names = new Set<string>();
    const entries: { name: string, path: string, offset: number, size: number }[] = [];
    for (let i = 0; i < count; i++) {
        const entryOffset = 20 + i * 16;
        const nameOffset = view.getUint32(entryOffset, true);
        const nameLength = view.getUint32(entryOffset + 4, true);
        const imageOffset = view.getUint32(entryOffset + 8, true);
        const imageSize = view.getUint32(entryOffset + 12, true);
        if (nameLength === 0 || nameOffset > stringTableSize || nameLength > stringTableSize - nameOffset ||
            imageOffset % 16 !== 0 || imageOffset < stringsEnd || imageOffset > payloadSize ||
            imageSize < 32 || imageSize > payloadSize - imageOffset ||
            view.getUint32(imageOffset, true) !== 0x4c496257 ||
            view.getUint16(imageOffset + 4, true) !== 1 || view.getUint16(imageOffset + 6, true) !== 0 ||
            view.getUint32(imageOffset + 28, true) !== 0) throw invalid();

        const name = decoder.decode(new Uint8Array(_ems_.HEAPU8.buffer, payloadPtr + stringTableOffset + nameOffset, nameLength));
        if (!name.endsWith(".dll") || name.includes("\\") || name.split("").some(character => character.charCodeAt(0) < 0x20) ||
            name.split("/").some(part => part === "" || part === "." || part === "..")) throw invalid();
        const shortName = name.substring(name.lastIndexOf("/") + 1);
        if (names.has(shortName.toLowerCase())) throw invalid();
        names.add(shortName.toLowerCase());
        entries.push({ name, path: browserVirtualAppBase + name, offset: imageOffset, size: imageSize });
    }

    const byOffset = [...entries].sort((a, b) => a.offset - b.offset);
    for (let i = 1; i < byOffset.length; i++) {
        if (byOffset[i - 1].size > byOffset[i].offset - byOffset[i - 1].offset) throw invalid();
    }

    for (const entry of entries) {
        const assembly = { ptr: payloadPtr + entry.offset, length: entry.size };
        const shortName = entry.name.substring(entry.name.lastIndexOf("/") + 1);
        loadedAssemblies.set(entry.path, assembly);
        loadedAssemblies.set(entry.name, assembly);
        loadedAssemblies.set(shortName, assembly);
        bundledAssemblyPaths.push(entry.path);
    }
}

export function BrowserHost_ExternalAssemblyProbe(pathPtr: CharPtr, outDataStartPtr: VoidPtrPtr, outSize: VoidPtr): boolean {
    const path = _ems_.UTF8ArrayToString(_ems_.dotnetApi.localHeapViewU8(), pathPtr as any);
    const assembly = findLoadedAssembly(path);
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
