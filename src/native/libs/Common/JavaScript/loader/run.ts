// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JsModuleExports, EmscriptenModuleInternal } from "./types";

import { dotnetAssert, dotnetInternals, dotnetBrowserHostExports, dotnetApi, Module } from "./cross-module";
import { exit, runtimeState } from "./exit";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { getIcuResourceName } from "./icu";
import { loaderConfig, validateLoaderConfig } from "./config";
import { fetchAssembly, fetchIcu, fetchNativeSymbols, fetchPdb, fetchSatelliteAssemblies, fetchVfs, fetchMainWasm, loadDotnetModule, loadJSModule, nativeModulePromiseController, verifyAllAssetsDownloaded } from "./assets";
import { initPolyfills } from "./polyfills";
import { validateEngineFeatures } from "./bootstrap";

const runMainPromiseController = createPromiseCompletionSource<number>();

async function callLibraryInitializers(modules: JsModuleExports[], resources: any[], methodName: string, args: any): Promise<void> {
    await Promise.all(modules.map(async (module, i) => {
        try {
            await (module as any)[methodName]?.(args);
        } catch (err) {
            const name = (resources[i] as any).name || "unknown";
            const message = err instanceof Error ? err.message : String(err);
            throw new Error(`Failed to invoke '${methodName}' on library initializer '${name}': ${message}`, { cause: err });
        }
    }));
}

// WASM-TODO: downloadOnly - Blazor render mode auto pre-download. Really no start.
// WASM-TODO: debugLevel

// many things happen in parallel here, but order matters for performance!
// ideally we want to utilize network and CPU at the same time
export async function createRuntime(downloadOnly: boolean): Promise<any> {
    if (!loaderConfig.resources || !loaderConfig.resources.coreAssembly || !loaderConfig.resources.coreAssembly.length) throw new Error("Invalid config, resources is not set");
    try {
        runtimeState.creatingRuntime = true;

        await validateEngineFeatures();

        if (typeof Module.onConfigLoaded === "function") {
            await Module.onConfigLoaded(loaderConfig);
        }
        validateLoaderConfig();

        const afterConfigLoadedResources = loaderConfig.resources.modulesAfterConfigLoaded || [];
        const modulesAfterConfigLoaded = await Promise.all(afterConfigLoadedResources.map(loadJSModule));
        await callLibraryInitializers(modulesAfterConfigLoaded, afterConfigLoadedResources, "onRuntimeConfigLoaded", loaderConfig);

        // after onConfigLoaded hooks, polyfills can be initialized
        await initPolyfills();

        if (loaderConfig.resources.jsModuleDiagnostics && loaderConfig.resources.jsModuleDiagnostics.length > 0) {
            const diagnosticsModule = await loadDotnetModule(loaderConfig.resources.jsModuleDiagnostics[0]);
            diagnosticsModule.dotnetInitializeModule<void>(dotnetInternals);
            if (loaderConfig.resources.wasmSymbols && loaderConfig.resources.wasmSymbols.length > 0) {
                await fetchNativeSymbols(loaderConfig.resources.wasmSymbols[0]);
            }
        }
        const nativeModulePromise: Promise<JsModuleExports> = loadDotnetModule(loaderConfig.resources.jsModuleNative[0]);
        const runtimeModulePromise: Promise<JsModuleExports> = loadDotnetModule(loaderConfig.resources.jsModuleRuntime[0]);
        const wasmNativePromise: Promise<Response> = fetchMainWasm(loaderConfig.resources.wasmNative[0]);

        const coreAssembliesPromise = Promise.all(loaderConfig.resources.coreAssembly.map(fetchAssembly));
        const coreVfsPromise = Promise.all((loaderConfig.resources.coreVfs || []).map(fetchVfs));

        const icuResourceName = getIcuResourceName();
        const icuDataPromise = icuResourceName ? Promise.all((loaderConfig.resources.icu || []).filter(asset => asset.name === icuResourceName).map(fetchIcu)) : Promise.resolve([]);

        const assembliesPromise = Promise.all(loaderConfig.resources.assembly.map(fetchAssembly));
        const satelliteResourcesPromise = loaderConfig.loadAllSatelliteResources && loaderConfig.resources.satelliteResources
            ? fetchSatelliteAssemblies(Object.keys(loaderConfig.resources.satelliteResources))
            : Promise.resolve();
        const vfsPromise = Promise.all((loaderConfig.resources.vfs || []).map(fetchVfs));

        // WASM-TODO: also check that the debugger is linked in and check feature flags
        const isDebuggingSupported = loaderConfig.debugLevel != 0;
        const corePDBsPromise = isDebuggingSupported ? Promise.all((loaderConfig.resources.corePdb || []).map(fetchPdb)) : Promise.resolve([]);
        const pdbsPromise = isDebuggingSupported ? Promise.all((loaderConfig.resources.pdb || []).map(fetchPdb)) : Promise.resolve([]);
        const afterRuntimeReadyResources = loaderConfig.resources.modulesAfterRuntimeReady || [];
        const modulesAfterRuntimeReadyPromise = Promise.all(afterRuntimeReadyResources.map(loadJSModule));

        const nativeModule = await nativeModulePromise;
        const modulePromise = nativeModule.dotnetInitializeModule<EmscriptenModuleInternal>(dotnetInternals);
        nativeModulePromiseController.propagateFrom(modulePromise);

        const runtimeModule = await runtimeModulePromise;
        const runtimeModuleReady = runtimeModule.dotnetInitializeModule<void>(dotnetInternals);

        await nativeModulePromiseController.promise;
        runtimeState.nativeReady = true;
        await coreAssembliesPromise;
        await coreVfsPromise;
        await vfsPromise;
        await icuDataPromise;
        await wasmNativePromise; // this is just to propagate errors
        if (!downloadOnly) {
            Module.runtimeKeepalivePush();
            await initializeCoreCLR();
        }

        await assembliesPromise;
        await satelliteResourcesPromise;
        await pdbsPromise;
        await corePDBsPromise;
        await runtimeModuleReady;

        verifyAllAssetsDownloaded();

        if (!downloadOnly) {
            if (typeof Module.onDotnetReady === "function") {
                await Module.onDotnetReady();
            }
            const modulesAfterRuntimeReady = await modulesAfterRuntimeReadyPromise;
            const allRuntimeReadyModules = [...modulesAfterConfigLoaded, ...modulesAfterRuntimeReady];
            const allRuntimeReadyResources = [...afterConfigLoadedResources, ...afterRuntimeReadyResources];
            await callLibraryInitializers(allRuntimeReadyModules, allRuntimeReadyResources, "onRuntimeReady", dotnetApi);
        }
        runtimeState.creatingRuntime = false;
    } catch (err) {
        exit(1, err);
    }
}
export function abortStartup(reason: any): void {
    if (runtimeState.creatingRuntime) {
        nativeModulePromiseController.reject(reason);
    }
}

async function initializeCoreCLR(): Promise<void> {
    dotnetAssert.check(!runtimeState.dotnetReady, "CoreCLR should be initialized just once");
    const res = dotnetBrowserHostExports.initializeCoreCLR();
    if (res != 0) {
        const reason = new Error("Failed to initialize CoreCLR");
        runMainPromiseController.reject(reason);
        exit(res, reason);
    }
    runtimeState.dotnetReady = true;
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


