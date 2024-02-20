// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { PThreadPtrNull, type AssetEntryInternal, type PThreadWorker, type PromiseAndController } from "../types/internal";
import type { AssetBehaviors, AssetEntry, LoadingResource, ResourceList, SingleAssetBehaviors as SingleAssetBehaviors, WebAssemblyBootResourceType } from "../types";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, ENVIRONMENT_IS_WEB, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { createPromiseController } from "./promise-controller";
import { mono_log_debug, mono_log_warn } from "./logging";
import { mono_exit } from "./exit";
import { addCachedReponse, findCachedResponse } from "./assetsCache";
import { getIcuResourceName } from "./icu";
import { makeURLAbsoluteWithApplicationBase } from "./polyfills";


let throttlingPromise: PromiseAndController<void> | undefined;
// in order to prevent net::ERR_INSUFFICIENT_RESOURCES if we start downloading too many files at same time
let parallel_count = 0;
const assetsToLoad: AssetEntryInternal[] = [];
const singleAssets: Map<string, AssetEntryInternal> = new Map();

// A duplicate in pthreads/shared.ts
const worker_empty_prefix = "          -    ";

const jsRuntimeModulesAssetTypes: {
    [k: string]: boolean
} = {
    "js-module-threads": true,
    "js-module-runtime": true,
    "js-module-dotnet": true,
    "js-module-native": true,
};

const jsModulesAssetTypes: {
    [k: string]: boolean
} = {
    ...jsRuntimeModulesAssetTypes,
    "js-module-library-initializer": true,
};

const singleAssetTypes: {
    [k: string]: boolean
} = {
    ...jsRuntimeModulesAssetTypes,
    "dotnetwasm": true,
    "heap": true,
    "manifest": true,
};

// append query to asset url to prevent reusing state
const appendQueryAssetTypes: {
    [k: string]: boolean
} = {
    ...jsModulesAssetTypes,
    "manifest": true,
};

// don't `fetch` javaScript and wasm files
const skipDownloadsByAssetTypes: {
    [k: string]: boolean
} = {
    ...jsModulesAssetTypes,
    "dotnetwasm": true,
};

// `response.arrayBuffer()` can't be called twice. Some usecases are calling it on response in the instantiation.
const skipBufferByAssetTypes: {
    [k: string]: boolean
} = {
    "dotnetwasm": true,
    "symbols": true,
    "segmentation-rules": true,
};

// these assets are instantiated differently than the main flow
const skipInstantiateByAssetTypes: {
    [k: string]: boolean
} = {
    ...jsModulesAssetTypes,
    "dotnetwasm": true,
    "symbols": true,
    "segmentation-rules": true,
};

// load again for each worker
const loadIntoWorker: {
    [k: string]: boolean
} = {
    "symbols": true,
    "segmentation-rules": true,
};

export function shouldLoadIcuAsset(asset: AssetEntryInternal): boolean {
    return !(asset.behavior == "icu" && asset.name != loaderHelpers.preferredIcuAsset);
}

function convert_single_asset(assetsCollection: AssetEntryInternal[], resource: ResourceList | undefined, behavior: SingleAssetBehaviors): AssetEntryInternal {
    const keys = Object.keys(resource || {});
    mono_assert(keys.length == 1, `Expect to have one ${behavior} asset in resources`);

    const name = keys[0];

    const asset = {
        name,
        hash: resource![name],
        behavior,
    };

    set_single_asset(asset);

    // so that we can use it on the worker too
    assetsCollection.push(asset);
    return asset;
}

function set_single_asset(asset: AssetEntryInternal) {
    if (singleAssetTypes[asset.behavior]) {
        singleAssets.set(asset.behavior, asset);
    }
}

function get_single_asset(behavior: SingleAssetBehaviors): AssetEntryInternal {
    mono_assert(singleAssetTypes[behavior], `Unknown single asset behavior ${behavior}`);
    const asset = singleAssets.get(behavior);
    mono_assert(asset, `Single asset for ${behavior} not found`);
    return asset;
}

