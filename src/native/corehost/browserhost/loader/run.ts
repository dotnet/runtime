// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetHostBuilder, JsModuleExports, EmscriptenModuleInternal } from "./types";

import { dotnetAssert, dotnetInternals, dotnetBrowserHostExports, Module } from "./cross-module";
import { findResources, isNodeHosted, isShellHosted, validateWasmFeatures } from "./bootstrap";
import { exit, runtimeState } from "./exit";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { getIcuResourceName } from "./icu";
import { loaderConfig } from "./config";
import { fetchDll, fetchIcu, fetchPdb, fetchVfs, fetchWasm, loadDotnetModule, loadJSModule, nativeModulePromiseController, verifyAllAssetsDownloaded } from "./assets";

const runMainPromiseController = createPromiseCompletionSource<number>();

// WASM-TODO: webCIL
// WASM-TODO: downloadOnly - blazor render mode auto pre-download. Really no start.
// WASM-TODO: loadAllSatelliteResources
// WASM-TODO: runtimeOptions
// WASM-TODO: debugLevel
// WASM-TODO: load symbolication json https://github.com/dotnet/runtime/issues/122647

// many things happen in parallel here, but order matters for performance!
// ideally we want to utilize network and CPU at the same time
export async function createRuntime(downloadOnly: boolean): Promise<any> {
    if (!loaderConfig.resources || !loaderConfig.resources.coreAssembly || !loaderConfig.resources.coreAssembly.length) throw new Error("Invalid config, resources is not set");

    await validateWasmFeatures();

    if (typeof Module.onConfigLoaded === "function") {
        await Module.onConfigLoaded(loaderConfig);
    }
    const modulesAfterConfigLoaded = await Promise.all((loaderConfig.resources.modulesAfterConfigLoaded || []).map(loadJSModule));
    for (const afterConfigLoadedModule of modulesAfterConfigLoaded) {
        await afterConfigLoadedModule.onRuntimeConfigLoaded?.(loaderConfig);
    }

    if (loaderConfig.resources.jsModuleDiagnostics && loaderConfig.resources.jsModuleDiagnostics.length > 0) {
        const diagnosticsModule = await loadDotnetModule(loaderConfig.resources.jsModuleDiagnostics[0]);
        diagnosticsModule.dotnetInitializeModule<void>(dotnetInternals);
    }
    const nativeModulePromise: Promise<JsModuleExports> = loadDotnetModule(loaderConfig.resources.jsModuleNative[0]);
    const runtimeModulePromise: Promise<JsModuleExports> = loadDotnetModule(loaderConfig.resources.jsModuleRuntime[0]);
    const wasmNativePromise: Promise<Response> = fetchWasm(loaderConfig.resources.wasmNative[0]);

    const coreAssembliesPromise = Promise.all(loaderConfig.resources.coreAssembly.map(fetchDll));
    const coreVfsPromise = Promise.all((loaderConfig.resources.coreVfs || []).map(fetchVfs));
    const assembliesPromise = Promise.all(loaderConfig.resources.assembly.map(fetchDll));
    const vfsPromise = Promise.all((loaderConfig.resources.vfs || []).map(fetchVfs));
    const icuResourceName = getIcuResourceName();
    const icuDataPromise = icuResourceName ? Promise.all((loaderConfig.resources.icu || []).filter(asset => asset.name === icuResourceName).map(fetchIcu)) : Promise.resolve([]);

    const corePDBsPromise = Promise.all((loaderConfig.resources.corePdb || []).map(fetchPdb));
    const pdbsPromise = Promise.all((loaderConfig.resources.pdb || []).map(fetchPdb));
    const modulesAfterRuntimeReadyPromise = Promise.all((loaderConfig.resources.modulesAfterRuntimeReady || []).map(loadJSModule));

    const nativeModule = await nativeModulePromise;
    const modulePromise = nativeModule.dotnetInitializeModule<EmscriptenModuleInternal>(dotnetInternals);
    nativeModulePromiseController.propagateFrom(modulePromise);

    const runtimeModule = await runtimeModulePromise;
    const runtimeModuleReady = runtimeModule.dotnetInitializeModule<void>(dotnetInternals);

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
    await corePDBsPromise;
    await pdbsPromise;
    await runtimeModuleReady;

    verifyAllAssetsDownloaded();

    if (typeof Module.onDotnetReady === "function") {
        await Module.onDotnetReady();
    }
    const modulesAfterRuntimeReady = await modulesAfterRuntimeReadyPromise;
    for (const afterRuntimeReadyModule of modulesAfterRuntimeReady) {
        await afterRuntimeReadyModule.onRuntimeReady?.(loaderConfig);
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
