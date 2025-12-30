// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VfsAsset, VoidPtr, VoidPtrPtr } from "./types";
import { } from "./cross-linked"; // ensure ambient symbols are declared

const loadedAssemblies: Map<string, { ptr: number, length: number }> = new Map();

export function registerDllBytes(bytes: Uint8Array, asset: { name: string, virtualPath: string }) {
    const sp = Module.stackSave();
    try {
        const sizeOfPtr = 4;
        const ptrPtr = Module.stackAlloc(sizeOfPtr);
        if (Module._posix_memalign(ptrPtr as any, 16, bytes.length)) {
            throw new Error("posix_memalign failed");
        }

        const ptr = Module.HEAPU32[ptrPtr as any >>> 2];
        Module.HEAPU8.set(bytes, ptr >>> 0);
        loadedAssemblies.set(asset.name, { ptr, length: bytes.length });
        loadedAssemblies.set(asset.virtualPath, { ptr, length: bytes.length });
    } finally {
        Module.stackRestore(sp);
    }
}

export function loadIcuData(bytes: Uint8Array) {
    const sp = Module.stackSave();
    try {
        const sizeOfPtr = 4;
        const ptrPtr = Module.stackAlloc(sizeOfPtr);
        if (Module._posix_memalign(ptrPtr as any, 16, bytes.length)) {
            throw new Error("posix_memalign failed for ICU data");
        }

        const ptr = Module.HEAPU32[ptrPtr as any >>> 2];
        Module.HEAPU8.set(bytes, ptr >>> 0);

        const result = _wasm_load_icu_data(ptr as unknown as VoidPtr);
        if (!result) {
            throw new Error("Failed to initialize ICU data");
        }
    } finally {
        Module.stackRestore(sp);
    }
}

export function installVfsFile(bytes: Uint8Array, asset: VfsAsset) {
    const virtualName: string = typeof (asset.virtualPath) === "string"
        ? asset.virtualPath
        : asset.name;
    const lastSlash = virtualName.lastIndexOf("/");
    let parentDirectory = (lastSlash > 0)
        ? virtualName.substring(0, lastSlash)
        : null;
    let fileName = (lastSlash > 0)
        ? virtualName.substring(lastSlash + 1)
        : virtualName;
    if (fileName.startsWith("/"))
        fileName = fileName.substring(1);
    if (parentDirectory) {
        if (!parentDirectory.startsWith("/"))
            parentDirectory = "/" + parentDirectory;

        if (parentDirectory.startsWith("/managed")) {
            throw new Error("Cannot create files under /managed virtual directory as it is reserved for NodeFS mounting");
        }

        dotnetLogger.debug(`Creating directory '${parentDirectory}'`);

        Module.FS_createPath(
            "/", parentDirectory, true, true // fixme: should canWrite be false?
        );
    } else {
        parentDirectory = "/";
    }

    dotnetLogger.debug(`Creating file '${fileName}' in directory '${parentDirectory}'`);

    Module.FS_createDataFile(
        parentDirectory, fileName,
        bytes, true /* canRead */, true /* canWrite */, true /* canOwn */
    );
}

// bool BrowserHost_ExternalAssemblyProbe(const char* pathPtr, /*out*/ void **outDataStartPtr, /*out*/ int64_t* outSize);
export function BrowserHost_ExternalAssemblyProbe(pathPtr: CharPtr, outDataStartPtr: VoidPtrPtr, outSize: VoidPtr) {
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

export async function runMain(mainAssemblyName?: string, args?: string[]): Promise<number> {
    const config = dotnetApi.getConfig();
    if (!mainAssemblyName) {
        mainAssemblyName = config.mainAssemblyName!;
    }
    // TODO-WASM: Difference in boot config generator
    if (!mainAssemblyName.endsWith(".dll")) {
        mainAssemblyName += ".dll";
    }
    const mainAssemblyNamePtr = dotnetBrowserUtilsExports.stringToUTF8Ptr(mainAssemblyName) as any;

    if (!args) {
        args = [];
    }

    const sp = Module.stackSave();
    const argsvPtr: number = Module.stackAlloc((args.length + 1) * 4) as any;
    const ptrs: VoidPtr[] = [];
    try {

        for (let i = 0; i < args.length; i++) {
            const ptr = dotnetBrowserUtilsExports.stringToUTF8Ptr(args[i]) as any;
            ptrs.push(ptr);
            Module.HEAPU32[(argsvPtr >>> 2) + i] = ptr;
        }
        const res = _BrowserHost_ExecuteAssembly(mainAssemblyNamePtr, args.length, argsvPtr);
        for (const ptr of ptrs) {
            Module._free(ptr);
        }

        if (res != 0) {
            const reason = new Error("Failed to execute assembly");
            dotnetApi.exit(res, reason);
            throw reason;
        }

        return dotnetLoaderExports.getRunMainPromise();
    } finally {
        Module.stackRestore(sp);
    }
}

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

