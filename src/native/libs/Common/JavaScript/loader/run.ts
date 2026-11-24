// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JsModuleExports, EmscriptenModuleInternal, JsAsset, PromiseCompletionSource } from "./types";

import { dotnetAssert, dotnetInternals, dotnetBrowserHostExports, dotnetLogger, Module, startPerformanceMeasure, endPerformanceMeasure } from "./cross-module";
import { exit, isExited, runtimeState } from "./exit";
import { createPromiseCompletionSource, delay } from "./promise-completion-source";
import { getIcuResourceName } from "./icu";
import { loaderConfig, validateLoaderConfig } from "./config";
import { fetchAssembly, fetchIcu, fetchNativeSymbols, fetchPdb, fetchSatelliteAssemblies, fetchVfs, fetchMainWasm, loadDotnetModule, loadJSModule, nativeModulePromiseController, verifyAllAssetsDownloaded, callLibraryInitializerOnRuntimeReady, callLibraryInitializerOnRuntimeConfigLoaded, prefetchAllResources, prefetchJSModuleLinks, resolveAllDownloadsQueued } from "./assets";
import { initPolyfillsLoader } from "./polyfills";
import { validateEngineFeatures } from "./bootstrap";
import { loaderCallbacks } from "./callbacks";
import { getAppSettingsAssets } from "../appsettings";
import { fetchAppSettings, nativeWasmReadyTime, prepareDeferredR2R, wasmMemoryPromiseController } from "./assets";
import type { DeferredR2RPreparation } from "./assets";
import { ENVIRONMENT_IS_WEB } from "../per-module";

const runMainPromiseController = createPromiseCompletionSource<number>();

type DownloadMode = "none" | "cacheOnly" | "intoMemory";
let downloadMode: DownloadMode = "none";
let downloadDeferred: PromiseCompletionSource<void> | undefined;
let downloadedIntoMemory = false;
let configInitialized = false;
let modulesAfterConfigLoadedCache: [JsAsset, Promise<any>][] = [];
let deferredR2RPromise: Promise<void> | undefined;
let deferredR2RPreparedImage: Promise<DeferredR2RPreparation> | undefined;
let deferredR2RCacheHit: Promise<boolean> | undefined;
let deferredR2RHistory: { storage: Storage, key: string, hash: string } | undefined;
let deferredR2RNativeFirst = false;
const deferredR2RActivationGate = createPromiseCompletionSource<void>();
const deferredR2RState = {
    status: "disabled" as "disabled" | "pending" | "downloading" | "activating" | "completed" | "failed" | "exited",
    eagerReadyTime: 0,
    nativeWasmReadyTime: 0,
    downloadQueuedTime: 0,
    downloadStartedTime: 0,
    compiledTime: 0,
    instantiatedTime: 0,
    completedTime: 0,
    publishedMethods: 0,
    activationAttempts: 0,
    interpreterOnlyMethods: 0,
    interpreterSuspensions: 0,
    activationMode: "lazy" as "lazy" | "eager",
    startupMode: "disabled" as "disabled" | "deferred" | "cached-native",
    cacheStatus: "not-checked",
};

export function getDeferredR2RState() {
    deferredR2RState.nativeWasmReadyTime = nativeWasmReadyTime;
    if (deferredR2RState.status !== "disabled" && !deferredR2RNativeFirst && runtimeState.dotnetReady && !isExited()) {
        Object.assign(deferredR2RState, dotnetBrowserHostExports.getDeferredR2RStats());
    }
    return { ...deferredR2RState };
}

export async function waitForDeferredR2R(): Promise<void> {
    if (!deferredR2RPromise) throw new Error("Deferred ReadyToRun has not been started.");
    await deferredR2RPromise;
}

