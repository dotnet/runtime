// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssetEntryInternal, PromiseAndController } from "../types/internal";
import type { AssetBehaviors, AssetEntry, LoadingResource, ResourceList, ResourceRequest, SingleAssetBehaviors as SingleAssetBehaviors, WebAssemblyBootResourceType } from "../types";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { createPromiseController } from "./promise-controller";
import { mono_log_debug } from "./logging";
import { mono_exit } from "./exit";
import { addCachedReponse, findCachedResponse, isCacheAvailable } from "./assetsCache";
import { getIcuResourceName } from "./icu";
import { mono_log_warn } from "./logging";
import { makeURLAbsoluteWithApplicationBase } from "./polyfills";


let throttlingPromise: PromiseAndController<void> | undefined;
// in order to prevent net::ERR_INSUFFICIENT_RESOURCES if we start downloading too many files at same time
let parallel_count = 0;

const jsModulesAssetTypes: {
    [k: string]: boolean
} = {
    "js-module-threads": true,
    "js-module-runtime": true,
    "js-module-dotnet": true,
    "js-module-native": true,
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
};

const containedInSnapshotByAssetTypes: {
    [k: string]: boolean
} = {
    "resource": true,
    "assembly": true,
    "pdb": true,
    "heap": true,
    "icu": true,
    ...jsModulesAssetTypes,
    "dotnetwasm": true,
};

// these assets are instantiated differently than the main flow
const skipInstantiateByAssetTypes: {
    [k: string]: boolean
} = {
    ...jsModulesAssetTypes,
    "dotnetwasm": true,
    "symbols": true,
};

export function shouldLoadIcuAsset(asset: AssetEntryInternal): boolean {
    return !(asset.behavior == "icu" && asset.name != loaderHelpers.preferredIcuAsset);
}

function getSingleAssetWithResolvedUrl(resources: ResourceList | undefined, behavior: SingleAssetBehaviors): AssetEntry {
    const keys = Object.keys(resources || {});
    mono_assert(keys.length == 1, `Expect to have one ${behavior} asset in resources`);

    const name = keys[0];
    const asset = {
        name,
        hash: resources![name],
        behavior,
        resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name), behavior)
    };

    const customSrc = invokeLoadBootResource(asset);
    if (typeof (customSrc) === "string") {
        asset.resolvedUrl = makeURLAbsoluteWithApplicationBase(customSrc);
    } else if (customSrc) {
        mono_log_warn(`For ${behavior} resource: ${name}, custom loaders must supply a URI string.`);
        // we apply a default URL
    }

    return asset;
}

export function resolve_single_asset_path(behavior: SingleAssetBehaviors): AssetEntryInternal {
    const resources = loaderHelpers.config.resources;
    mono_assert(resources, "Can't find resources in config");

    switch (behavior) {
        case "dotnetwasm":
            return getSingleAssetWithResolvedUrl(resources.wasmNative, behavior);
        case "js-module-threads":
            return getSingleAssetWithResolvedUrl(resources.jsModuleWorker, behavior);
        case "js-module-native":
            return getSingleAssetWithResolvedUrl(resources.jsModuleNative, behavior);
        case "js-module-runtime":
            return getSingleAssetWithResolvedUrl(resources.jsModuleRuntime, behavior);
        default:
            throw new Error(`Unknown single asset behavior ${behavior}`);
    }
}

