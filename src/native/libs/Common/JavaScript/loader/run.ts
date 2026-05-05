// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JsModuleExports, EmscriptenModuleInternal, JsAsset, PromiseCompletionSource } from "./types";

import { dotnetAssert, dotnetInternals, dotnetBrowserHostExports, Module } from "./cross-module";
import { exit, runtimeState } from "./exit";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { getIcuResourceName } from "./icu";
import { loaderConfig, validateLoaderConfig } from "./config";
import { fetchAssembly, fetchIcu, fetchNativeSymbols, fetchPdb, fetchSatelliteAssemblies, fetchVfs, fetchMainWasm, loadDotnetModule, loadJSModule, nativeModulePromiseController, verifyAllAssetsDownloaded, callLibraryInitializerOnRuntimeReady, callLibraryInitializerOnRuntimeConfigLoaded, prefetchAllResources, prefetchJSModuleLinks } from "./assets";
import { initPolyfills } from "./polyfills";
import { validateEngineFeatures } from "./bootstrap";

const runMainPromiseController = createPromiseCompletionSource<number>();

type DownloadMode = "none" | "cacheOnly" | "intoMemory";
let downloadMode: DownloadMode = "none";
let downloadDeferred: PromiseCompletionSource<void> | undefined;
let downloadedIntoMemory = false;
let configInitialized = false;
let modulesAfterConfigLoadedCache: [JsAsset, Promise<any>][] = [];