function queueDeferredR2R(): void {
    const assets = loaderConfig.resources?.r2rAssembly;
    if (!assets?.length || deferredR2RPromise) return;
    deferredR2RState.status = "pending";
    deferredR2RState.startupMode = "deferred";
    if (assets.length !== 1 || !assets[0].isCompositeImage || !assets[0].tableSize || !assets[0].payloadSize) {
        throw new Error("Deferred ReadyToRun requires exactly one composite Webcil image with payload and table sizes.");
    }
    deferredR2RState.downloadQueuedTime = performance.now();
    const activationStart = startPerformanceMeasure(loaderConfig);
    const probeCache = readDeferredR2RHistory();
    const preparation = prepareDeferredR2R(assets[0], () => {
        deferredR2RState.status = "downloading";
        deferredR2RState.downloadStartedTime = performance.now();
    }, probeCache, async response => {
        await nativeModulePromiseController.promise;
        await installDeferredR2R(response);
    });
    const preparedImage = deferredR2RPreparedImage = preparation.prepared.then(image => {
        deferredR2RState.compiledTime = "module" in image ? performance.now() : deferredR2RState.instantiatedTime;
        endPerformanceMeasure("dotnet.deferred-r2r.compile", activationStart);
        return image;
    });
    deferredR2RCacheHit = preparation.cached.then(result => {
        if (probeCache) {
            deferredR2RState.cacheStatus = result.reason;
            if (!result.hit) updateDeferredR2RHistory(false);
        }
        return result.hit;
    });
    deferredR2RPromise = activate();
    void deferredR2RPromise.catch(error => {
        if (isExited()) {
            deferredR2RState.status = "exited";
            return;
        }
        deferredR2RState.status = "failed";
        dotnetLogger.error("Deferred ReadyToRun failed", error);
        setTimeout(() => exit(1, error), 0);
    });

    async function activate(): Promise<void> {
        const [image] = await Promise.all([preparedImage, deferredR2RActivationGate.promise, deferredR2RCacheHit]);
        if (!deferredR2RNativeFirst) {
            if (!("module" in image)) throw new Error("Cannot activate a cached ReadyToRun image through the deferred path.");
            await delay(0);
            await installDeferredR2R(image);
            endPerformanceMeasure("dotnet.deferred-r2r.download-and-instantiate", activationStart);
            await delay(0);
            if (isExited()) throw new Error("Runtime exited during deferred ReadyToRun activation.");
            const attachStart = startPerformanceMeasure(loaderConfig);
            dotnetBrowserHostExports.activateDeferredR2R();
            endPerformanceMeasure("dotnet.deferred-r2r.attach", attachStart);
        }
        deferredR2RState.status = "completed";
        deferredR2RState.completedTime = performance.now();
        updateDeferredR2RHistory(true);
        endPerformanceMeasure("dotnet.deferred-r2r.total", activationStart, deferredR2RState);
    }
}

async function installDeferredR2R(input: Response | { module: WebAssembly.Module }): Promise<void> {
    if (isExited()) throw new Error("Runtime exited before deferred ReadyToRun instantiation.");
    deferredR2RState.nativeWasmReadyTime = nativeWasmReadyTime;
    const memory = await wasmMemoryPromiseController.promise;
    const asset = loaderConfig.resources!.r2rAssembly![0];
    await dotnetBrowserHostExports.instantiateWebcilModule(Promise.resolve(input), memory,
        asset.virtualPath!, asset.tableSize, asset.payloadSize);
    deferredR2RState.instantiatedTime = performance.now();
    deferredR2RState.status = "activating";
}

function readDeferredR2RHistory(): boolean {
    if (!ENVIRONMENT_IS_WEB || !loaderConfig.resources?.hash) {
        deferredR2RState.cacheStatus = "history-unavailable";
        return false;
    }
    try {
        const storage = globalThis.localStorage;
        const key = `dotnet-deferred-r2r:v1:${new URL(".", document.baseURI).href}:${loaderConfig.mainAssemblyName}`;
        deferredR2RHistory = { storage, key, hash: loaderConfig.resources.hash };
        const matches = storage.getItem(key) === deferredR2RHistory.hash;
        deferredR2RState.cacheStatus = matches ? "history-hit" : "history-miss";
        return matches;
    } catch (error) {
        if (!(error instanceof DOMException) || (error.name !== "SecurityError" && error.name !== "QuotaExceededError")) throw error;
        deferredR2RHistory = undefined;
        deferredR2RState.cacheStatus = "storage-unavailable";
        dotnetLogger.warn("Deferred ReadyToRun history is unavailable; using deferred startup.", error);
        return false;
    }
}