export function resolve_single_asset_path(behavior: SingleAssetBehaviors): AssetEntryInternal {
    const asset = get_single_asset(behavior);
    if (!asset.resolvedUrl) {
        asset.resolvedUrl = loaderHelpers.locateFile(asset.name);

        if (jsRuntimeModulesAssetTypes[asset.behavior]) {
            // give loadBootResource chance to override the url for JS modules with 'dotnetjs' type
            const customLoadResult = invokeLoadBootResource(asset);
            if (customLoadResult) {
                mono_assert(typeof customLoadResult === "string", "loadBootResource response for 'dotnetjs' type should be a URL string");
                asset.resolvedUrl = customLoadResult;
            } else {
                asset.resolvedUrl = appendUniqueQuery(asset.resolvedUrl, asset.behavior);
            }
        } else if (asset.behavior !== "dotnetwasm") {
            throw new Error(`Unknown single asset behavior ${behavior}`);
        }
    }
    return asset;
}

export async function mono_download_assets(): Promise<void> {
    mono_log_debug("mono_download_assets");
    try {
        const promises_of_assets: Promise<AssetEntryInternal>[] = [];

        const countAndStartDownload = (asset: AssetEntryInternal) => {
            if (!skipInstantiateByAssetTypes[asset.behavior] && shouldLoadIcuAsset(asset)) {
                loaderHelpers.expected_instantiated_assets_count++;
            }
            if (!skipDownloadsByAssetTypes[asset.behavior] && shouldLoadIcuAsset(asset)) {
                loaderHelpers.expected_downloaded_assets_count++;
                promises_of_assets.push(start_asset_download(asset));
            }
        };

        // start fetching assets in parallel
        for (const asset of assetsToLoad) {
            countAndStartDownload(asset);
        }

        loaderHelpers.allDownloadsQueued.promise_control.resolve();

        // continue after the dotnet.runtime.js was loaded
        await loaderHelpers.runtimeModuleLoaded.promise;

        const promises_of_asset_instantiation: Promise<void>[] = [];
        for (const downloadPromise of promises_of_assets) {
            promises_of_asset_instantiation.push((async () => {
                const asset = await downloadPromise;
                if (asset.buffer) {
                    if (!skipInstantiateByAssetTypes[asset.behavior]) {
                        mono_assert(asset.buffer && typeof asset.buffer === "object", "asset buffer must be array-like or buffer-like or promise of these");
                        mono_assert(typeof asset.resolvedUrl === "string", "resolvedUrl must be string");
                        const url = asset.resolvedUrl!;
                        const buffer = await asset.buffer;
                        const data = new Uint8Array(buffer);
                        cleanupAsset(asset);

                        // wait till after onRuntimeInitialized

                        await runtimeHelpers.beforeOnRuntimeInitialized.promise;
                        runtimeHelpers.instantiate_asset(asset, url, data);
                    }
                } else {
                    const headersOnly = skipBufferByAssetTypes[asset.behavior];
                    if (!headersOnly) {
                        mono_assert(asset.isOptional, "Expected asset to have the downloaded buffer");
                        if (!skipDownloadsByAssetTypes[asset.behavior] && shouldLoadIcuAsset(asset)) {
                            loaderHelpers.expected_downloaded_assets_count--;
                        }
                        if (!skipInstantiateByAssetTypes[asset.behavior] && shouldLoadIcuAsset(asset)) {
                            loaderHelpers.expected_instantiated_assets_count--;
                        }
                    } else {
                        if (asset.behavior === "symbols") {
                            await runtimeHelpers.instantiate_symbols_asset(asset);
                            cleanupAsset(asset);
                        } else if (asset.behavior === "segmentation-rules") {
                            await runtimeHelpers.instantiate_segmentation_rules_asset(asset);
                            cleanupAsset(asset);
                        }

                        if (skipBufferByAssetTypes[asset.behavior]) {
                            ++loaderHelpers.actual_downloaded_assets_count;
                        }
                    }
                }
            })());
        }

        // this await will get past the onRuntimeInitialized because we are not blocking via addRunDependency
        // and we are not awating it here
        Promise.all(promises_of_asset_instantiation).then(() => {
            runtimeHelpers.allAssetsInMemory.promise_control.resolve();
        }).catch(err => {
            loaderHelpers.err("Error in mono_download_assets: " + err);
            mono_exit(1, err);
            throw err;
        });
        // OPTIMIZATION explained:
        // we do it this way so that we could allocate memory immediately after asset is downloaded (and after onRuntimeInitialized which happened already)
        // spreading in time
        // rather than to block all downloads after onRuntimeInitialized or block onRuntimeInitialized after all downloads are done. That would create allocation burst.
    } catch (e: any) {
        loaderHelpers.err("Error in mono_download_assets: " + e);
        throw e;
    }
}

