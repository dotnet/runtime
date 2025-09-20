// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { JSEngine, Logger, Module, loadedAssemblies, loaderExports } from "./cross-module";
import type { CharPtr, VoidPtr, VoidPtrPtr } from "./types";

export function registerDllBytes(bytes: Uint8Array, asset: { name: string }) {
    const sp = Module.stackSave();
    try {
        const sizeOfPtr = 4;
        const ptrPtr = Module.stackAlloc(sizeOfPtr);
        if (Module._posix_memalign(ptrPtr as any, 16, bytes.length)) {
            throw new Error("posix_memalign failed");
        }

        const ptr = Module.HEAPU32[ptrPtr as any >> 2];
        Module.HEAPU8.set(bytes, ptr);
        loadedAssemblies[asset.name] = { ptr, length: bytes.length };
    } finally {
        Module.stackRestore(sp);
    }
}

// bool browserHostExternalAssemblyProbe(const char* pathPtr, /*out*/ void **outDataStartPtr, /*out*/ int64_t* outSize);
export function browserHostExternalAssemblyProbe(pathPtr:CharPtr, outDataStartPtr:VoidPtrPtr, outSize:VoidPtr) {
    const path = Module.UTF8ToString(pathPtr);
    const assembly = loadedAssemblies[path];
    if (!assembly) {
        return false;
    }
    Module.HEAPU32[outDataStartPtr as any >> 2] = assembly.ptr;
    // upper bits are cleared by the C caller
    Module.HEAPU32[outSize as any >> 2] = assembly.length;
    return true;
}
browserHostExternalAssemblyProbe["__deps"] = ["loadedAssemblies"];

export function browserHostResolveMain(exitCode:number) {
    loaderExports.browserHostResolveMain(exitCode);
}

export function browserHostRejectMain(reason:any) {
    loaderExports.browserHostRejectMain(reason);
}

export function exit(exit_code: number, reason: any): void {
    const reasonStr = reason ? (reason.stack ? reason.stack || reason.message : reason.toString()) : "";
    if (exit_code !== 0) {
        Logger.error(`Exit with code ${exit_code} ${reason ? "and reason: " + reasonStr : ""}`);
    }
    if (JSEngine.IS_NODE) {
        (globalThis as any).process.exit(exit_code);
    }
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function runMain(mainAssemblyName?: string, args?: string[]): Promise<number> {
    // int browserHostExecuteAssembly(char * assemblyPath)
    const res = Module.ccall("browserHostExecuteAssembly", "number", ["string"], [mainAssemblyName]) as number;
    if (res != 0) {
        const reason = new Error("Failed to execute assembly");
        exit(res, reason);
        throw reason;
    }

    return loaderExports.getRunMainPromise();
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
//setEnvironmentVariable["__deps"] = ["setenv"];