export async function mono_download_assets(): Promise<void> {
    mono_log_debug("mono_download_assets");
    loaderHelpers.maxParallelDownloads = loaderHelpers.config.maxParallelDownloads || loaderHelpers.maxParallelDownloads;
    loaderHelpers.enableDownloadRetry = loaderHelpers.config.enableDownloadRetry || loaderHelpers.enableDownloadRetry;
    try {
        const alwaysLoadedAssets: AssetEntryInternal[] = [];
        const containedInSnapshotAssets: AssetEntryInternal[] = [];
        const promises_of_assets: Promise<AssetEntryInternal>[] = [];

        prepareAssets(containedInSnapshotAssets, alwaysLoadedAssets);

        const countAndStartDownload = (asset: AssetEntryInternal) => {
            if (!skipInstantiateByAssetTypes[asset.behavior] && shouldLoadIcuAsset(asset)) {
                loaderHelpers.expected_instantiated_assets_count++;
            }
            if (!skipDownloadsByAssetTypes[asset.behavior] && shouldLoadIcuAsset(asset)) {
                loaderHelpers.expected_downloaded_assets_count++;
                promises_of_assets.push(start_asset_download(asset));
            }
        };

        // start fetching assets in parallel, only assets which are not part of memory snapshot
        for (const asset of alwaysLoadedAssets) {
            countAndStartDownload(asset);
        }

        // continue after the dotnet.runtime.js was loaded
        await loaderHelpers.runtimeModuleLoaded.promise;

        // continue after we know if memory snapshot is available or not
        await runtimeHelpers.memorySnapshotSkippedOrDone.promise;

        // start fetching assets in parallel, only if memory snapshot is not available.
        for (const asset of containedInSnapshotAssets) {
            if (!runtimeHelpers.loadedMemorySnapshot) {
                countAndStartDownload(asset);
            } else {
                // Otherwise cleanup in case we were given pending download. It would be even better if we could abort the download.
                cleanupAsset(asset);
                // tell the debugger it is loaded
                if (asset.behavior == "resource" || asset.behavior == "assembly" || asset.behavior == "pdb") {
                    const url = resolve_path(asset, "");
                    const virtualName: string = typeof (asset.virtualPath) === "string"
                        ? asset.virtualPath
                        : asset.name;
                    loaderHelpers._loaded_files.push({ url: url, file: virtualName });
                }
            }
        }

        loaderHelpers.allDownloadsQueued.promise_control.resolve();
        await loaderHelpers.runtimeModuleLoaded.promise;

        const promises_of_asset_instantiation: Promise<void>[] = [];
        for (const downloadPromise of promises_of_assets) {
            promises_of_asset_instantiation.push((async () => {
                const asset = await downloadPromise;
                if (asset.buffer) {
                    if (!skipInstantiateByAssetTypes[asset.behavior]) {
                        mono_assert(asset.buffer && typeof asset.buffer === "object", "asset buffer must be array or buffer like");
                        mono_assert(typeof asset.resolvedUrl === "string", "resolvedUrl must be string");
                        const url = asset.resolvedUrl!;
                        const data = new Uint8Array(asset.buffer!);
                        cleanupAsset(asset);

                        // wait till after onRuntimeInitialized and after memory snapshot is loaded or skipped

                        await runtimeHelpers.beforeOnRuntimeInitialized.promise;
                        await runtimeHelpers.memorySnapshotSkippedOrDone.promise;
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

function prepareAssets(containedInSnapshotAssets: AssetEntryInternal[], alwaysLoadedAssets: AssetEntryInternal[]) {
    const config = loaderHelpers.config;

    // if assets exits, we will assume Net7 legacy and not process resources object
    if (config.assets) {
        for (const a of config.assets) {
            const asset: AssetEntryInternal = a;
            mono_assert(typeof asset === "object", () => `asset must be object, it was ${typeof asset} : ${asset}`);
            mono_assert(typeof asset.behavior === "string", "asset behavior must be known string");
            mono_assert(typeof asset.name === "string", "asset name must be string");
            mono_assert(!asset.resolvedUrl || typeof asset.resolvedUrl === "string", "asset resolvedUrl could be string");
            mono_assert(!asset.hash || typeof asset.hash === "string", "asset resolvedUrl could be string");
            mono_assert(!asset.pendingDownload || typeof asset.pendingDownload === "object", "asset pendingDownload could be object");
            if (containedInSnapshotByAssetTypes[asset.behavior]) {
                containedInSnapshotAssets.push(asset);
            } else {
                alwaysLoadedAssets.push(asset);
            }
        }
    } else if (config.resources) {
        const resources = config.resources;
        if (resources.assembly) {
            for (const name in resources.assembly) {
                containedInSnapshotAssets.push({
                    name,
                    hash: resources.assembly[name],
                    behavior: "assembly"
                });
            }
        }

        if (config.debugLevel != 0 && resources.pdb) {
            for (const name in resources.pdb) {
                containedInSnapshotAssets.push({
                    name,
                    hash: resources.pdb[name],
                    behavior: "pdb"
                });
            }
        }

        if (config.loadAllSatelliteResources && resources.satelliteResources) {
            for (const culture in resources.satelliteResources) {
                for (const name in resources.satelliteResources[culture]) {
                    containedInSnapshotAssets.push({
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
                    alwaysLoadedAssets.push({
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
                    containedInSnapshotAssets.push({
                        name,
                        hash: resources.icu[name],
                        behavior: "icu",
                        loadRemote: true
                    });
                }
            }
        }

        if (resources.wasmSymbols) {
            for (const name in resources.wasmSymbols) {
                alwaysLoadedAssets.push({
                    name,
                    hash: resources.wasmSymbols[name],
                    behavior: "symbols"
                });
            }
        }
    }

    if (config.appsettings) {
        for (let i = 0; i < config.appsettings.length; i++) {
            const configUrl = config.appsettings[i];
            const configFileName = fileName(configUrl);
            if (configFileName === "appsettings.json" || configFileName === `appsettings.${config.applicationEnvironment}.json`) {
                alwaysLoadedAssets.push({
                    name: configFileName,
                    resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(configUrl), "vfs"),
                    behavior: "vfs"
                });
            }
        }
    }

    config.assets = [...containedInSnapshotAssets, ...alwaysLoadedAssets];
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
        const buffer = asset.buffer;
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
    if (loaderHelpers.modulesUniqueQuery && jsModulesAssetTypes[behavior]) {
        attemptUrl = attemptUrl + loaderHelpers.modulesUniqueQuery;
    }

    return attemptUrl;
}

let resourcesLoaded = 0;
const totalResources = new Set<string>();

function download_resource(request: ResourceRequest): LoadingResource {
    try {
        mono_assert(request.resolvedUrl, "Request's resolvedUrl must be set");
        const fetchResponse = download_resource_with_cache(request);
        const response = { name: request.name, url: request.resolvedUrl, response: fetchResponse };

        totalResources.add(request.name!);
        response.response.then(() => {
            if (request.behavior == "assembly") {
                loaderHelpers.loadedAssemblies.push(request.resolvedUrl!);
            }

            resourcesLoaded++;
            if (loaderHelpers.onDownloadResourceProgress)
                loaderHelpers.onDownloadResourceProgress(resourcesLoaded, totalResources.size);
        });
        return response;
    } catch (err) {
        const response = <Response><any>{
            ok: false,
            url: request.resolvedUrl,
            status: 500,
            statusText: "ERR29: " + err,
            arrayBuffer: () => { throw err; },
            json: () => { throw err; }
        };
        return {
            name: request.name, url: request.resolvedUrl!, response: Promise.resolve(response)
        };
    }
}

async function download_resource_with_cache(request: ResourceRequest): Promise<Response> {
    let response = await findCachedResponse(request);
    if (!response) {
        response = await fetchResource(request);
        addCachedReponse(request, response);
    }

    return response;
}

const credentialsIncludeAssetBehaviors: AssetBehaviors[] = ["vfs"]; // Previously only configuration

function fetchResource(request: ResourceRequest): Promise<Response> {
    // Allow developers to override how the resource is loaded
    let url = request.resolvedUrl!;
    if (loaderHelpers.loadBootResource) {
        const customLoadResult = invokeLoadBootResource(request);
        if (customLoadResult instanceof Promise) {
            // They are supplying an entire custom response, so just use that
            return customLoadResult;
        } else if (typeof customLoadResult === "string") {
            url = makeURLAbsoluteWithApplicationBase(customLoadResult);
        }
    }

    const fetchOptions: RequestInit = {
        cache: "no-cache"
    };

    if (credentialsIncludeAssetBehaviors.includes(request.behavior)) {
        // Include credentials so the server can allow download / provide user specific file
        fetchOptions.credentials = "include";
    } else {
        // Any other resource than configuration should provide integrity check
        // Note that if cacheBootResources was explicitly disabled, we also bypass hash checking
        // This is to give developers an easy opt-out from the entire caching/validation flow if
        // there's anything they don't like about it.
        fetchOptions.integrity = isCacheAvailable() ? (request.hash ?? "") : undefined;
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

function invokeLoadBootResource(request: ResourceRequest): string | Promise<Response> | null | undefined {
    if (loaderHelpers.loadBootResource) {
        const requestHash = request.hash ?? "";
        const url = request.resolvedUrl!;

        const resourceType = monoToBlazorAssetTypeMap[request.behavior];
        if (resourceType) {
            return loaderHelpers.loadBootResource(resourceType, request.name, url, requestHash, request.behavior);
        }
    }

    return undefined;
}

export function cleanupAsset(asset: AssetEntryInternal) {
    // give GC chance to collect resources
    asset.pendingDownloadInternal = null as any; // GC
    asset.pendingDownload = null as any; // GC
    asset.buffer = null as any; // GC
}

function fileName(name: string) {
    let lastIndexOfSlash = name.lastIndexOf("/");
    if (lastIndexOfSlash >= 0) {
        lastIndexOfSlash++;
    }
    return name.substring(lastIndexOfSlash);
}