export function prepareAssets() {
    const config = loaderHelpers.config;
    const modulesAssets: AssetEntryInternal[] = [];

    // if assets exits, we will assume Net7 legacy and not process resources object
    if (config.assets) {
        for (const asset of config.assets) {
            mono_assert(typeof asset === "object", () => `asset must be object, it was ${typeof asset} : ${asset}`);
            mono_assert(typeof asset.behavior === "string", "asset behavior must be known string");
            mono_assert(typeof asset.name === "string", "asset name must be string");
            mono_assert(!asset.resolvedUrl || typeof asset.resolvedUrl === "string", "asset resolvedUrl could be string");
            mono_assert(!asset.hash || typeof asset.hash === "string", "asset resolvedUrl could be string");
            mono_assert(!asset.pendingDownload || typeof asset.pendingDownload === "object", "asset pendingDownload could be object");
            assetsToLoad.push(asset);
            set_single_asset(asset);
        }
    } else if (config.resources) {
        const resources = config.resources;

        mono_assert(resources.wasmNative, "resources.wasmNative must be defined");
        mono_assert(resources.jsModuleNative, "resources.jsModuleNative must be defined");
        mono_assert(resources.jsModuleRuntime, "resources.jsModuleRuntime must be defined");
        mono_assert(!WasmEnableThreads || resources.jsModuleWorker, "resources.jsModuleWorker must be defined");
        convert_single_asset(assetsToLoad, resources.wasmNative, "dotnetwasm");
        convert_single_asset(modulesAssets, resources.jsModuleNative, "js-module-native");
        convert_single_asset(modulesAssets, resources.jsModuleRuntime, "js-module-runtime");
        if (WasmEnableThreads) {
            convert_single_asset(modulesAssets, resources.jsModuleWorker, "js-module-threads");
        }

        if (resources.assembly) {
            for (const name in resources.assembly) {
                assetsToLoad.push({
                    name,
                    hash: resources.assembly[name],
                    behavior: "assembly"
                });
            }
        }

        if (config.debugLevel != 0 && loaderHelpers.isDebuggingSupported() && resources.pdb) {
            for (const name in resources.pdb) {
                assetsToLoad.push({
                    name,
                    hash: resources.pdb[name],
                    behavior: "pdb"
                });
            }
        }

        if (config.loadAllSatelliteResources && resources.satelliteResources) {
            for (const culture in resources.satelliteResources) {
                for (const name in resources.satelliteResources[culture]) {
                    assetsToLoad.push({
                        name,
                        hash: resources.satelliteResources[culture][name],
                        behavior: "resource",
                        culture
                    });
                }
            }
        }

        if (resources.vfs) {
            for (const virtualPath in resources.vfs) {
                for (const name in resources.vfs[virtualPath]) {
                    assetsToLoad.push({
                        name,
                        hash: resources.vfs[virtualPath][name],
                        behavior: "vfs",
                        virtualPath
                    });
                }
            }
        }

        const icuDataResourceName = getIcuResourceName(config);
        if (icuDataResourceName && resources.icu) {
            for (const name in resources.icu) {
                if (name === icuDataResourceName) {
                    assetsToLoad.push({
                        name,
                        hash: resources.icu[name],
                        behavior: "icu",
                        loadRemote: true
                    });
                } else if (name === "segmentation-rules.json") {
                    assetsToLoad.push({
                        name,
                        hash: resources.icu[name],
                        behavior: "segmentation-rules",
                    });
                }
            }
        }

        if (resources.wasmSymbols) {
            for (const name in resources.wasmSymbols) {
                assetsToLoad.push({
                    name,
                    hash: resources.wasmSymbols[name],
                    behavior: "symbols"
                });
            }
        }
    }

    // FIXME: should we also load Net7 backward compatible `config.configs` in a same way ?
    if (config.appsettings) {
        for (let i = 0; i < config.appsettings.length; i++) {
            const configUrl = config.appsettings[i];
            const configFileName = fileName(configUrl);
            if (configFileName === "appsettings.json" || configFileName === `appsettings.${config.applicationEnvironment}.json`) {
                assetsToLoad.push({
                    name: configUrl,
                    behavior: "vfs",
                    // TODO what should be the virtualPath ?
                    noCache: true,
                    useCredentials: true
                });
            }
            // FIXME: why are not loading all the other named files in appsettings ? https://github.com/dotnet/runtime/issues/89861
        }
    }

    config.assets = [...assetsToLoad, ...modulesAssets];
}

