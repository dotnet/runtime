// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssetEntryInternal, CharPtrPtr, VoidPtr } from "../types";
import { _ems_ } from "../ems-ambient";
import { browserVirtualAppBase, sizeOfPtr } from "../per-module";

const HOST_PROPERTY_RUNTIME_CONTRACT = "HOST_RUNTIME_CONTRACT";
const HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES = "TRUSTED_PLATFORM_ASSEMBLIES";
const HOST_PROPERTY_ENTRY_ASSEMBLY_NAME = "ENTRY_ASSEMBLY_NAME";
const HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES = "NATIVE_DLL_SEARCH_DIRECTORIES";
const HOST_PROPERTY_APP_PATHS = "APP_PATHS";
const APP_CONTEXT_BASE_DIRECTORY = "APP_CONTEXT_BASE_DIRECTORY";
const RUNTIME_IDENTIFIER = "RUNTIME_IDENTIFIER";

export function initializeCoreCLR(deferR2R = false): number {
    const loaderConfig = _ems_.dotnetApi.getConfig();
    const hostContractPtr = _ems_._BrowserHost_CreateHostContract();
    const runtimeConfigProperties = new Map<string, string>();
    if (loaderConfig.runtimeConfig?.runtimeOptions?.configProperties) {
        for (const [key, value] of Object.entries(loaderConfig.runtimeConfig?.runtimeOptions?.configProperties)) {
            runtimeConfigProperties.set(key, "" + value);
        }
    }

    const assemblyPaths = loaderConfig.resources!.assembly.map(asset => asset.virtualPath.replace(/\.wasm$/, ".dll"));
    const coreAssemblyPaths = loaderConfig.resources!.coreAssembly
        .filter(asset => !(asset as AssetEntryInternal).isCompositeImage)
        .map(asset => asset.virtualPath.replace(/\.wasm$/, ".dll"));
    const tpa = [...coreAssemblyPaths, ...assemblyPaths].join(":");
    runtimeConfigProperties.set(HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES, tpa);
    runtimeConfigProperties.set(HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES, loaderConfig.virtualWorkingDirectory!);
    runtimeConfigProperties.set(HOST_PROPERTY_APP_PATHS, loaderConfig.virtualWorkingDirectory!);
    runtimeConfigProperties.set(HOST_PROPERTY_ENTRY_ASSEMBLY_NAME, loaderConfig.mainAssemblyName!);
    runtimeConfigProperties.set(APP_CONTEXT_BASE_DIRECTORY, browserVirtualAppBase);
    runtimeConfigProperties.set(RUNTIME_IDENTIFIER, "browser-wasm");
    runtimeConfigProperties.set(HOST_PROPERTY_RUNTIME_CONTRACT, `0x${(hostContractPtr as unknown as number).toString(16)}`);

    const buffers: VoidPtr[] = [];
    const appctx_keys = _ems_._malloc(sizeOfPtr * runtimeConfigProperties.size) as any as CharPtrPtr;
    const appctx_values = _ems_._malloc(sizeOfPtr * runtimeConfigProperties.size) as any as CharPtrPtr;
    buffers.push(appctx_keys as any);
    buffers.push(appctx_values as any);

    let propertyCount = 0;
    for (const [key, value] of runtimeConfigProperties.entries()) {
        const keyPtr = _ems_.dotnetBrowserUtilsExports.stringToUTF8Ptr(key);
        const valuePtr = _ems_.dotnetBrowserUtilsExports.stringToUTF8Ptr(value);
        _ems_.dotnetApi.setHeapU32((appctx_keys as any) + (propertyCount * sizeOfPtr), keyPtr);
        _ems_.dotnetApi.setHeapU32((appctx_values as any) + (propertyCount * sizeOfPtr), valuePtr);
        propertyCount++;
        buffers.push(keyPtr as any);
        buffers.push(valuePtr as any);
    }

    const res = _ems_._BrowserHost_InitializeDotnet(propertyCount, appctx_keys, appctx_values, deferR2R ? 1 : 0);
    for (const buf of buffers) {
        _ems_._free(buf as any);
    }
    return res;
}

export function activateDeferredR2R(): void {
    const result = _ems_._BrowserHost_ActivateDeferredR2R();
    if (result < 0) throw new Error(`Deferred ReadyToRun activation failed: 0x${(result >>> 0).toString(16)}`);
}

export function getDeferredR2RStats(): { activationAttempts: number, publishedMethods: number, interpreterOnlyMethods: number, interpreterSuspensions: number } {
    const sp = _ems_.stackSave();
    try {
        const ptr = _ems_.stackAlloc(4 * sizeOfPtr);
        _ems_._BrowserHost_GetDeferredR2RStats(ptr);
        const index = Number(ptr) >>> 2;
        return {
            activationAttempts: _ems_.HEAPU32[index],
            publishedMethods: _ems_.HEAPU32[index + 1],
            interpreterOnlyMethods: _ems_.HEAPU32[index + 2],
            interpreterSuspensions: _ems_.HEAPU32[index + 3],
        };
    } finally {
        _ems_.stackRestore(sp);
    }
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
        const argsvPtr: number = _ems_.stackAlloc((args.length + 1) * sizeOfPtr) as any;
        const ptrs: VoidPtr[] = [];
        try {

            for (let i = 0; i < args.length; i++) {
                const ptr = _ems_.dotnetBrowserUtilsExports.stringToUTF8Ptr(args[i]) as any;
                ptrs.push(ptr);
                _ems_.HEAPU32[(argsvPtr >>> 2) + i] = ptr;
            }
            _ems_.EXITSTATUS = _ems_._BrowserHost_ExecuteAssembly(mainAssemblyNamePtr, args.length, argsvPtr);
            for (const ptr of ptrs) {
                _ems_._free(ptr);
            }

            if (_ems_.EXITSTATUS == -1) {
                const reason = new Error("Failed to execute assembly");
                _ems_.dotnetApi.exit(-1, reason);
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
    const exitCode = await runMain(mainAssemblyName, args);
    try {
        _ems_.dotnetApi.exit(exitCode, null);
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (!error || typeof error.status !== "number") {
            _ems_.dotnetApi.exit(1, error);
            throw error;
        }
        return error.status;
    }
    return exitCode;
}

