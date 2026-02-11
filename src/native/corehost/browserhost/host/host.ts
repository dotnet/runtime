// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtrPtr, VoidPtr } from "./types";
import { _ems_ } from "../../../libs/Common/JavaScript/ems-ambient";
import { browserVirtualAppBase } from "./per-module";

const HOST_PROPERTY_RUNTIME_CONTRACT = "HOST_RUNTIME_CONTRACT";
const HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES = "TRUSTED_PLATFORM_ASSEMBLIES";
const HOST_PROPERTY_ENTRY_ASSEMBLY_NAME = "ENTRY_ASSEMBLY_NAME";
const HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES = "NATIVE_DLL_SEARCH_DIRECTORIES";
const HOST_PROPERTY_APP_PATHS = "APP_PATHS";
const APP_CONTEXT_BASE_DIRECTORY = "APP_CONTEXT_BASE_DIRECTORY";
const RUNTIME_IDENTIFIER = "RUNTIME_IDENTIFIER";

export function initializeCoreCLR(): number {
    const loaderConfig = _ems_.dotnetApi.getConfig();
    const hostContractPtr = _ems_._BrowserHost_CreateHostContract();
    const runtimeConfigProperties = new Map<string, string>();
    if (loaderConfig.runtimeConfig?.runtimeOptions?.configProperties) {
        for (const [key, value] of Object.entries(loaderConfig.runtimeConfig?.runtimeOptions?.configProperties)) {
            runtimeConfigProperties.set(key, "" + value);
        }
    }

    const assemblyPaths = loaderConfig.resources!.assembly.map(asset => asset.virtualPath);
    const coreAssemblyPaths = loaderConfig.resources!.coreAssembly.map(asset => asset.virtualPath);
    const tpa = [...coreAssemblyPaths, ...assemblyPaths].join(":");
    runtimeConfigProperties.set(HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES, tpa);
    runtimeConfigProperties.set(HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES, loaderConfig.virtualWorkingDirectory!);
    runtimeConfigProperties.set(HOST_PROPERTY_APP_PATHS, loaderConfig.virtualWorkingDirectory!);
    runtimeConfigProperties.set(HOST_PROPERTY_ENTRY_ASSEMBLY_NAME, loaderConfig.mainAssemblyName!);
    runtimeConfigProperties.set(APP_CONTEXT_BASE_DIRECTORY, browserVirtualAppBase);
    runtimeConfigProperties.set(RUNTIME_IDENTIFIER, "browser-wasm");
    runtimeConfigProperties.set(HOST_PROPERTY_RUNTIME_CONTRACT, `0x${(hostContractPtr as unknown as number).toString(16)}`);

    const buffers: VoidPtr[] = [];
    const appctx_keys = _ems_._malloc(4 * runtimeConfigProperties.size) as any as CharPtrPtr;
    const appctx_values = _ems_._malloc(4 * runtimeConfigProperties.size) as any as CharPtrPtr;
    buffers.push(appctx_keys as any);
    buffers.push(appctx_values as any);

    let propertyCount = 0;
    for (const [key, value] of runtimeConfigProperties.entries()) {
        const keyPtr = _ems_.dotnetBrowserUtilsExports.stringToUTF8Ptr(key);
        const valuePtr = _ems_.dotnetBrowserUtilsExports.stringToUTF8Ptr(value);
        _ems_.dotnetApi.setHeapU32((appctx_keys as any) + (propertyCount * 4), keyPtr);
        _ems_.dotnetApi.setHeapU32((appctx_values as any) + (propertyCount * 4), valuePtr);
        propertyCount++;
        buffers.push(keyPtr as any);
        buffers.push(valuePtr as any);
    }

    const res = _ems_._BrowserHost_InitializeCoreCLR(propertyCount, appctx_keys, appctx_values);
    for (const buf of buffers) {
        _ems_._free(buf as any);
    }
    return res;
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

        const sp = _ems_.stackSave();
        const argsvPtr: number = _ems_.stackAlloc((args.length + 1) * 4) as any;
        const ptrs: VoidPtr[] = [];
        try {

            for (let i = 0; i < args.length; i++) {
                const ptr = _ems_.dotnetBrowserUtilsExports.stringToUTF8Ptr(args[i]) as any;
                ptrs.push(ptr);
                _ems_.HEAPU32[(argsvPtr >>> 2) + i] = ptr;
            }
            const res = _ems_._BrowserHost_ExecuteAssembly(mainAssemblyNamePtr, args.length, argsvPtr);
            for (const ptr of ptrs) {
                _ems_._free(ptr);
            }

            if (res != 0) {
                const reason = new Error("Failed to execute assembly");
                _ems_.dotnetApi.exit(res, reason);
                throw reason;
            }

            return _ems_.dotnetLoaderExports.getRunMainPromise();
        } finally {
            _ems_.stackRestore(sp);
        }
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (!error || typeof error.status !== "number") {
            _ems_.dotnetApi.exit(1, error);
            throw error;
        }
        return error.status;
    }
}

export async function runMainAndExit(mainAssemblyName?: string, args?: string[]): Promise<number> {
    const res = await runMain(mainAssemblyName, args);
    try {
        _ems_.dotnetApi.exit(0, null);
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (!error || typeof error.status !== "number") {
            _ems_.dotnetApi.exit(1, error);
            throw error;
        }
        return error.status;
    }
    return res;
}