export function prepareAssetsWorker() {
    const config = loaderHelpers.config;
    mono_assert(config.assets, "config.assets must be defined");

    for (const asset of config.assets) {
        set_single_asset(asset);
        if (loadIntoWorker[asset.behavior]) {
            assetsToLoad.push(asset);
        }
    }
}

export function delay(ms: number): Promise<void> {
    return new Promise(resolve => globalThis.setTimeout(resolve, ms));
}

export async function retrieve_asset_download(asset: AssetEntry): Promise<ArrayBuffer> {
    const pendingAsset = await start_asset_download(asset);
    await pendingAsset.pendingDownloadInternal!.response;
    return pendingAsset.buffer!;
}

// FIXME: Connection reset is probably the only good one for which we should retry
export async function start_asset_download(asset: AssetEntryInternal): Promise<AssetEntryInternal> {
    try {
        return await start_asset_download_with_throttle(asset);
    } catch (err: any) {
        if (!loaderHelpers.enableDownloadRetry) {
            // we will not re-try if disabled
            throw err;
        }
        if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE) {
            // we will not re-try on shell
            throw err;
        }
        if (asset.pendingDownload && asset.pendingDownloadInternal == asset.pendingDownload) {
            // we will not re-try with external source
            throw err;
        }
        if (asset.resolvedUrl && asset.resolvedUrl.indexOf("file://") != -1) {
            // we will not re-try with local file
            throw err;
        }
        if (err && err.status == 404) {
            // we will not re-try with 404
            throw err;
        }
        asset.pendingDownloadInternal = undefined;
        // second attempt only after all first attempts are queued
        await loaderHelpers.allDownloadsQueued.promise;
        try {
            mono_log_debug(`Retrying download '${asset.name}'`);
            return await start_asset_download_with_throttle(asset);
        } catch (err) {
            asset.pendingDownloadInternal = undefined;
            // third attempt after small delay
            await delay(100);

            mono_log_debug(`Retrying download (2) '${asset.name}' after delay`);
            return await start_asset_download_with_throttle(asset);
        }
    }
}

async function start_asset_download_with_throttle(asset: AssetEntryInternal): Promise<AssetEntryInternal> {
    // we don't addRunDependency to allow download in parallel with onRuntimeInitialized event!
    while (throttlingPromise) {
        await throttlingPromise.promise;
    }
    try {
        ++parallel_count;
        if (parallel_count == loaderHelpers.maxParallelDownloads) {
            mono_log_debug("Throttling further parallel downloads");
            throttlingPromise = createPromiseController<void>();
        }

        const response = await start_asset_download_sources(asset);
        if (!response) {
            return asset;
        }
        const skipBuffer = skipBufferByAssetTypes[asset.behavior];
        if (skipBuffer) {
            return asset;
        }
        asset.buffer = await response.arrayBuffer();
        ++loaderHelpers.actual_downloaded_assets_count;
        return asset;
    }
    finally {
        --parallel_count;
        if (throttlingPromise && parallel_count == loaderHelpers.maxParallelDownloads - 1) {
            mono_log_debug("Resuming more parallel downloads");
            const old_throttling = throttlingPromise;
            throttlingPromise = undefined;
            old_throttling.promise_control.resolve();
        }
    }
}

