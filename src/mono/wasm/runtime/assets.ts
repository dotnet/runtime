// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";
import { mono_wasm_load_icu_data } from "./icu";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, ENVIRONMENT_IS_WEB, Module, runtimeHelpers } from "./globals";
import { parseSymbolMapFile } from "./logging";
import { mono_wasm_load_bytes_into_heap } from "./memory";
import { endMeasure, MeasuredBlock, startMeasure } from "./profiler";
import { createPromiseController, PromiseAndController } from "./promise-controller";
import { delay } from "./promise-utils";
import { abort_startup, beforeOnRuntimeInitialized, memorySnapshotSkippedOrDone } from "./startup";
import { AssetEntryInternal, mono_assert } from "./types";
import { AssetBehaviours, AssetEntry, LoadingResource, ResourceRequest } from "./types-api";
import { InstantiateWasmSuccessCallback, VoidPtr } from "./types/emscripten";

const allAssetsInMemory = createPromiseController<void>();
const allDownloadsQueued = createPromiseController<void>();
let actual_downloaded_assets_count = 0;
let actual_instantiated_assets_count = 0;
let expected_downloaded_assets_count = 0;
let expected_instantiated_assets_count = 0;
const loaded_files: { url: string, file: string }[] = [];
// in order to prevent net::ERR_INSUFFICIENT_RESOURCES if we start downloading too many files at same time
let parallel_count = 0;
let throttlingPromise: PromiseAndController<void> | undefined;

// don't `fetch` javaScript files
const skipDownloadsByAssetTypes: {
    [k: string]: boolean
} = {
    "js-module-threads": true,
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
    "dotnetwasm": true,
};


// these assets are instantiated differently than the main flow
const skipInstantiateByAssetTypes: {
    [k: string]: boolean
} = {
    "js-module-threads": true,
    "dotnetwasm": true,
    "symbols": true,
};

export function shouldLoadIcuAsset(asset: AssetEntryInternal): boolean {
    return !(asset.behavior == "icu" && asset.name != runtimeHelpers.preferredIcuAsset);
}

export function resolve_asset_path(behavior: AssetBehaviours) {
    const asset: AssetEntryInternal | undefined = runtimeHelpers.config.assets?.find(a => a.behavior == behavior);
    mono_assert(asset, () => `Can't find asset for ${behavior}`);
    if (!asset.resolvedUrl) {
        asset.resolvedUrl = resolve_path(asset, "");
    }
    return asset;
}
export async function mono_download_assets(): Promise<void> {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_download_assets");
    runtimeHelpers.maxParallelDownloads = runtimeHelpers.config.maxParallelDownloads || runtimeHelpers.maxParallelDownloads;
    runtimeHelpers.enableDownloadRetry = runtimeHelpers.config.enableDownloadRetry || runtimeHelpers.enableDownloadRetry;
    try {
        const alwaysLoadedAssets: AssetEntryInternal[] = [];
        const containedInSnapshotAssets: AssetEntryInternal[] = [];
        const promises_of_assets: Promise<AssetEntryInternal>[] = [];

        for (const a of runtimeHelpers.config.assets!) {
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
                expected_instantiated_assets_count++;
            }
            if (!skipDownloadsByAssetTypes[asset.behavior] && shouldLoadIcuAsset(asset)) {
                expected_downloaded_assets_count++;
                promises_of_assets.push(start_asset_download(asset));
            }
        };

        // start fetching assets in parallel, only assets which are not part of memory snapshot
        for (const asset of alwaysLoadedAssets) {
            countAndStartDownload(asset);
        }

        // continue after we know if memory snapshot is available or not
        await memorySnapshotSkippedOrDone.promise;

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
                    loaded_files.push({ url: url, file: virtualName });
                }
            }
        }

        allDownloadsQueued.promise_control.resolve();

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
                        await memorySnapshotSkippedOrDone.promise;
                        await beforeOnRuntimeInitialized.promise;
                        _instantiate_asset(asset, url, data);
                    }
                    if (asset.behavior === "symbols") {
                        await instantiate_symbols_asset(asset);
                        cleanupAsset(asset);
                    }
                } else {
                    const headersOnly = skipBufferByAssetTypes[asset.behavior];
                    if (!headersOnly) {
                        mono_assert(asset.isOptional, "Expected asset to have the downloaded buffer");
                        if (!skipDownloadsByAssetTypes[asset.behavior] && shouldLoadIcuAsset(asset)) {
                            expected_downloaded_assets_count--;
                        }
                        if (!skipInstantiateByAssetTypes[asset.behavior] && shouldLoadIcuAsset(asset)) {
                            expected_instantiated_assets_count--;
                        }
                    } else {
                        if (skipBufferByAssetTypes[asset.behavior]) {
                            ++actual_downloaded_assets_count;
                        }
                    }
                }
            })());
        }

        // this await will get past the onRuntimeInitialized because we are not blocking via addRunDependency
        // and we are not awating it here
        Promise.all(promises_of_asset_instantiation).then(() => {
            allAssetsInMemory.promise_control.resolve();
        }).catch(e => {
            Module.err("MONO_WASM: Error in mono_download_assets: " + e);
            abort_startup(e, true);
        });
        // OPTIMIZATION explained:
        // we do it this way so that we could allocate memory immediately after asset is downloaded (and after onRuntimeInitialized which happened already)
        // spreading in time
        // rather than to block all downloads after onRuntimeInitialized or block onRuntimeInitialized after all downloads are done. That would create allocation burst.
    } catch (e: any) {
        Module.err("MONO_WASM: Error in mono_download_assets: " + e);
        throw e;
    }
}

