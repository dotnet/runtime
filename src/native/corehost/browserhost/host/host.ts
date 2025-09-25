// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VoidPtr, VoidPtrPtr } from "./types";
import { } from "./cross-linked"; // ensure ambient symbols are declared

const loadedAssemblies : Map<string, { ptr: number, length: number }> = new Map();

export function registerDllBytes(bytes: Uint8Array, asset: { name: string }) {
    const sp = Module.stackSave();
    try {
        const sizeOfPtr = 4;
        const ptrPtr = Module.stackAlloc(sizeOfPtr);
        if (Module._posix_memalign(ptrPtr as any, 16, bytes.length)) {
            throw new Error("posix_memalign failed");
        }

        const ptr = Module.HEAPU32[ptrPtr as any >>> 2];
        Module.HEAPU8.set(bytes, ptr);
        loadedAssemblies.set(asset.name, { ptr, length: bytes.length });
    } finally {
        Module.stackRestore(sp);
    }
}

// bool BrowserHost_ExternalAssemblyProbe(const char* pathPtr, /*out*/ void **outDataStartPtr, /*out*/ int64_t* outSize);
export function BrowserHost_ExternalAssemblyProbe(pathPtr:CharPtr, outDataStartPtr:VoidPtrPtr, outSize:VoidPtr) {
    const path = Module.UTF8ToString(pathPtr);
    const assembly = loadedAssemblies.get(path);
    if (assembly) {
        Module.HEAPU32[outDataStartPtr as any >>> 2] = assembly.ptr;
        // int64_t target
        Module.HEAPU32[outSize as any >>> 2] = assembly.length;
        Module.HEAPU32[((outSize as any) + 4) >>> 2] = 0;
        return true;
    }
    Module.HEAPU32[outDataStartPtr as any >>> 2] = 0;
    Module.HEAPU32[outSize as any >>> 2] = 0;
    Module.HEAPU32[((outSize as any) + 4) >>> 2] = 0;
    return false;
}

export function BrowserHost_ResolveMain(exitCode:number) {
    netLoaderExports.resolveRunMainPromise(exitCode);
}

export function BrowserHost_RejectMain(reason:any) {
    netLoaderExports.rejectRunMainPromise(reason);
}

// WASM-TODO: take ideas from Mono
// - second call to exit should be silent
// - second call to exit not override the first exit code
// - improve reason extraction
// - install global handler for unhandled exceptions and promise rejections
export function exit(exit_code: number, reason: any): void {
    const reasonStr = reason ? (reason.stack ? reason.stack || reason.message : reason.toString()) : "";
    if (exit_code !== 0) {
        Logger.error(`Exit with code ${exit_code} ${reason ? "and reason: " + reasonStr : ""}`);
    }
    if (netJSEngine.IS_NODE) {
        (globalThis as any).process.exit(exit_code);
    }
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function runMain(mainAssemblyName?: string, args?: string[]): Promise<number> {
    // int BrowserHost_ExecuteAssembly(char * assemblyPath)
    const res = Module.ccall("BrowserHost_ExecuteAssembly", "number", ["string"], [mainAssemblyName]) as number;
    if (res != 0) {
        const reason = new Error("Failed to execute assembly");
        exit(res, reason);
        throw reason;
    }

    return netLoaderExports.getRunMainPromise();
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function runMainAndExit(mainAssemblyName?: string, args?: string[]): Promise<number> {
    try {
        await runMain(mainAssemblyName, args);
    } catch (error) {
        exit(1, error);
        throw error;
    }
    exit(0, null);
    return 0;
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function setEnvironmentVariable(name: string, value: string): void {
    throw new Error("Not implemented");
}
