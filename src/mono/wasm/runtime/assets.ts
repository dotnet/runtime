import cwraps from "./cwraps";
import { mono_wasm_load_icu_data } from "./icu";
import { ENVIRONMENT_IS_WEB, Module, runtimeHelpers } from "./imports";
import { mono_wasm_load_bytes_into_heap } from "./memory";
import { MONO } from "./net6-legacy/imports";
import { createPromiseController, PromiseAndController } from "./promise-controller";
import { delay } from "./promise-utils";
import { beforeOnRuntimeInitialized } from "./startup";
import { AssetBehaviours, AssetEntry, LoadingResource, mono_assert, ResourceRequest } from "./types";
import { InstantiateWasmSuccessCallback, VoidPtr } from "./types/emscripten";

const allAssetsInMemory = createPromiseController<void>();
const allDownloadsQueued = createPromiseController<void>();
let downloded_assets_count = 0;
let instantiated_assets_count = 0;
const loaded_files: { url: string, file: string }[] = [];
const loaded_assets: { [id: string]: [VoidPtr, number] } = Object.create(null);
// in order to prevent net::ERR_INSUFFICIENT_RESOURCES if we start downloading too many files at same time
let parallel_count = 0;
let throttling: PromiseAndController<void> | undefined;
const skipDownloadsAssetTypes: {
    [k: string]: boolean
} = {
    "js-module-crypto": true,
    "js-module-threads": true,
};

export function resolve_asset_path(behavior: AssetBehaviours) {
    const asset: AssetEntry | undefined = runtimeHelpers.config.assets?.find(a => a.behavior == behavior);
    mono_assert(asset, () => `Can't find asset for ${behavior}`);
    if (!asset.resolvedUrl) {
        asset.resolvedUrl = resolve_path(asset, "");
    }
    return asset;
}

export async function mono_download_assets(): Promise<void> {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_download_assets");
    runtimeHelpers.maxParallelDownloads = runtimeHelpers.config.maxParallelDownloads || runtimeHelpers.maxParallelDownloads;
    try {
        const download_promises: Promise<AssetEntry | undefined>[] = [];
        // start fetching and instantiating all assets in parallel
        for (const asset of runtimeHelpers.config.assets!) {
            if (!asset.pending && !skipDownloadsAssetTypes[asset.behavior]) {
                download_promises.push(start_asset_download(asset));
            }
        }
        allDownloadsQueued.promise_control.resolve();

        const asset_promises: Promise<void>[] = [];
        for (const downloadPromise of download_promises) {
            const downloadedAsset = await downloadPromise;
            if (downloadedAsset) {
                asset_promises.push((async () => {
                    const url = downloadedAsset.pending!.url;
                    const response = await downloadedAsset.pending!.response;
                    downloadedAsset.pending = undefined; //GC
                    const buffer = await response.arrayBuffer();
                    await beforeOnRuntimeInitialized.promise;
                    // this is after onRuntimeInitialized
                    _instantiate_asset(downloadedAsset, url, new Uint8Array(buffer));
                })());
            }
        }

        // this await will get past the onRuntimeInitialized because we are not blocking via addRunDependency
        // and we are not awating it here
        Promise.all(asset_promises).then(() => allAssetsInMemory.promise_control.resolve());
        // OPTIMIZATION explained:
        // we do it this way so that we could allocate memory immediately after asset is downloaded (and after onRuntimeInitialized which happened already)
        // spreading in time
        // rather than to block all downloads after onRuntimeInitialized or block onRuntimeInitialized after all downloads are done. That would create allocation burst.
    } catch (err: any) {
        Module.printErr("MONO_WASM: Error in mono_download_assets: " + err);
        throw err;
    }
}

export async function start_asset_download(asset: AssetEntry): Promise<AssetEntry | undefined> {
    try {
        return await start_asset_download_throttle(asset);
    } catch (err: any) {
        if (err && err.status == 404) {
            throw err;
        }
        // second attempt only after all first attempts are queued
        await allDownloadsQueued.promise;
        try {
            return await start_asset_download_throttle(asset);
        } catch (err) {
            // third attempt after small delay
            await delay(100);
            return await start_asset_download_throttle(asset);
        }
    }
}