// FIXME: Connection reset is probably the only good one for which we should retry
export async function start_asset_download(asset: AssetEntryInternal): Promise<AssetEntryInternal> {
    try {
        return await start_asset_download_with_throttle(asset);
    } catch (err: any) {
        if (!runtimeHelpers.enableDownloadRetry) {
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
        await allDownloadsQueued.promise;
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
        if (parallel_count == runtimeHelpers.maxParallelDownloads) {
            if (runtimeHelpers.diagnosticTracing)
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
        ++actual_downloaded_assets_count;
        return asset;
    }
    finally {
        --parallel_count;
        if (throttlingPromise && parallel_count == runtimeHelpers.maxParallelDownloads - 1) {
            if (runtimeHelpers.diagnosticTracing)
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

    const sourcesList = asset.loadRemote && runtimeHelpers.config.remoteSources ? runtimeHelpers.config.remoteSources : [""];
    let response: Response | undefined = undefined;
    for (let sourcePrefix of sourcesList) {
        sourcePrefix = sourcePrefix.trim();
        // HACK: Special-case because MSBuild doesn't allow "" as an attribute
        if (sourcePrefix === "./")
            sourcePrefix = "";

        const attemptUrl = resolve_path(asset, sourcePrefix);
        if (asset.name === attemptUrl) {
            if (runtimeHelpers.diagnosticTracing)
                console.debug(`MONO_WASM: Attempting to download '${attemptUrl}'`);
        } else {
            if (runtimeHelpers.diagnosticTracing)
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
    const isOkToFail = asset.isOptional || (asset.name.match(/\.pdb$/) && runtimeHelpers.config.ignorePdbLoadErrors);
    mono_assert(response, () => `Response undefined ${asset.name}`);
    if (!isOkToFail) {
        const err: any = new Error(`MONO_WASM: download '${response.url}' for ${asset.name} failed ${response.status} ${response.statusText}`);
        err.status = response.status;
        throw err;
    } else {
        Module.out(`MONO_WASM: optional download '${response.url}' for ${asset.name} failed ${response.status} ${response.statusText}`);
        return undefined;
    }
}

function resolve_path(asset: AssetEntry, sourcePrefix: string): string {
    mono_assert(sourcePrefix !== null && sourcePrefix !== undefined, () => `sourcePrefix must be provided for ${asset.name}`);
    let attemptUrl;
    const assemblyRootFolder = runtimeHelpers.config.assemblyRootFolder;
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
        attemptUrl = runtimeHelpers.locateFile(attemptUrl);
    }
    else {
        attemptUrl = asset.resolvedUrl;
    }
    mono_assert(attemptUrl && typeof attemptUrl == "string", "attemptUrl need to be path or url string");
    return attemptUrl;
}

function download_resource(request: ResourceRequest): LoadingResource {
    try {
        if (typeof Module.downloadResource === "function") {
            const loading = Module.downloadResource(request);
            if (loading) return loading;
        }
        const options: any = {};
        if (request.hash) {
            options.integrity = request.hash;
        }
        const response = runtimeHelpers.fetch_like(request.resolvedUrl!, options);
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

// this need to be run only after onRuntimeInitialized event, when the memory is ready
function _instantiate_asset(asset: AssetEntry, url: string, bytes: Uint8Array) {
    if (runtimeHelpers.diagnosticTracing)
        console.debug(`MONO_WASM: Loaded:${asset.name} as ${asset.behavior} size ${bytes.length} from ${url}`);
    const mark = startMeasure();

    const virtualName: string = typeof (asset.virtualPath) === "string"
        ? asset.virtualPath
        : asset.name;
    let offset: VoidPtr | null = null;

    switch (asset.behavior) {
        case "dotnetwasm":
        case "js-module-threads":
        case "symbols":
            // do nothing
            break;
        case "resource":
        case "assembly":
        case "pdb":
            loaded_files.push({ url: url, file: virtualName });
        // falls through
        case "heap":
        case "icu":
            offset = mono_wasm_load_bytes_into_heap(bytes);
            break;

        case "vfs": {
            // FIXME
            const lastSlash = virtualName.lastIndexOf("/");
            let parentDirectory = (lastSlash > 0)
                ? virtualName.substr(0, lastSlash)
                : null;
            let fileName = (lastSlash > 0)
                ? virtualName.substr(lastSlash + 1)
                : virtualName;
            if (fileName.startsWith("/"))
                fileName = fileName.substr(1);
            if (parentDirectory) {
                if (runtimeHelpers.diagnosticTracing)
                    console.debug(`MONO_WASM: Creating directory '${parentDirectory}'`);

                Module.FS_createPath(
                    "/", parentDirectory, true, true // fixme: should canWrite be false?
                );
            } else {
                parentDirectory = "/";
            }

            if (runtimeHelpers.diagnosticTracing)
                console.debug(`MONO_WASM: Creating file '${fileName}' in directory '${parentDirectory}'`);

            if (!mono_wasm_load_data_archive(bytes, parentDirectory)) {
                Module.FS_createDataFile(
                    parentDirectory, fileName,
                    bytes, true /* canRead */, true /* canWrite */, true /* canOwn */
                );
            }
            break;
        }
        default:
            throw new Error(`Unrecognized asset behavior:${asset.behavior}, for asset ${asset.name}`);
    }

    if (asset.behavior === "assembly") {
        // this is reading flag inside the DLL about the existence of PDB
        // it doesn't relate to whether the .pdb file is downloaded at all
        const hasPpdb = cwraps.mono_wasm_add_assembly(virtualName, offset!, bytes.length);

        if (!hasPpdb) {
            const index = loaded_files.findIndex(element => element.file == virtualName);
            loaded_files.splice(index, 1);
        }
    }
    else if (asset.behavior === "pdb") {
        cwraps.mono_wasm_add_assembly(virtualName, offset!, bytes.length);
    }
    else if (asset.behavior === "icu") {
        if (!mono_wasm_load_icu_data(offset!))
            Module.err(`MONO_WASM: Error loading ICU asset ${asset.name}`);
    }
    else if (asset.behavior === "resource") {
        cwraps.mono_wasm_add_satellite_assembly(virtualName, asset.culture || "", offset!, bytes.length);
    }
    endMeasure(mark, MeasuredBlock.instantiateAsset, asset.name);
    ++actual_instantiated_assets_count;
}

export async function instantiate_wasm_asset(
    pendingAsset: AssetEntryInternal,
    wasmModuleImports: WebAssembly.Imports,
    successCallback: InstantiateWasmSuccessCallback,
): Promise<void> {
    mono_assert(pendingAsset && pendingAsset.pendingDownloadInternal && pendingAsset.pendingDownloadInternal.response, "Can't load dotnet.wasm");
    const response = await pendingAsset.pendingDownloadInternal.response;
    const contentType = response.headers && response.headers.get ? response.headers.get("Content-Type") : undefined;
    let compiledInstance: WebAssembly.Instance;
    let compiledModule: WebAssembly.Module;
    if (typeof WebAssembly.instantiateStreaming === "function" && contentType === "application/wasm") {
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: instantiate_wasm_module streaming");
        const streamingResult = await WebAssembly.instantiateStreaming(response, wasmModuleImports!);
        compiledInstance = streamingResult.instance;
        compiledModule = streamingResult.module;
    } else {
        if (ENVIRONMENT_IS_WEB && contentType !== "application/wasm") {
            console.warn("MONO_WASM: WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.");
        }
        const arrayBuffer = await response.arrayBuffer();
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: instantiate_wasm_module buffered");
        if (ENVIRONMENT_IS_SHELL) {
            // workaround for old versions of V8 with https://bugs.chromium.org/p/v8/issues/detail?id=13823
            compiledModule = new WebAssembly.Module(arrayBuffer);
            compiledInstance = new WebAssembly.Instance(compiledModule, wasmModuleImports);
        } else {
            const arrayBufferResult = await WebAssembly.instantiate(arrayBuffer, wasmModuleImports!);
            compiledInstance = arrayBufferResult.instance;
            compiledModule = arrayBufferResult.module;
        }
    }
    successCallback(compiledInstance, compiledModule);
}

export async function instantiate_symbols_asset(
    pendingAsset: AssetEntryInternal,
): Promise<void> {
    try {
        const response = await pendingAsset.pendingDownloadInternal!.response;
        const text = await response.text();
        parseSymbolMapFile(text);
    } catch (error: any) {
        console.log(`MONO_WASM: Error loading symbol file ${pendingAsset.name}: ${JSON.stringify(error)}`);
    }
}

// used from Blazor
export function mono_wasm_load_data_archive(data: Uint8Array, prefix: string): boolean {
    if (data.length < 8)
        return false;

    const dataview = new DataView(data.buffer);
    const magic = dataview.getUint32(0, true);
    //    get magic number
    if (magic != 0x626c6174) {
        return false;
    }
    const manifestSize = dataview.getUint32(4, true);
    if (manifestSize == 0 || data.length < manifestSize + 8)
        return false;

    let manifest;
    try {
        const manifestContent = Module.UTF8ArrayToString(data, 8, manifestSize);
        manifest = JSON.parse(manifestContent);
        if (!(manifest instanceof Array))
            return false;
    } catch (exc) {
        return false;
    }

    data = data.slice(manifestSize + 8);

    // Create the folder structure
    const folders = new Set<string>();
    manifest.filter(m => {
        const file = m[0];
        const last = file.lastIndexOf("/");
        const directory = file.slice(0, last + 1);
        folders.add(directory);
    });
    folders.forEach(folder => {
        Module["FS_createPath"](prefix, folder, true, true);
    });

    for (const row of manifest) {
        const name = row[0];
        const length = row[1];
        const bytes = data.slice(0, length);
        Module["FS_createDataFile"](prefix, name, bytes, true, true);
        data = data.slice(length);
    }
    return true;
}

export async function wait_for_all_assets() {
    // wait for all assets in memory
    await allAssetsInMemory.promise;
    if (runtimeHelpers.config.assets) {
        mono_assert(actual_downloaded_assets_count == expected_downloaded_assets_count, () => `Expected ${expected_downloaded_assets_count} assets to be downloaded, but only finished ${actual_downloaded_assets_count}`);
        mono_assert(actual_instantiated_assets_count == expected_instantiated_assets_count, () => `Expected ${expected_instantiated_assets_count} assets to be in memory, but only instantiated ${actual_instantiated_assets_count}`);
        loaded_files.forEach(value => runtimeHelpers.loadedFiles.push(value.url));
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: all assets are loaded in wasm memory");
    }
}

// Used by the debugger to enumerate loaded dlls and pdbs
export function mono_wasm_get_loaded_files(): string[] {
    return runtimeHelpers.loadedFiles;
}

export function cleanupAsset(asset: AssetEntryInternal) {
    // give GC chance to collect resources
    asset.pendingDownloadInternal = null as any; // GC
    asset.pendingDownload = null as any; // GC
    asset.buffer = null as any; // GC
}