async function start_asset_download_sources(asset: AssetEntryInternal): Promise<Response | undefined> {
    // we don't addRunDependency to allow download in parallel with onRuntimeInitialized event!
    if (asset.pendingDownload) {
        asset.pendingDownloadInternal = asset.pendingDownload;
    }
    if (asset.pendingDownloadInternal && asset.pendingDownloadInternal.response) {
        return asset.pendingDownloadInternal.response;
    }
    if (asset.buffer) {
        const buffer = await asset.buffer;
        if (!asset.resolvedUrl) {
            asset.resolvedUrl = "undefined://" + asset.name;
        }
        asset.pendingDownloadInternal = {
            url: asset.resolvedUrl,
            name: asset.name,
            response: Promise.resolve({
                ok: true,
                arrayBuffer: () => buffer,
                json: () => JSON.parse(new TextDecoder("utf-8").decode(buffer)),
                text: () => { throw new Error("NotImplementedException"); },
                headers: {
                    get: () => undefined,
                }
            }) as any
        };
        return asset.pendingDownloadInternal.response;
    }

    const sourcesList = asset.loadRemote && loaderHelpers.config.remoteSources ? loaderHelpers.config.remoteSources : [""];
    let response: Response | undefined = undefined;
    for (let sourcePrefix of sourcesList) {
        sourcePrefix = sourcePrefix.trim();
        // HACK: Special-case because MSBuild doesn't allow "" as an attribute
        if (sourcePrefix === "./")
            sourcePrefix = "";

        const attemptUrl = resolve_path(asset, sourcePrefix);
        if (asset.name === attemptUrl) {
            mono_log_debug(`Attempting to download '${attemptUrl}'`);
        } else {
            mono_log_debug(`Attempting to download '${attemptUrl}' for ${asset.name}`);
        }
        try {
            asset.resolvedUrl = attemptUrl;
            const loadingResource = download_resource(asset);
            asset.pendingDownloadInternal = loadingResource;
            response = await loadingResource.response;
            if (!response || !response.ok) {
                continue;// next source
            }
            return response;
        }
        catch (err) {
            if (!response) {
                response = {
                    ok: false,
                    url: attemptUrl,
                    status: 0,
                    statusText: "" + err,
                } as any;
            }
            continue; //next source
        }
    }
    const isOkToFail = asset.isOptional || (asset.name.match(/\.pdb$/) && loaderHelpers.config.ignorePdbLoadErrors);
    mono_assert(response, () => `Response undefined ${asset.name}`);
    if (!isOkToFail) {
        const err: any = new Error(`download '${response.url}' for ${asset.name} failed ${response.status} ${response.statusText}`);
        err.status = response.status;
        throw err;
    } else {
        loaderHelpers.out(`optional download '${response.url}' for ${asset.name} failed ${response.status} ${response.statusText}`);
        return undefined;
    }
}

function resolve_path(asset: AssetEntry, sourcePrefix: string): string {
    mono_assert(sourcePrefix !== null && sourcePrefix !== undefined, () => `sourcePrefix must be provided for ${asset.name}`);
    let attemptUrl;
    if (!asset.resolvedUrl) {
        if (sourcePrefix === "") {
            if (asset.behavior === "assembly" || asset.behavior === "pdb") {
                attemptUrl = asset.name;
            }
            else if (asset.behavior === "resource") {
                const path = asset.culture && asset.culture !== "" ? `${asset.culture}/${asset.name}` : asset.name;
                attemptUrl = path;
            }
            else {
                attemptUrl = asset.name;
            }
        } else {
            attemptUrl = sourcePrefix + asset.name;
        }
        attemptUrl = appendUniqueQuery(loaderHelpers.locateFile(attemptUrl), asset.behavior);
    }
    else {
        attemptUrl = asset.resolvedUrl;
    }
    mono_assert(attemptUrl && typeof attemptUrl == "string", "attemptUrl need to be path or url string");
    return attemptUrl;
}

