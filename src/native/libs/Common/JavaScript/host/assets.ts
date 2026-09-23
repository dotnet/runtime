// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VfsAsset, VoidPtr, VoidPtrPtr } from "../types";
import { _ems_ } from "../ems-ambient";

import { browserVirtualAppBase, sizeOfPtr } from "../per-module";

const hasInstantiateStreaming = typeof WebAssembly !== "undefined" && typeof WebAssembly.instantiateStreaming === "function";
const loadedAssemblies: Map<string, { ptr: number, length: number }> = new Map();
const WasmSectionData = 11;
const WasmDataSegmentActive = 0;
const WasmDataSegmentPassive = 1;
const WasmOpcodeGlobalGet = 0x23;
const WasmOpcodeI32Const = 0x41;
const WasmOpcodeEnd = 0x0B;

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
    // the loader validates the data section against the boot config rather than calling
    // getWebcilSize. Assets without a tableSize are plain (Webcil wrapper version 0) images.
    if (typeof payloadSize !== "number" || payloadSize === 0) {
        throw new Error(`Webcil asset '${virtualPath}' is missing payloadSize in the boot config.`);
    }
    const tableEntries = typeof tableSize === "number" ? tableSize : 0;

    const res = await checkWebcilResponse(webcilPromise, virtualPath);
    let payloadPtr = 0;
    try {
        let instantiateBuffer: ArrayBuffer | undefined;
        const contentType = res.headers && res.headers.get ? res.headers.get("Content-Type") : undefined;
        const streamingOk = hasInstantiateStreaming && typeof globalThis.Response === "function" && res instanceof globalThis.Response && contentType === "application/wasm";
        if (streamingOk) {
            const data = await res.clone().arrayBuffer();
            validateWebcilInWasmDataSegments(data, payloadSize, tableEntries, virtualPath);
        } else {
            instantiateBuffer = await res.arrayBuffer();
            validateWebcilInWasmDataSegments(instantiateBuffer, payloadSize, tableEntries, virtualPath);
        }

        payloadPtr = allocWebcilPayload(payloadSize);
        const imports: WebAssembly.Imports = { webcil: buildWebcilImports(memory, payloadPtr, tableEntries) };
        let instance: WebAssembly.Instance;
        if (streamingOk) {
            const instantiated = await WebAssembly.instantiateStreaming(res, imports);
            instance = instantiated.instance;
        } else {
            const instantiated = await WebAssembly.instantiate(instantiateBuffer!, imports);
            instance = instantiated.instance;
        }
        finishWebcilInstance(instance, payloadPtr, payloadSize, tableEntries, virtualPath);
    } catch (err) {
        // Instantiation failed after the payload buffer was allocated; free it to avoid leaking
        // unmanaged memory. (A grown R2R table cannot be shrunk back, but a failed R2R instantiate is fatal.)
        if (payloadPtr !== 0) {
            _ems_._free(payloadPtr as any);
        }
        throw err;
    }
}

function validateWebcilInWasmDataSegments(bufferSource: BufferSource, expectedPayloadSize: number, expectedTableSize: number, virtualPath: string): void {
    const bytes = asUint8Array(bufferSource);
    const headerView = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    if (bytes.length < 8 || headerView.getUint32(0, true) !== 0x6d736100 || headerView.getUint32(4, true) !== 1) {
        throw new Error(`Webcil asset '${virtualPath}' is not a valid WebAssembly module.`);
    }

    let offset = 8;
    while (offset < bytes.length) {
        const sectionCode = bytes[offset++];
        const sectionSizeInfo = readULEB128(bytes, offset);
        const sectionStart = sectionSizeInfo.offset;
        const sectionEnd = checkedAdd(sectionStart, sectionSizeInfo.value, bytes.length, "section");
        if (sectionCode !== WasmSectionData) {
            offset = sectionEnd;
            continue;
        }

        const segmentCountInfo = readULEB128(bytes, sectionStart, sectionEnd);
        if (segmentCountInfo.value !== 2) {
            throw new Error(`Webcil asset '${virtualPath}' has ${segmentCountInfo.value} data segments; expected 2.`);
        }

        const sizeSegment = readWebcilDataSegment(bytes, segmentCountInfo.offset, sectionEnd, false);
        if (sizeSegment.dataLength < 4) {
            throw new Error(`Webcil asset '${virtualPath}' data segment 0 is too small to hold payloadSize.`);
        }

        const sizeView = new DataView(bytes.buffer, bytes.byteOffset + sizeSegment.dataStart, sizeSegment.dataLength);
        const actualPayloadSize = sizeView.getUint32(0, true);
        const actualTableSize = sizeSegment.dataLength >= 8 ? sizeView.getUint32(4, true) : 0;
        if (actualPayloadSize !== expectedPayloadSize) {
            throw new Error(`Webcil asset '${virtualPath}' payloadSize mismatch: boot config has ${expectedPayloadSize}, wrapper has ${actualPayloadSize}.`);
        }
        if (actualTableSize !== expectedTableSize) {
            throw new Error(`Webcil asset '${virtualPath}' tableSize mismatch: boot config has ${expectedTableSize}, wrapper has ${actualTableSize}.`);
        }

        const payloadSegment = readWebcilDataSegment(bytes, sizeSegment.offset, sectionEnd, true);
        if (payloadSegment.dataLength !== expectedPayloadSize) {
            throw new Error(`Webcil asset '${virtualPath}' payload segment length mismatch: expected ${expectedPayloadSize}, found ${payloadSegment.dataLength}.`);
        }
        if (payloadSegment.offset !== sectionEnd) {
            throw new Error(`Webcil asset '${virtualPath}' has unexpected data after the payload segment.`);
        }
        return;
    }

    throw new Error(`Webcil asset '${virtualPath}' has no data section.`);
}

