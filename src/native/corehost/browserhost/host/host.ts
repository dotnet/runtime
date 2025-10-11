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
    dotnetLoaderExports.resolveRunMainPromise(exitCode);
}

export function BrowserHost_RejectMain(reason:any) {
    dotnetLoaderExports.rejectRunMainPromise(reason);
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function runMain(mainAssemblyName?: string, args?: string[]): Promise<number> {
    // int BrowserHost_ExecuteAssembly(char * assemblyPath)
    const res = Module.ccall("BrowserHost_ExecuteAssembly", "number", ["string"], [mainAssemblyName]) as number;
    if (res != 0) {
        const reason = new Error("Failed to execute assembly");
        dotnetApi.exit(res, reason);
        throw reason;
    }

    return dotnetLoaderExports.getRunMainPromise();
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function runMainAndExit(mainAssemblyName?: string, args?: string[]): Promise<number> {
    try {
        await runMain(mainAssemblyName, args);
    } catch (error) {
        dotnetApi.exit(1, error);
        throw error;
    }
    dotnetApi.exit(0, null);
    return 0;
}