export function appendUniqueQuery(attemptUrl: string, behavior: AssetBehaviors): string {
    // apply unique query to js modules to make the module state independent of the other runtime instances
    if (loaderHelpers.modulesUniqueQuery && appendQueryAssetTypes[behavior]) {
        attemptUrl = attemptUrl + loaderHelpers.modulesUniqueQuery;
    }

    return attemptUrl;
}

let resourcesLoaded = 0;
const totalResources = new Set<string>();

function download_resource(asset: AssetEntryInternal): LoadingResource {
    try {
        mono_assert(asset.resolvedUrl, "Request's resolvedUrl must be set");
        const fetchResponse = download_resource_with_cache(asset);
        const response = { name: asset.name, url: asset.resolvedUrl, response: fetchResponse };

        totalResources.add(asset.name!);
        response.response.then(() => {
            if (asset.behavior == "assembly") {
                loaderHelpers.loadedAssemblies.push(asset.name);
            }

            resourcesLoaded++;
            if (loaderHelpers.onDownloadResourceProgress)
                loaderHelpers.onDownloadResourceProgress(resourcesLoaded, totalResources.size);
        });
        return response;
    } catch (err) {
        const response = <Response><any>{
            ok: false,
            url: asset.resolvedUrl,
            status: 500,
            statusText: "ERR29: " + err,
            arrayBuffer: () => { throw err; },
            json: () => { throw err; }
        };
        return {
            name: asset.name, url: asset.resolvedUrl!, response: Promise.resolve(response)
        };
    }
}

async function download_resource_with_cache(asset: AssetEntryInternal): Promise<Response> {
    let response = await findCachedResponse(asset);
    if (!response) {
        response = await fetchResource(asset);
        addCachedReponse(asset, response);
    }

    return response;
}

function fetchResource(asset: AssetEntryInternal): Promise<Response> {
    // Allow developers to override how the resource is loaded
    let url = asset.resolvedUrl!;
    if (loaderHelpers.loadBootResource) {
        const customLoadResult = invokeLoadBootResource(asset);
        if (customLoadResult instanceof Promise) {
            // They are supplying an entire custom response, so just use that
            return customLoadResult;
        } else if (typeof customLoadResult === "string") {
            url = customLoadResult;
        }
    }

    const fetchOptions: RequestInit = {};
    if (!loaderHelpers.config.disableNoCacheFetch) {
        // FIXME: "no-cache" is how blazor works in Net7, but this prevents caching on HTTP level
        // if we would like to get rid of our own cache and only use HTTP cache, we need to remove this
        // https://github.com/dotnet/runtime/issues/74815
        fetchOptions.cache = "no-cache";
    }
    if (asset.useCredentials) {
        // Include credentials so the server can allow download / provide user specific file
        fetchOptions.credentials = "include";
    } else {
        // `disableIntegrityCheck` is to give developers an easy opt-out from the integrity check 
        if (!loaderHelpers.config.disableIntegrityCheck && asset.hash) {
            // Any other resource than configuration should provide integrity check
            fetchOptions.integrity = asset.hash;
        }
    }

    return loaderHelpers.fetch_like(url, fetchOptions);
}

const monoToBlazorAssetTypeMap: { [key: string]: WebAssemblyBootResourceType | undefined } = {
    "resource": "assembly",
    "assembly": "assembly",
    "pdb": "pdb",
    "icu": "globalization",
    "vfs": "configuration",
    "manifest": "manifest",
    "dotnetwasm": "dotnetwasm",
    "js-module-dotnet": "dotnetjs",
    "js-module-native": "dotnetjs",
    "js-module-runtime": "dotnetjs",
    "js-module-threads": "dotnetjs"
};

