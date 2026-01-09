// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VfsAsset, VoidPtr, VoidPtrPtr } from "./types";
import { _ems_ } from "../../../libs/Common/JavaScript/ems-ambient";

const loadedAssemblies: Map<string, { ptr: number, length: number }> = new Map();

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function registerPdbBytes(bytes: Uint8Array, asset: { name: string, virtualPath: string }) {
    // WASM-TODO: https://github.com/dotnet/runtime/issues/122921
}

export function registerDllBytes(bytes: Uint8Array, asset: { name: string, virtualPath: string }) {
    const sp = _ems_.Module.stackSave();
    try {
        const sizeOfPtr = 4;
        const ptrPtr = _ems_.Module.stackAlloc(sizeOfPtr);
        if (_ems_.Module._posix_memalign(ptrPtr as any, 16, bytes.length)) {
            throw new Error("posix_memalign failed");
        }

        const ptr = _ems_.Module.HEAPU32[ptrPtr as any >>> 2];
        _ems_.Module.HEAPU8.set(bytes, ptr >>> 0);
        loadedAssemblies.set(asset.virtualPath, { ptr, length: bytes.length });
        if (!asset.virtualPath.startsWith("/")) {
            loadedAssemblies.set("/" + asset.virtualPath, { ptr, length: bytes.length });
        }
    } finally {
        _ems_.Module.stackRestore(sp);
    }
}

export function loadIcuData(bytes: Uint8Array) {
    const sp = _ems_.Module.stackSave();
    try {
        const sizeOfPtr = 4;
        const ptrPtr = _ems_.Module.stackAlloc(sizeOfPtr);
        if (_ems_.Module._posix_memalign(ptrPtr as any, 16, bytes.length)) {
            throw new Error("posix_memalign failed for ICU data");
        }

        const ptr = _ems_.Module.HEAPU32[ptrPtr as any >>> 2];
        _ems_.Module.HEAPU8.set(bytes, ptr >>> 0);

        const result = _ems_._wasm_load_icu_data(ptr as unknown as VoidPtr);
        if (!result) {
            throw new Error("Failed to initialize ICU data");
        }
    } finally {
        _ems_.Module.stackRestore(sp);
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

        _ems_.dotnetLogger.debug(`Creating directory '${parentDirectory}'`);

        _ems_.Module.FS_createPath(
            "/", parentDirectory, true, true // fixme: should canWrite be false?
        );
    } else {
        parentDirectory = "/";
    }

    _ems_.dotnetLogger.debug(`Creating file '${fileName}' in directory '${parentDirectory}'`);

    _ems_.Module.FS_createDataFile(
        parentDirectory, fileName,
        bytes, true /* canRead */, true /* canWrite */, true /* canOwn */
    );
}

export function initializeCoreCLR(): number {
    return _ems_._BrowserHost_InitializeCoreCLR();
}

// bool BrowserHost_ExternalAssemblyProbe(const char* pathPtr, /*out*/ void **outDataStartPtr, /*out*/ int64_t* outSize);
export function BrowserHost_ExternalAssemblyProbe(pathPtr: CharPtr, outDataStartPtr: VoidPtrPtr, outSize: VoidPtr) {
    const path = _ems_.Module.UTF8ToString(pathPtr);
    const assembly = loadedAssemblies.get(path);
    if (assembly) {
        _ems_.Module.HEAPU32[outDataStartPtr as any >>> 2] = assembly.ptr;
        // int64_t target
        _ems_.Module.HEAPU32[outSize as any >>> 2] = assembly.length;
        _ems_.Module.HEAPU32[((outSize as any) + 4) >>> 2] = 0;
        return true;
    }
    _ems_.dotnetLogger.debug(`Assembly not found: '${path}'`);
    _ems_.Module.HEAPU32[outDataStartPtr as any >>> 2] = 0;
    _ems_.Module.HEAPU32[outSize as any >>> 2] = 0;
    _ems_.Module.HEAPU32[((outSize as any) + 4) >>> 2] = 0;
    return false;
}

export async function runMain(mainAssemblyName?: string, args?: string[]): Promise<number> {
    try {
        const config = _ems_.dotnetApi.getConfig();
        if (!mainAssemblyName) {
            mainAssemblyName = config.mainAssemblyName!;
        }
        if (!mainAssemblyName.endsWith(".dll")) {
            mainAssemblyName += ".dll";
        }
        const mainAssemblyNamePtr = _ems_.dotnetBrowserUtilsExports.stringToUTF8Ptr(mainAssemblyName) as any;

        args ??= [];

        const sp = _ems_.Module.stackSave();
        const argsvPtr: number = _ems_.Module.stackAlloc((args.length + 1) * 4) as any;
        const ptrs: VoidPtr[] = [];
        try {

            for (let i = 0; i < args.length; i++) {
                const ptr = _ems_.dotnetBrowserUtilsExports.stringToUTF8Ptr(args[i]) as any;
                ptrs.push(ptr);
                _ems_.Module.HEAPU32[(argsvPtr >>> 2) + i] = ptr;
            }
            const res = _ems_._BrowserHost_ExecuteAssembly(mainAssemblyNamePtr, args.length, argsvPtr);
            for (const ptr of ptrs) {
                _ems_.Module._free(ptr);
            }

            if (res != 0) {
                const reason = new Error("Failed to execute assembly");
                _ems_.dotnetApi.exit(res, reason);
                throw reason;
            }

            return _ems_.dotnetLoaderExports.getRunMainPromise();
        } finally {
            _ems_.Module.stackRestore(sp);
        }
    } catch (error: any) {
        // if the error is an ExitStatus, use its status code
        if (error && typeof error.status === "number") {
            return error.status;
        }
        _ems_.dotnetApi.exit(1, error);
        throw error;
    }
}

export async function runMainAndExit(mainAssemblyName?: string, args?: string[]): Promise<number> {
    const res = await runMain(mainAssemblyName, args);
    try {
        _ems_.dotnetApi.exit(0, null);
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (error.status === undefined) {
            _ems_.dotnetApi.exit(1, error);
            throw error;
        }
    }
    return res;
}

