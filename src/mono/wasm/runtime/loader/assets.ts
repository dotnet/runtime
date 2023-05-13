// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssetEntryInternal, PromiseAndController } from "../types/internal";
import type { AssetBehaviours, AssetEntry, LoadingResource, ResourceRequest } from "../types";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, loaderHelpers, runtimeHelpers } from "./globals";
import { createPromiseController } from "./promise-controller";


let throttlingPromise: PromiseAndController<void> | undefined;
// in order to prevent net::ERR_INSUFFICIENT_RESOURCES if we start downloading too many files at same time
let parallel_count = 0;

// don't `fetch` javaScript files
const skipDownloadsByAssetTypes: {
    [k: string]: boolean
} = {
    "js-module-threads": true,
    "js-module-runtime": true,
    "js-module-native": true,
    "js-module-dotnet": true,
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
    "js-module-threads": true,
    "js-module-runtime": true,
    "js-module-native": true,
    "js-module-dotnet": true,
    "dotnetwasm": true,
};

// these assets are instantiated differently than the main flow
const skipInstantiateByAssetTypes: {
    [k: string]: boolean
} = {
    "js-module-threads": true,
    "js-module-runtime": true,
    "js-module-native": true,
    "js-module-dotnet": true,
    "dotnetwasm": true,
    "symbols": true,
};

export function shouldLoadIcuAsset(asset: AssetEntryInternal): boolean {
    return !(asset.behavior == "icu" && asset.name != loaderHelpers.preferredIcuAsset);
}

export function resolve_asset_path(behavior: AssetBehaviours): AssetEntryInternal {
    const asset: AssetEntryInternal | undefined = loaderHelpers.config.assets?.find(a => a.behavior == behavior);
    mono_assert(asset, () => `Can't find asset for ${behavior}`);
    if (!asset.resolvedUrl) {
        asset.resolvedUrl = resolve_path(asset, "");
    }
    return asset;
}
export async function mono_download_assets(): Promise<void> {
    if (loaderHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_download_assets");
    loaderHelpers.maxParallelDownloads = loaderHelpers.config.maxParallelDownloads || loaderHelpers.maxParallelDownloads;
    loaderHelpers.enableDownloadRetry = loaderHelpers.config.enableDownloadRetry || loaderHelpers.enableDownloadRetry;
    try {
        const alwaysLoadedAssets: AssetEntryInternal[] = [];
        const containedInSnapshotAssets: AssetEntryInternal[] = [];
        const promises_of_assets: Promise<AssetEntryInternal>[] = [];

        for (const a of loaderHelpers.config.assets!) {
            const asset: AssetEntryInternal = a;
            mono_assert(typeof asset === "object", "asset must be object");
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
                        const url = asset.pendingDownloadInternal!.url;
                        mono_assert(asset.buffer && typeof asset.buffer === "object", "asset buffer must be array or buffer like");
                        const data = new Uint8Array(asset.buffer!);
                        cleanupAsset(asset);

                        // wait till after onRuntimeInitialized and after memory snapshot is loaded or skipped

                        await runtimeHelpers.beforeOnRuntimeInitialized.promise;
                        await runtimeHelpers.memorySnapshotSkippedOrDone.promise;
                        runtimeHelpers.instantiate_asset(asset, url, data);
                    }
                    if (asset.behavior === "symbols") {
                        await runtimeHelpers.instantiate_symbols_asset(asset);
                        cleanupAsset(asset);
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
        }).catch(e => {
            loaderHelpers.err("MONO_WASM: Error in mono_download_assets: " + e);
            loaderHelpers.abort_startup(e, true);
        });
        // OPTIMIZATION explained:
        // we do it this way so that we could allocate memory immediately after asset is downloaded (and after onRuntimeInitialized which happened already)
        // spreading in time
        // rather than to block all downloads after onRuntimeInitialized or block onRuntimeInitialized after all downloads are done. That would create allocation burst.
    } catch (e: any) {
        loaderHelpers.err("MONO_WASM: Error in mono_download_assets: " + e);
        throw e;
    }
}

export function delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
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
            return await start_asset_download_with_throttle(asset);
        } catch (err) {
            asset.pendingDownloadInternal = undefined;
            // third attempt after small delay
            await delay(100);
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
            if (loaderHelpers.diagnosticTracing)
                console.debug("MONO_WASM: Throttling further parallel downloads");
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
            if (loaderHelpers.diagnosticTracing)
                console.debug("MONO_WASM: Resuming more parallel downloads");
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
        asset.buffer = null as any; // GC
        asset.pendingDownloadInternal = {
            url: "undefined://" + asset.name,
            name: asset.name,
            response: Promise.resolve({
                arrayBuffer: () => buffer,
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
            if (loaderHelpers.diagnosticTracing)
                console.debug(`MONO_WASM: Attempting to download '${attemptUrl}'`);
        } else {
            if (loaderHelpers.diagnosticTracing)
                console.debug(`MONO_WASM: Attempting to download '${attemptUrl}' for ${asset.name}`);
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
        const err: any = new Error(`MONO_WASM: download '${response.url}' for ${asset.name} failed ${response.status} ${response.statusText}`);
        err.status = response.status;
        throw err;
    } else {
        loaderHelpers.out(`MONO_WASM: optional download '${response.url}' for ${asset.name} failed ${response.status} ${response.statusText}`);
        return undefined;
    }
}

function resolve_path(asset: AssetEntry, sourcePrefix: string): string {
    mono_assert(sourcePrefix !== null && sourcePrefix !== undefined, () => `sourcePrefix must be provided for ${asset.name}`);
    let attemptUrl;
    const assemblyRootFolder = loaderHelpers.config.assemblyRootFolder;
    if (!asset.resolvedUrl) {
        if (sourcePrefix === "") {
            if (asset.behavior === "assembly" || asset.behavior === "pdb") {
                attemptUrl = assemblyRootFolder
                    ? (assemblyRootFolder + "/" + asset.name)
                    : asset.name;
            }
            else if (asset.behavior === "resource") {
                const path = asset.culture && asset.culture !== "" ? `${asset.culture}/${asset.name}` : asset.name;
                attemptUrl = assemblyRootFolder
                    ? (assemblyRootFolder + "/" + path)
                    : path;
            }
            else {
                attemptUrl = asset.name;
            }
        } else {
            attemptUrl = sourcePrefix + asset.name;
        }
        attemptUrl = loaderHelpers.locateFile(attemptUrl);
        if (loaderHelpers.assetUniqueQuery) {
            attemptUrl = attemptUrl + loaderHelpers.assetUniqueQuery;
        }
    }
    else {
        attemptUrl = asset.resolvedUrl;
    }
    mono_assert(attemptUrl && typeof attemptUrl == "string", "attemptUrl need to be path or url string");
    return attemptUrl;
}

function download_resource(request: ResourceRequest): LoadingResource {
    try {
        if (typeof loaderHelpers.downloadResource === "function") {
            const loading = loaderHelpers.downloadResource(request);
            if (loading) return loading;
        }
        const options: any = {};
        if (request.hash) {
            options.integrity = request.hash;
        }
        const response = loaderHelpers.fetch_like(request.resolvedUrl!, options);
        return {
            name: request.name, url: request.resolvedUrl!, response
        };
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

export function cleanupAsset(asset: AssetEntryInternal) {
    // give GC chance to collect resources
    asset.pendingDownloadInternal = null as any; // GC
    asset.pendingDownload = null as any; // GC
    asset.buffer = null as any; // GC
}