function invokeLoadBootResource(asset: AssetEntryInternal): string | Promise<Response> | null | undefined {
    if (loaderHelpers.loadBootResource) {
        const requestHash = asset.hash ?? "";
        const url = asset.resolvedUrl!;

        const resourceType = monoToBlazorAssetTypeMap[asset.behavior];
        if (resourceType) {
            const customLoadResult = loaderHelpers.loadBootResource(resourceType, asset.name, url, requestHash, asset.behavior);
            if (typeof customLoadResult === "string") {
                return makeURLAbsoluteWithApplicationBase(customLoadResult);
            }
            return customLoadResult;
        }
    }

    return undefined;
}

export function cleanupAsset(asset: AssetEntryInternal) {
    // give GC chance to collect resources
    asset.pendingDownloadInternal = null as any; // GC
    asset.pendingDownload = null as any; // GC
    asset.buffer = null as any; // GC
    asset.moduleExports = null as any; // GC
}

function fileName(name: string) {
    let lastIndexOfSlash = name.lastIndexOf("/");
    if (lastIndexOfSlash >= 0) {
        lastIndexOfSlash++;
    }
    return name.substring(lastIndexOfSlash);
}

export async function streamingCompileWasm() {
    try {
        const wasmModuleAsset = resolve_single_asset_path("dotnetwasm");
        await start_asset_download(wasmModuleAsset);
        mono_assert(wasmModuleAsset && wasmModuleAsset.pendingDownloadInternal && wasmModuleAsset.pendingDownloadInternal.response, "Can't load dotnet.native.wasm");
        const response = await wasmModuleAsset.pendingDownloadInternal.response;
        const contentType = response.headers && response.headers.get ? response.headers.get("Content-Type") : undefined;
        let compiledModule: WebAssembly.Module;
        if (typeof WebAssembly.compileStreaming === "function" && contentType === "application/wasm") {
            compiledModule = await WebAssembly.compileStreaming(response);
        } else {
            if (ENVIRONMENT_IS_WEB && contentType !== "application/wasm") {
                mono_log_warn("WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.");
            }
            const arrayBuffer = await response.arrayBuffer();
            mono_log_debug("instantiate_wasm_module buffered");
            if (ENVIRONMENT_IS_SHELL) {
                // workaround for old versions of V8 with https://bugs.chromium.org/p/v8/issues/detail?id=13823
                compiledModule = await Promise.resolve(new WebAssembly.Module(arrayBuffer));
            } else {
                compiledModule = await WebAssembly.compile(arrayBuffer!);
            }
        }
        wasmModuleAsset.pendingDownloadInternal = null as any; // GC
        wasmModuleAsset.pendingDownload = null as any; // GC
        wasmModuleAsset.buffer = null as any; // GC
        wasmModuleAsset.moduleExports = null as any; // GC
        loaderHelpers.wasmCompilePromise.promise_control.resolve(compiledModule);
    }
    catch (err) {
        loaderHelpers.wasmCompilePromise.promise_control.reject(err);
    }
}

export function preloadWorkers() {
    if (!WasmEnableThreads) return;
    const jsModuleWorker = resolve_single_asset_path("js-module-threads");
    for (let i = 0; i < loaderHelpers.config.pthreadPoolInitialSize!; i++) {
        const workerNumber = loaderHelpers.workerNextNumber++;
        const worker: Partial<PThreadWorker> = new Worker(jsModuleWorker.resolvedUrl!, {
            name: "dotnet-worker-" + workerNumber.toString().padStart(3, "0"),
        });
        worker.info = {
            workerNumber,
            pthreadId: PThreadPtrNull,
            reuseCount: 0,
            updateCount: 0,
            threadPrefix: worker_empty_prefix,
            threadName: "emscripten-pool",
        } as any;
        loaderHelpers.loadingWorkers.push(worker as any);
    }
}