function asUint8Array(bufferSource: BufferSource): Uint8Array {
    if (bufferSource instanceof ArrayBuffer) {
        return new Uint8Array(bufferSource);
    }

    if (ArrayBuffer.isView(bufferSource)) {
        return new Uint8Array(bufferSource.buffer, bufferSource.byteOffset, bufferSource.byteLength);
    }

    throw new TypeError("Expected a BufferSource");
}

function readULEB128(bytes: Uint8Array, offset: number, limit: number = bytes.length): { value: number, offset: number } {
    let value = 0;
    let shift = 0;

    for (; ;) {
        if (offset >= limit) {
            throw new RangeError("Unexpected end of input while reading ULEB128.");
        }

        const b = bytes[offset++];
        value |= (b & 0x7f) << shift;

        if ((b & 0x80) === 0) {
            return { value, offset };
        }

        shift += 7;
        if (shift >= 35) {
            throw new RangeError("ULEB128 is too large for a u32.");
        }
    }
}

function readWebcilDataSegment(bytes: Uint8Array, offset: number, limit: number, allowActive: boolean): { dataStart: number, dataLength: number, offset: number } {
    if (offset >= limit) {
        throw new RangeError("Unexpected end of input while reading data segment.");
    }

    const mode = bytes[offset++];
    switch (mode) {
        case WasmDataSegmentActive:
            if (!allowActive) {
                throw new Error("Expected a passive data segment.");
            }
            offset = skipActiveDataSegmentOffsetExpression(bytes, offset, limit);
            break;

        case WasmDataSegmentPassive:
            break;

        default:
            throw new Error(`Unsupported Webcil data segment mode ${mode}.`);
    }

    const lengthInfo = readULEB128(bytes, offset, limit);
    const dataStart = lengthInfo.offset;
    const dataEnd = checkedAdd(dataStart, lengthInfo.value, limit, "data segment");
    return {
        dataStart,
        dataLength: lengthInfo.value,
        offset: dataEnd
    };
}

function skipActiveDataSegmentOffsetExpression(bytes: Uint8Array, offset: number, limit: number): number {
    if (offset >= limit) {
        throw new RangeError("Unexpected end of input while reading active data segment offset expression.");
    }

    const opcode = bytes[offset++];
    switch (opcode) {
        case WasmOpcodeGlobalGet:
        case WasmOpcodeI32Const:
            offset = readULEB128(bytes, offset, limit).offset;
            break;

        default:
            throw new Error(`Unsupported active data segment offset opcode ${opcode}.`);
    }

    if (offset >= limit || bytes[offset++] !== WasmOpcodeEnd) {
        throw new Error("Active data segment offset expression is missing end opcode.");
    }

    return offset;
}

function checkedAdd(start: number, size: number, limit: number, description: string): number {
    const end = start + size;
    if (end < start || end > limit) {
        throw new RangeError(`${description} extends past its enclosing bounds.`);
    }

    return end;
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
// These import names and the webcilVersion/getWebcilPayload/fillWebcilTable/patchWebcilHeader
// handshake in finishWebcilInstance are the R2R Webcil-in-Wasm host ABI defined by crossgen's
// WasmObjectWriter (src/coreclr/tools/Common/Compiler/ObjectWriter/WasmObjectWriter.cs,
// CreateDefaultGlobalImports/WriteExports). Keep in sync with the corerun host
// (src/coreclr/hosts/corerun/wasm/libCorerun.js, BrowserHost_ExternalAssemblyProbe). The browser
// loader receives payloadSize/tableSize from boot config and validates the wrapper against them.
function buildWebcilImports(memory: WebAssembly.Memory, payloadPtr: number, tableSize: number): Record<string, WebAssembly.ImportValue> {
    const webcilImports: Record<string, WebAssembly.ImportValue> = { memory };
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
        webcilImports.__memory_base = new WebAssembly.Global({ value: "i32", mutable: false }, payloadPtr);
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

    // Two image shapes reach this point. A component stub carries its payload and table in passive
    // segments and hands them over via getWebcilPayload/fillWebcilTable. A composite uses active
    // segments, so the engine installed both at instantiation and only the header's tableBase field
    // is left to write. Feature-detect rather than assume: getWebcilPayload on a composite would
    // trap, because memory.init against an active (hence dropped) segment is out of bounds.
    const patchWebcilHeader = instance.exports.patchWebcilHeader as ((ptr: number, size: number) => void) | undefined;
    if (typeof patchWebcilHeader === "function") {
        patchWebcilHeader(payloadPtr, payloadSize);
    } else {
        const getWebcilPayload = instance.exports.getWebcilPayload as (ptr: number, size: number) => void;
        getWebcilPayload(payloadPtr, payloadSize);
        if (tableSize > 0) {
            const fillWebcilTable = instance.exports.fillWebcilTable as () => void;
            fillWebcilTable();
        }
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