function resolve_path(asset: AssetEntry, sourcePrefix: string): string {
    let attemptUrl;
    const assemblyRootFolder = runtimeHelpers.config.assemblyRootFolder;
    if (!asset.resolvedUrl) {
        if (sourcePrefix === "") {
            if (asset.behavior === "assembly" || asset.behavior === "pdb")
                attemptUrl = assemblyRootFolder + "/" + asset.name;
            else if (asset.behavior === "resource") {
                const path = asset.culture !== "" ? `${asset.culture}/${asset.name}` : asset.name;
                attemptUrl = assemblyRootFolder + "/" + path;
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
    return attemptUrl;
}

function download_resource(request: ResourceRequest): LoadingResource {
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
}

async function start_asset_download_sources(asset: AssetEntry): Promise<AssetEntry | undefined> {
    // we don't addRunDependency to allow download in parallel with onRuntimeInitialized event!
    if (asset.buffer) {
        ++downloded_assets_count;
        const buffer = asset.buffer;
        asset.buffer = undefined;//GC later
        asset.pending = {
            url: "undefined://" + asset.name,
            name: asset.name,
            response: Promise.resolve({
                arrayBuffer: () => buffer,
                headers: {
                    get: () => undefined,
                }
            }) as any
        };
        return Promise.resolve(asset);
    }
    if (asset.pending) {
        ++downloded_assets_count;
        return asset;
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
            const loadingResource = download_resource({
                name: asset.name,
                resolvedUrl: attemptUrl,
                hash: asset.hash,
                behavior: asset.behavior
            });
            asset.pending = loadingResource;
            response = await loadingResource.response;
            if (!response.ok) {
                continue;// next source
            }
            ++downloded_assets_count;
            return asset;
        }
        catch (err) {
            continue; //next source
        }
    }
    throw response;
}

async function start_asset_download_throttle(asset: AssetEntry): Promise<AssetEntry | undefined> {
    // we don't addRunDependency to allow download in parallel with onRuntimeInitialized event!
    while (throttling) {
        await throttling.promise;
    }
    try {
        ++parallel_count;
        if (parallel_count == runtimeHelpers.maxParallelDownloads) {
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: Throttling further parallel downloads");
            throttling = createPromiseController<void>();
        }
        return await start_asset_download_sources(asset);
    }
    catch (response: any) {
        const isOkToFail = asset.isOptional || (asset.name.match(/\.pdb$/) && runtimeHelpers.config.ignorePdbLoadErrors);
        if (!isOkToFail) {
            const err: any = new Error(`MONO_WASM: download '${response.url}' for ${asset.name} failed ${response.status} ${response.statusText}`);
            err.status = response.status;
            throw err;
        }
    }
    finally {
        --parallel_count;
        if (throttling && parallel_count == runtimeHelpers.maxParallelDownloads - 1) {
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: Resuming more parallel downloads");
            const old_throttling = throttling;
            throttling = undefined;
            old_throttling.promise_control.resolve();
        }
    }
}

// this need to be run only after onRuntimeInitialized event, when the memory is ready
function _instantiate_asset(asset: AssetEntry, url: string, bytes: Uint8Array) {
    if (runtimeHelpers.diagnosticTracing)
        console.debug(`MONO_WASM: Loaded:${asset.name} as ${asset.behavior} size ${bytes.length} from ${url}`);

    const virtualName: string = typeof (asset.virtualPath) === "string"
        ? asset.virtualPath
        : asset.name;
    let offset: VoidPtr | null = null;

    switch (asset.behavior) {
        case "dotnetwasm":
        case "js-module-crypto":
        case "js-module-threads":
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
            loaded_assets[virtualName] = [offset, bytes.length];
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
    else if (asset.behavior === "icu") {
        if (!mono_wasm_load_icu_data(offset!))
            Module.printErr(`MONO_WASM: Error loading ICU asset ${asset.name}`);
    }
    else if (asset.behavior === "resource") {
        cwraps.mono_wasm_add_satellite_assembly(virtualName, asset.culture!, offset!, bytes.length);
    }
    ++instantiated_assets_count;
}

export async function instantiate_wasm_asset(
    pendingAsset: AssetEntry,
    wasmModuleImports: WebAssembly.Imports,
    successCallback: InstantiateWasmSuccessCallback,
) {
    mono_assert(pendingAsset && pendingAsset.pending, "Can't load dotnet.wasm");
    const response = await pendingAsset.pending.response;
    const contentType = response.headers ? response.headers.get("Content-Type") : undefined;
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
        const arrayBufferResult = await WebAssembly.instantiate(arrayBuffer, wasmModuleImports!);
        compiledInstance = arrayBufferResult.instance;
        compiledModule = arrayBufferResult.module;
    }
    ++instantiated_assets_count;
    successCallback(compiledInstance, compiledModule);
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
    // /usr/share/zoneinfo
    // /usr/share/zoneinfo/Africa
    // /usr/share/zoneinfo/Asia
    // ..

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
        const expected_asset_count = runtimeHelpers.config.assets.filter(a => !skipDownloadsAssetTypes[a.behavior]).length;
        mono_assert(downloded_assets_count == expected_asset_count, "Expected assets to be downloaded");
        mono_assert(instantiated_assets_count == expected_asset_count, "Expected assets to be in memory");
        loaded_files.forEach(value => MONO.loaded_files.push(value.url));
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: all assets are loaded in wasm memory");
    }
}

// Used by the debugger to enumerate loaded dlls and pdbs
export function mono_wasm_get_loaded_files(): string[] {
    return MONO.loaded_files;
}
