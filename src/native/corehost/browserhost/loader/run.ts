// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetHostBuilder, JsModuleExports, EmscriptenModuleInternal } from "./types";

import { dotnetAssert, dotnetGetInternals, dotnetBrowserHostExports, Module } from "./cross-module";
import { findResources, isNodeHosted, isShellHosted } from "./bootstrap";
import { exit, runtimeState } from "./exit";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { getIcuResourceName } from "./icu";
import { getLoaderConfig } from "./config";
import { fetchDll, fetchIcu, fetchVfs, fetchWasm, loadJSModule, nativeModulePromiseController } from "./assets";

const runMainPromiseController = createPromiseCompletionSource<number>();

// WASM-TODO: retry logic
// WASM-TODO: throttling logic
// WASM-TODO: Module.onDownloadResourceProgress
// WASM-TODO: invokeLibraryInitializers
// WASM-TODO: webCIL
// WASM-TODO: downloadOnly - blazor render mode auto pre-download. Really no start.
// WASM-TODO: fail fast for missing WASM features - SIMD, EH, BigInt detection
// WASM-TODO: Module.locateFile
// WASM-TODO: loadBootResource
// WASM-TODO: loadAllSatelliteResources
// WASM-TODO: runtimeOptions
// WASM-TODO: debugLevel
// WASM-TODO: load symbolication json https://github.com/dotnet/runtime/issues/122647
export async function createRuntime(downloadOnly: boolean): Promise<any> {
    const config = getLoaderConfig();
    if (!config.resources || !config.resources.coreAssembly || !config.resources.coreAssembly.length) throw new Error("Invalid config, resources is not set");


    if (typeof Module.onConfigLoaded === "function") {
        await Module.onConfigLoaded(config);
    }

    if (config.resources.jsModuleDiagnostics && config.resources.jsModuleDiagnostics.length > 0) {
        const diagnosticsModule = await loadJSModule(config.resources.jsModuleDiagnostics[0]);
        diagnosticsModule.dotnetInitializeModule<void>(dotnetGetInternals());
    }
    const nativeModulePromise: Promise<JsModuleExports> = loadJSModule(config.resources.jsModuleNative[0]);
    const runtimeModulePromise: Promise<JsModuleExports> = loadJSModule(config.resources.jsModuleRuntime[0]);
    const wasmNativePromise: Promise<Response> = fetchWasm(config.resources.wasmNative[0]);

    const coreAssembliesPromise = Promise.all(config.resources.coreAssembly.map(fetchDll));
    const coreVfsPromise = Promise.all((config.resources.coreVfs || []).map(fetchVfs));
    const assembliesPromise = Promise.all(config.resources.assembly.map(fetchDll));
    const vfsPromise = Promise.all((config.resources.vfs || []).map(fetchVfs));
    const icuResourceName = getIcuResourceName(config);
    const icuDataPromise = icuResourceName ? Promise.all((config.resources.icu || []).filter(asset => asset.name === icuResourceName).map(fetchIcu)) : Promise.resolve([]);

    const nativeModule = await nativeModulePromise;
    const modulePromise = nativeModule.dotnetInitializeModule<EmscriptenModuleInternal>(dotnetGetInternals());
    nativeModulePromiseController.propagateFrom(modulePromise);

    const runtimeModule = await runtimeModulePromise;
    const runtimeModuleReady = runtimeModule.dotnetInitializeModule<void>(dotnetGetInternals());

    await nativeModulePromiseController.promise;
    await coreAssembliesPromise;
    await coreVfsPromise;
    await vfsPromise;
    await icuDataPromise;
    await wasmNativePromise; // this is just to propagate errors
    if (!downloadOnly) {
        Module.runtimeKeepalivePush();
        initializeCoreCLR();
    }

    await assembliesPromise;
    await runtimeModuleReady;

    if (typeof Module.onDotnetReady === "function") {
        await Module.onDotnetReady();
    }
}

export function abortStartup(reason: any): void {
    nativeModulePromiseController.reject(reason);
}

function initializeCoreCLR(): void {
    dotnetAssert.check(!runtimeState.runtimeReady, "CoreCLR should be initialized just once");
    const res = dotnetBrowserHostExports.initializeCoreCLR();
    if (res != 0) {
        const reason = new Error("Failed to initialize CoreCLR");
        runMainPromiseController.reject(reason);
        exit(res, reason);
    }
    runtimeState.runtimeReady = true;
}

export function resolveRunMainPromise(exitCode: number): void {
    runMainPromiseController.resolve(exitCode);
}

export function rejectRunMainPromise(reason: any): void {
    runMainPromiseController.reject(reason);
}

export function getRunMainPromise(): Promise<number> {
    return runMainPromiseController.promise;
}

// Auto-start when in NodeJS environment as a entry script
export async function selfHostNodeJS(dotnet: DotnetHostBuilder): Promise<void> {
    try {
        if (isNodeHosted()) {
            await findResources(dotnet);
            await dotnet.runMainAndExit();
        } else if (isShellHosted()) {
            // because in V8 we can't probe directories to find assemblies
            throw new Error("Shell/V8 hosting is not supported");
        }
    } catch (err: any) {
        exit(1, err);
    }
}