function updateDeferredR2RHistory(success: boolean): void {
    if (!deferredR2RHistory) return;
    try {
        const { storage, key, hash } = deferredR2RHistory;
        if (success) storage.setItem(key, hash);
        else if (storage.getItem(key) === hash) storage.removeItem(key);
    } catch (error) {
        if (!(error instanceof DOMException) || (error.name !== "SecurityError" && error.name !== "QuotaExceededError")) throw error;
        dotnetLogger.warn("Could not update deferred ReadyToRun history.", error);
    }
}

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
            deferredR2RActivationGate.resolve();

            await loaderCallbacks.dotnetReady?.();

            const resources = loaderConfig.resources;
            // modulesAfterRuntimeReady were only prefetched during download(), now load and call onRuntimeReady.
            const modulesAfterRuntimeReadyPromises: [JsAsset, Promise<any>][] = normalizeCollection(resources.modulesAfterRuntimeReady).map((a) => [a, loadJSModule(a)]);
            // modulesAfterConfigLoaded were loaded during download() — call onRuntimeReady for them too.
            await Promise.all([...modulesAfterConfigLoadedCache, ...modulesAfterRuntimeReadyPromises].map(callLibraryInitializerOnRuntimeReady));
            return;
        }

        const resources = loaderConfig.resources;

        // Run config initialization once: callbacks, modulesAfterConfigLoaded, polyfills.
        // This must happen before any asset fetches so that URL overrides take effect.
        let modulesAfterConfigLoadedPromises: [JsAsset, Promise<any>][] = [];
        if (!configInitialized) {
            await validateEngineFeatures();

            for (const callback of [...loaderCallbacks.configLoaded]) {
                await callback(loaderConfig);
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

            // after config-loaded callbacks that could install polyfills, our polyfills can be initialized
            await initPolyfillsLoader();

            configInitialized = true;
            modulesAfterConfigLoadedCache = modulesAfterConfigLoadedPromises;
        }

        const appsettings = getAppSettingsAssets(loaderConfig);

        // HTTP cache only path: just fetch all resources into browser cache and discard
        if (downloadOnly && httpCacheOnly) {
            await prefetchAllResources(appsettings);
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

        const eagerStart = startPerformanceMeasure(loaderConfig);
        const coreAssembliesPromise = forEachResource(resources.coreAssembly, fetchAssembly);

        const icuResourceName = getIcuResourceName();
        const icuDataPromise = forEachResource(resources.icu, fetchIcu, asset => asset.name === icuResourceName);

        const assembliesPromise = forEachResource(resources.assembly, fetchAssembly);
        const satelliteResourcesPromise = loaderConfig.loadAllSatelliteResources && resources.satelliteResources
            ? fetchSatelliteAssemblies(Object.keys(resources.satelliteResources))
            : Promise.resolve();

        const vfsPromise = forEachResource(resources.vfs, fetchVfs);
        const appsettingsPromise = forEachResource(appsettings, fetchAppSettings);

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

        queueDeferredR2R();
        // Retries follow all first attempts, including the late R2R image.
        resolveAllDownloadsQueued();

        const nativeModule = await nativeModulePromise;
        const modulePromise = nativeModule.dotnetInitializeModule<EmscriptenModuleInternal>(dotnetInternals);
        nativeModulePromiseController.propagateFrom(modulePromise);

        const runtimeModule = await runtimeModulePromise;
        const runtimeModuleReady = runtimeModule.dotnetInitializeModule<void>(dotnetInternals);

        await nativeModulePromiseController.promise;
        runtimeState.nativeReady = true;
        await coreAssembliesPromise;
        await vfsPromise;
        await appsettingsPromise;
        await icuDataPromise;
        await wasmNativePromise; // this is just to propagate errors

        if (!downloadOnly) {
            if (resources.r2rAssembly?.length) await assembliesPromise;
            endPerformanceMeasure("dotnet.eager-assets.ready", eagerStart);
            deferredR2RState.eagerReadyTime = performance.now();
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
            deferredR2RState.eagerReadyTime = performance.now();
            downloadMode = "intoMemory";
            downloadedIntoMemory = true;
            downloadDeferred?.resolve(undefined as unknown as void);
            return;
        }

        deferredR2RActivationGate.resolve();
        await loaderCallbacks.dotnetReady?.();

        await Promise.all([...modulesAfterConfigLoadedPromises, ...modulesAfterRuntimeReadyPromises].map(callLibraryInitializerOnRuntimeReady));

    } catch (err) {
        if (deferredR2RPromise) deferredR2RActivationGate.reject(err);
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
    const assets = loaderConfig.resources?.r2rAssembly;
    if (deferredR2RCacheHit && await deferredR2RCacheHit) {
        deferredR2RNativeFirst = true;
        deferredR2RState.startupMode = "cached-native";
        deferredR2RState.activationMode = "eager";
        await deferredR2RPreparedImage;
    }
    const start = startPerformanceMeasure(loaderConfig);
    const res = dotnetBrowserHostExports.initializeCoreCLR(!!assets?.length && !deferredR2RNativeFirst);
    endPerformanceMeasure("dotnet.vm.initialize", start);
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
    return Promise.all(filteredCollection.map(asset => callback(asset)));
}

function normalizeCollection<T>(collection: T[] | undefined): T[] {
    if (!collection) {
        return [];
    }
    return collection;
}