// many things happen in parallel here, but order matters for performance!
// ideally we want to utilize network and CPU at the same time
export async function createRuntime(downloadOnly: boolean, httpCacheOnly: boolean = false): Promise<any> {
    if (!loaderConfig.resources || !loaderConfig.resources.coreAssembly || !loaderConfig.resources.coreAssembly.length) throw new Error("Invalid config, resources is not set");
    try {
        runtimeState.creatingRuntime = true;

        // Re-entrancy guard: await any in-flight download, skip if already at requested level
        if (downloadOnly) {
            if (downloadDeferred) {
                await downloadDeferred.promise;
            }
            if (downloadMode === "intoMemory" || (httpCacheOnly && downloadMode === "cacheOnly")) {
                return;
            }
            downloadDeferred = createPromiseCompletionSource<void>();
        }

        // Fast path: download() already loaded everything into memory, create() just needs to init
        if (downloadedIntoMemory && !downloadOnly) {
            Module.runtimeKeepalivePush();
            await initializeCoreCLR();

            if (typeof Module.onDotnetReady === "function") {
                await Module.onDotnetReady();
            }

            const resources = loaderConfig.resources;
            // modulesAfterRuntimeReady were only prefetched during download(), now load and call onRuntimeReady.
            const modulesAfterRuntimeReadyPromises: [JsAsset, Promise<any>][] = normalizeCollection(resources.modulesAfterRuntimeReady).map((a) => [a, loadJSModule(a)]);
            // modulesAfterConfigLoaded were loaded during download() — call onRuntimeReady for them too.
            await Promise.all([...modulesAfterConfigLoadedCache, ...modulesAfterRuntimeReadyPromises].map(callLibraryInitializerOnRuntimeReady));
            return;
        }

        const resources = loaderConfig.resources;

        // Run config initialization once: onConfigLoaded, modulesAfterConfigLoaded, polyfills.
        // This must happen before any asset fetches so that URL overrides take effect.
        let modulesAfterConfigLoadedPromises: [JsAsset, Promise<any>][] = [];
        if (!configInitialized) {
            await validateEngineFeatures();

            if (typeof Module.onConfigLoaded === "function") {
                await Module.onConfigLoaded(loaderConfig);
            }
            validateLoaderConfig();

            modulesAfterConfigLoadedPromises = normalizeCollection(resources.modulesAfterConfigLoaded).map((a) => [a, callLibraryInitializerOnRuntimeConfigLoaded(a)]);
            await Promise.all(modulesAfterConfigLoadedPromises.map(([, p]) => p));

            // Wire user-provided out/err overrides to Emscripten's print/printErr.
            // This must happen before the native module loads so Emscripten picks them up.
            if (!Module.out) {
                // eslint-disable-next-line no-console
                Module.out = console.log.bind(console);
            }
            if (!Module.err) {
                // eslint-disable-next-line no-console
                Module.err = console.error.bind(console);
            }
            if (!Module.print) {
                Module.print = Module.out;
            }
            if (!Module.printErr) {
                Module.printErr = Module.err;
            }

            // after onConfigLoaded hooks that could install polyfills, our polyfills can be initialized
            await initPolyfills();

            configInitialized = true;
            modulesAfterConfigLoadedCache = modulesAfterConfigLoadedPromises;
        }

        // HTTP cache only path: just fetch all resources into browser cache and discard
        if (downloadOnly && httpCacheOnly) {
            await prefetchAllResources();
            downloadMode = "cacheOnly";
            downloadDeferred?.resolve(undefined as unknown as void);
            return;
        }

        if (resources.jsModuleDiagnostics && resources.jsModuleDiagnostics.length > 0) {
            const diagnosticsModule = await loadDotnetModule(resources.jsModuleDiagnostics[0]);
            diagnosticsModule.dotnetInitializeModule<void>(dotnetInternals);
            if (resources.wasmSymbols && resources.wasmSymbols.length > 0) {
                await fetchNativeSymbols(resources.wasmSymbols[0]);
            }
        }
        const nativeModulePromise: Promise<JsModuleExports> = loadDotnetModule(resources.jsModuleNative[0]);
        const runtimeModulePromise: Promise<JsModuleExports> = loadDotnetModule(resources.jsModuleRuntime[0]);
        const wasmNativePromise: Promise<Response> = fetchMainWasm(resources.wasmNative[0]);

        const coreAssembliesPromise = forEachResource(resources.coreAssembly, fetchAssembly);
        const coreVfsPromise = forEachResource(resources.coreVfs, fetchVfs);

        const icuResourceName = getIcuResourceName();
        const icuDataPromise = forEachResource(resources.icu, fetchIcu, asset => asset.name === icuResourceName);

        const assembliesPromise = forEachResource(resources.assembly, fetchAssembly);
        const satelliteResourcesPromise = loaderConfig.loadAllSatelliteResources && resources.satelliteResources
            ? fetchSatelliteAssemblies(Object.keys(resources.satelliteResources))
            : Promise.resolve();
        const vfsPromise = forEachResource(resources.vfs, fetchVfs);

        // WASM-TODO: also check that the debugger is linked in and check feature flags
        const isDebuggingSupported = loaderConfig.debugLevel != 0;
        const corePDBsPromise = forEachResource(resources.corePdb, fetchPdb, () => isDebuggingSupported);
        const pdbsPromise = forEachResource(resources.pdb, fetchPdb, () => isDebuggingSupported);
        // In download-only mode, just add prefetch hints for runtime-ready modules so create() loads them from cache.
        // In create mode, load them now so onRuntimeReady can be called later.
        let modulesAfterRuntimeReadyPromises: [JsAsset, Promise<any>][] = [];
        if (downloadOnly) {
            prefetchJSModuleLinks(normalizeCollection(resources.modulesAfterRuntimeReady));
        } else {
            modulesAfterRuntimeReadyPromises = normalizeCollection(resources.modulesAfterRuntimeReady).map((a) => [a, loadJSModule(a)]);
        }

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

        if (downloadOnly) {
            downloadMode = "intoMemory";
            downloadedIntoMemory = true;
            downloadDeferred?.resolve(undefined as unknown as void);
            return;
        }

        if (typeof Module.onDotnetReady === "function") {
            await Module.onDotnetReady();
        }

        await Promise.all([...modulesAfterConfigLoadedPromises, ...modulesAfterRuntimeReadyPromises].map(callLibraryInitializerOnRuntimeReady));

    } catch (err) {
        downloadDeferred?.reject(err);
        exit(1, err);
    } finally {
        runtimeState.creatingRuntime = false;
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

function forEachResource<T, R>(collection: T[] | undefined, callback: (item: T) => Promise<R>, filter?: (item: T) => boolean): Promise<R[]> {
    if (!collection) {
        return Promise.resolve([]);
    }
    const filteredCollection = filter ? collection.filter(filter) : collection;
    return Promise.all(filteredCollection.map(callback));
}

function normalizeCollection<T>(collection: T[] | undefined): T[] {
    if (!collection) {
        return [];
    }
    return collection;
}
