// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JsModuleExports, JsAsset, AssemblyAsset, WasmAsset, IcuAsset, EmscriptenModuleInternal, WebAssemblyBootResourceType, AssetEntryInternal, PromiseCompletionSource, LoadBootResourceCallback, InstantiateWasmSuccessCallback, SymbolsAsset } from "./types";

import { dotnetAssert, dotnetLogger, dotnetInternals, dotnetBrowserHostExports, dotnetUpdateInternals, Module, dotnetDiagnosticsExports, dotnetNativeBrowserExports } from "./cross-module";
import { ENVIRONMENT_IS_SHELL, ENVIRONMENT_IS_NODE, browserVirtualAppBase } from "./per-module";
import { createPromiseCompletionSource, delay } from "./promise-completion-source";
import { locateFile, makeURLAbsoluteWithApplicationBase } from "./bootstrap";
import { fetchLike, responseLike } from "./polyfills";
import { loaderConfig } from "./config";

let throttlingPCS: PromiseCompletionSource<void> | undefined;
let currentParallelDownloads = 0;
let downloadedAssetsCount = 0;
let totalAssetsToDownload = 0;
let loadBootResourceCallback: LoadBootResourceCallback | undefined = undefined;
const loadedLazyAssemblies = new Set<string>();
let mainWasmAsset: WasmAsset | null = null;

export function setLoadBootResourceCallback(callback: LoadBootResourceCallback | undefined): void {
    loadBootResourceCallback = callback;
}
export let wasmBinaryPromise: Promise<Response> | undefined = undefined;
export const wasmMemoryPromiseController = createPromiseCompletionSource<WebAssembly.Memory>();
export const nativeModulePromiseController = createPromiseCompletionSource<EmscriptenModuleInternal>(() => {
    dotnetUpdateInternals(dotnetInternals);
});

export async function loadDotnetModule(asset: JsAsset): Promise<JsModuleExports> {
    return loadJSModule(asset);
}

export async function loadJSModule(asset: JsAsset): Promise<any> {
    const assetInternal = asset as AssetEntryInternal;
    let mod: JsModuleExports = asset.moduleExports;
    totalAssetsToDownload++;
    if (!mod) {
        if (assetInternal.name && !asset.resolvedUrl) {
            asset.resolvedUrl = locateFile(assetInternal.name, true);
        }
        assetInternal.behavior = "js-module-dotnet";
        if (typeof loadBootResourceCallback === "function") {
            const blazorType = behaviorToBlazorAssetTypeMap[assetInternal.behavior];
            dotnetAssert.check(blazorType, `Unsupported asset behavior: ${assetInternal.behavior}`);
            const customLoadResult = loadBootResourceCallback(blazorType, assetInternal.name, asset.resolvedUrl!, assetInternal.integrity!, assetInternal.behavior);
            dotnetAssert.check(typeof customLoadResult === "string", "loadBootResourceCallback for JS modules must return string URL");
            asset.resolvedUrl = makeURLAbsoluteWithApplicationBase(customLoadResult);
        }

        if (!asset.resolvedUrl) throw new Error("Invalid config, resources is not set");
        mod = await import(/* webpackIgnore: true */ asset.resolvedUrl);
    }
    onDownloadedAsset(assetInternal);
    return mod;
}

export function fetchMainWasm(asset: WasmAsset): Promise<Response> {
    totalAssetsToDownload++;
    const assetInternal = asset as AssetEntryInternal;
    if (assetInternal.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetInternal.name);
    }
    assetInternal.behavior = "dotnetwasm";
    if (!asset.resolvedUrl) throw new Error("Invalid config, resources is not set");
    mainWasmAsset = asset;
    wasmBinaryPromise = loadResource(assetInternal);
    return wasmBinaryPromise;
}

export async function instantiateMainWasm(imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback): Promise<void> {
    const assetInternal = mainWasmAsset as AssetEntryInternal;
    mainWasmAsset = null;
    let instance: WebAssembly.Instance;
    let module: WebAssembly.Module;
    try {
        const result = await dotnetBrowserHostExports.instantiateWasm(wasmBinaryPromise!, imports);
        instance = result.instance;
        module = result.module;
    } finally {
        onDownloadedAsset(assetInternal);
    }
    successCallback(instance, module);
    const memory = dotnetNativeBrowserExports.getWasmMemory();
    wasmMemoryPromiseController.resolve(memory);
}

export async function fetchIcu(asset: IcuAsset): Promise<void> {
    const assetInternal = asset as AssetEntryInternal;
    let bytes;
    try {
        totalAssetsToDownload++;
        if (assetInternal.name && !asset.resolvedUrl) {
            asset.resolvedUrl = locateFile(assetInternal.name);
        }
        assetInternal.behavior = "icu";
        bytes = await fetchBytes(assetInternal);
    } finally {
        onDownloadedAsset(assetInternal);
    }
    if (bytes) {
        await nativeModulePromiseController.promise;
        dotnetBrowserHostExports.loadIcuData(bytes);
    }
}

export async function fetchAssembly(asset: AssemblyAsset): Promise<void> {
    const assetInternal = asset as AssetEntryInternal;
    totalAssetsToDownload++;
    dotnetAssert.check(assetInternal.virtualPath, "Assembly asset must have virtualPath");
    const assetNameForUrl = assetInternal.culture
        ? `${assetInternal.culture}/${assetInternal.name}`
        : assetInternal.name;
    if (assetNameForUrl && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetNameForUrl);
    }
    assetInternal.virtualPath = assetInternal.virtualPath.startsWith("/")
        ? assetInternal.virtualPath
        : assetInternal.culture
            ? `${browserVirtualAppBase}${assetInternal.culture}/${assetInternal.virtualPath}`
            : browserVirtualAppBase + assetInternal.virtualPath;
    assetInternal.shortName = assetInternal.virtualPath.startsWith(browserVirtualAppBase)
        ? assetInternal.virtualPath.substring(browserVirtualAppBase.length)
        : assetInternal.virtualPath.substring(assetInternal.virtualPath.lastIndexOf("/") + 1);

    if (assetInternal.virtualPath.endsWith(".wasm")) {
        await fetchWebcil(assetInternal);
    } else {
        await fetchDll(assetInternal);
    }
}

async function fetchWebcil(assetInternal: AssetEntryInternal): Promise<void> {
    try {
        assetInternal.behavior = "webcil";
        const virtualPath = assetInternal.virtualPath = assetInternal.virtualPath!.replace(/\.wasm$/, ".dll");
        assetInternal.shortName = virtualPath.startsWith(browserVirtualAppBase)
            ? virtualPath.substring(browserVirtualAppBase.length)
            : virtualPath.substring(virtualPath.lastIndexOf("/") + 1);

        const webcilPromise = loadResource(assetInternal);

        const memory = await wasmMemoryPromiseController.promise;
        const instancePromise = dotnetBrowserHostExports.instantiateWebcilModule(webcilPromise, memory, virtualPath);
        await instancePromise;
    } finally {
        onDownloadedAsset(assetInternal);
    }
}

async function fetchDll(assetInternal: AssetEntryInternal): Promise<void> {
    const virtualPath = assetInternal.virtualPath!;
    assetInternal.behavior = "assembly";
    let bytes;
    try {
        bytes = await fetchBytes(assetInternal);
    } finally {
        onDownloadedAsset(assetInternal);
    }
    await nativeModulePromiseController.promise;
    if (bytes) {
        dotnetBrowserHostExports.registerDllBytes(bytes, virtualPath, assetInternal.shortName!);
    }
}

export async function fetchPdb(asset: AssemblyAsset): Promise<void> {
    const assetInternal = asset as AssetEntryInternal;
    let bytes;
    try {
        totalAssetsToDownload++;
        dotnetAssert.check(assetInternal.virtualPath, "PDB asset must have virtualPath");
        if (assetInternal.name && !asset.resolvedUrl) {
            asset.resolvedUrl = locateFile(assetInternal.name);
        }
        assetInternal.behavior = "pdb";
        assetInternal.isOptional = assetInternal.isOptional || loaderConfig.ignorePdbLoadErrors;
        assetInternal.virtualPath = assetInternal.virtualPath.startsWith("/")
            ? assetInternal.virtualPath
            : browserVirtualAppBase + assetInternal.virtualPath;
        assetInternal.shortName = assetInternal.virtualPath.startsWith(browserVirtualAppBase)
            ? assetInternal.virtualPath.substring(browserVirtualAppBase.length)
            : assetInternal.virtualPath.substring(assetInternal.virtualPath.lastIndexOf("/") + 1);

        bytes = await fetchBytes(assetInternal);
    } finally {
        onDownloadedAsset(assetInternal);
    }
    if (bytes) {
        await nativeModulePromiseController.promise;
        dotnetBrowserHostExports.registerPdbBytes(bytes, assetInternal.virtualPath);
    }
}

export async function fetchVfs(asset: AssemblyAsset): Promise<void> {
    const assetInternal = asset as AssetEntryInternal;
    let bytes;
    try {
        totalAssetsToDownload++;
        if (assetInternal.name && !asset.resolvedUrl) {
            asset.resolvedUrl = locateFile(assetInternal.name);
        }
        assetInternal.behavior = "vfs";
        bytes = await fetchBytes(assetInternal);
    } finally {
        onDownloadedAsset(assetInternal);
    }
    if (bytes) {
        await nativeModulePromiseController.promise;
        dotnetBrowserHostExports.installVfsFile(bytes, asset);
    }
}

export async function fetchSatelliteAssemblies(culturesToLoad: string[]): Promise<void> {
    const satelliteResources = loaderConfig.resources?.satelliteResources;
    if (!satelliteResources) {
        return;
    }

    const promises: Promise<void>[] = [];
    for (const culture of culturesToLoad) {
        if (!Object.prototype.hasOwnProperty.call(satelliteResources, culture)) {
            continue;
        }
        for (const asset of satelliteResources[culture]) {
            const assetInternal = asset as AssetEntryInternal;
            assetInternal.culture = culture;
            promises.push(fetchAssembly(asset));
        }
    }
    await Promise.all(promises);
}

export async function fetchLazyAssembly(assemblyNameToLoad: string): Promise<boolean> {
    const lazyAssemblies = loaderConfig.resources?.lazyAssembly;
    if (!lazyAssemblies) {
        throw new Error("No assemblies have been marked as lazy-loadable. Use the 'BlazorWebAssemblyLazyLoad' item group in your project file to enable lazy loading an assembly.");
    }

    let assemblyNameWithoutExtension = assemblyNameToLoad;
    if (assemblyNameToLoad.endsWith(".dll"))
        assemblyNameWithoutExtension = assemblyNameToLoad.substring(0, assemblyNameToLoad.length - 4);
    else if (assemblyNameToLoad.endsWith(".wasm"))
        assemblyNameWithoutExtension = assemblyNameToLoad.substring(0, assemblyNameToLoad.length - 5);

    const assemblyNameToLoadDll = assemblyNameWithoutExtension + ".dll";
    const assemblyNameToLoadWasm = assemblyNameWithoutExtension + ".wasm";

    let dllAsset: AssemblyAsset | null = null;
    for (const asset of lazyAssemblies) {
        if (asset.virtualPath === assemblyNameToLoadDll || asset.virtualPath === assemblyNameToLoadWasm) {
            dllAsset = asset;
            break;
        }
    }

    if (!dllAsset) {
        throw new Error(`${assemblyNameToLoad} must be marked with 'BlazorWebAssemblyLazyLoad' item group in your project file to allow lazy-loading.`);
    }

    if (loadedLazyAssemblies.has(dllAsset.virtualPath)) {
        return false;
    }

    await fetchAssembly(dllAsset);
    loadedLazyAssemblies.add(dllAsset.virtualPath);

    if (loaderConfig.debugLevel !== 0) {
        const pdbNameToLoad = assemblyNameWithoutExtension + ".pdb";
        const pdbAssets = loaderConfig.resources?.pdb;
        let pdbAssetToLoad: AssemblyAsset | undefined;
        if (pdbAssets) {
            for (const pdbAsset of pdbAssets) {
                if (pdbAsset.virtualPath === pdbNameToLoad) {
                    pdbAssetToLoad = pdbAsset;
                    break;
                }
            }
        }
        if (!pdbAssetToLoad) {
            for (const lazyAsset of lazyAssemblies) {
                if (lazyAsset.virtualPath === pdbNameToLoad) {
                    pdbAssetToLoad = lazyAsset as AssemblyAsset;
                    break;
                }
            }
        }
        if (pdbAssetToLoad) {
            await fetchPdb(pdbAssetToLoad);
        }
    }

    return true;
}

export async function fetchNativeSymbols(asset: SymbolsAsset): Promise<void> {
    const assetInternal = asset as AssetEntryInternal;
    let tableText;
    try {
        totalAssetsToDownload++;
        if (assetInternal.name && !asset.resolvedUrl) {
            asset.resolvedUrl = locateFile(assetInternal.name);
        }
        assetInternal.behavior = "symbols";
        assetInternal.isOptional = assetInternal.isOptional || loaderConfig.ignorePdbLoadErrors;
        tableText = await fetchText(assetInternal);
    } finally {
        onDownloadedAsset(assetInternal);
    }
    dotnetDiagnosticsExports.installNativeSymbols(tableText || "");
}

async function fetchBytes(asset: AssetEntryInternal): Promise<Uint8Array | null> {
    dotnetAssert.check(asset && asset.resolvedUrl, "Bad asset.resolvedUrl");
    const response = await loadResource(asset);
    if (!response.ok) {
        if (asset.isOptional) {
            dotnetLogger.warn(`Optional resource '${asset.name}' failed to load from '${asset.resolvedUrl}'. HTTP status: ${response.status} ${response.statusText}`);
            return null;
        }
        throw new Error(`Failed to load resource '${asset.name}' from '${asset.resolvedUrl}'. HTTP status: ${response.status} ${response.statusText}`);
    }
    const buffer = await (asset.buffer || response.arrayBuffer());
    return new Uint8Array(buffer);
}

async function fetchText(asset: AssetEntryInternal): Promise<string | null> {
    dotnetAssert.check(asset && asset.resolvedUrl, "Bad asset.resolvedUrl");
    const response = await loadResource(asset);
    if (!response.ok) {
        if (asset.isOptional) {
            dotnetLogger.warn(`Optional resource '${asset.name}' failed to load from '${asset.resolvedUrl}'. HTTP status: ${response.status} ${response.statusText}`);
            return null;
        }
        throw new Error(`Failed to load resource '${asset.name}' from '${asset.resolvedUrl}'. HTTP status: ${response.status} ${response.statusText}`);
    }
    return response.text();
}

function loadResource(asset: AssetEntryInternal): Promise<Response> {
    if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE || asset.resolvedUrl && asset.resolvedUrl.indexOf("file://") !== -1) {
        // no need to retry or throttle local file access
        return loadResourceFetch(asset);
    }
    if (!loaderConfig.enableDownloadRetry || noRetry[asset.behavior]) {
        // only throttle, no retry
        return loadResourceThrottle(asset);
    }
    // retry and throttle
    return loadResourceRetry(asset);
}

const noRetryStatusCodes = new Set<number>([400, 401, 403, 404, 405, 406, 409, 410, 411, 413, 414, 415, 422, 426, 501, 505,]);
async function loadResourceRetry(asset: AssetEntryInternal): Promise<Response> {
    let response: Response;
    response = await loadResourceAttempt();
    if (response.ok || asset.isOptional || noRetryStatusCodes.has(response.status)) {
        return response;
    }
    if (response.status === 429) {
        // Too Many Requests
        await delay(100);
    }
    response = await loadResourceAttempt();
    if (response.ok || noRetryStatusCodes.has(response.status)) {
        return response;
    }
    await delay(100); // wait 100ms before the last retry
    response = await loadResourceAttempt();
    if (response.ok) {
        return response;
    }
    throw new Error(`Failed to load resource '${asset.name}' from '${asset.resolvedUrl}' after multiple attempts. Last HTTP status: ${response.status} ${response.statusText}`);

    async function loadResourceAttempt(): Promise<Response> {
        let response: Response;
        try {
            response = await loadResourceThrottle(asset);
            if (!response) {
                response = responseLike(asset.resolvedUrl!, null, {
                    status: 404,
                    statusText: "No response",
                });
            }
        } catch (err: any) {
            response = responseLike(asset.resolvedUrl!, null, {
                status: 500,
                statusText: err.message || "Exception during fetch",
            });
        }
        return response;
    }
}

// in order to prevent net::ERR_INSUFFICIENT_RESOURCES if we start downloading too many files at same time on a device with low resources
// also when downloading in background
async function loadResourceThrottle(asset: AssetEntryInternal): Promise<Response> {
    while (throttlingPCS && !asset.priority) {
        await throttlingPCS.promise;
    }
    try {

        enterThrottling(asset);
        const responsePromise = loadResourceFetch(asset);
        const response = await responsePromise;
        dotnetAssert.check(response, "Bad response in loadResourceThrottle");

        if (!noBuffer[asset.behavior]) {
            asset.buffer = await response.arrayBuffer();
        }
        if (!leaveAfterInstantiation[asset.behavior]) {
            leaveThrottling(asset);
        }
        return response;
    } catch (err) {
        leaveThrottling(asset);
        throw err;
    }
}

function enterThrottling(asset: AssetEntryInternal) {
    asset.inprogress = true;
    ++currentParallelDownloads;
    if (currentParallelDownloads === loaderConfig.maxParallelDownloads) {
        dotnetLogger.debug("Throttling further parallel downloads");
        throttlingPCS = createPromiseCompletionSource<void>();
    }
}

function leaveThrottling(asset: AssetEntryInternal) {
    dotnetAssert.check(asset.inprogress, "Asset is not in progress in leaveThrottling");
    asset.inprogress = false;
    --currentParallelDownloads;
    if (throttlingPCS && currentParallelDownloads == loaderConfig.maxParallelDownloads! - 1) {
        dotnetLogger.debug("Resuming more parallel downloads");
        const oldThrottlingPCS = throttlingPCS;
        throttlingPCS = undefined;
        oldThrottlingPCS.resolve();
    }
}

async function loadResourceFetch(asset: AssetEntryInternal): Promise<Response> {
    const expectedContentType = behaviorToContentTypeMap[asset.behavior];
    dotnetAssert.check(expectedContentType, `Unsupported asset behavior: ${asset.behavior}`);
    if (asset.buffer) {
        const arrayBuffer = await asset.buffer;
        return responseLike(asset.resolvedUrl!, arrayBuffer, {
            status: 200,
            statusText: "OK",
            headers: {
                "Content-Length": arrayBuffer.byteLength.toString(),
                "Content-Type": expectedContentType,
            }
        });
    }
    if (asset.pendingDownload) {
        return asset.pendingDownload.response;
    }
    if (typeof loadBootResourceCallback === "function") {
        const blazorType = behaviorToBlazorAssetTypeMap[asset.behavior];
        dotnetAssert.check(blazorType, `Unsupported asset behavior: ${asset.behavior}`);
        const customLoadResult = loadBootResourceCallback(blazorType, asset.name, asset.resolvedUrl!, asset.integrity!, asset.behavior);
        if (typeof customLoadResult === "string") {
            asset.resolvedUrl = makeURLAbsoluteWithApplicationBase(customLoadResult);
        } else if (typeof customLoadResult === "object") {
            return customLoadResult as any;
        }
    }
    dotnetAssert.check(asset.resolvedUrl, "Bad asset.resolvedUrl");
    const fetchOptions: RequestInit = {};

    if (asset.cache) {
        // If the asset definition specifies a cache mode, use it.
        fetchOptions.cache = asset.cache;
    } else if (!loaderConfig.disableNoCacheFetch) {
        // Otherwise, for backwards compatibility use "no-cache" setting unless disabled by the user.
        // https://github.com/dotnet/runtime/issues/74815
        fetchOptions.cache = "no-cache";
    }

    if (asset.useCredentials) {
        // Include credentials so the server can allow download / provide user specific file
        fetchOptions.credentials = "include";
    } else {
        // `disableIntegrityCheck` is to give developers an easy opt-out from the integrity check
        if (!loaderConfig.disableIntegrityCheck && asset.hash) {
            // Any other resource than configuration should provide integrity check
            fetchOptions.integrity = asset.hash;
        }
    }

    return fetchLike(asset.resolvedUrl!, fetchOptions, expectedContentType);
}

function onDownloadedAsset(asset: AssetEntryInternal): void {
    if (asset.inprogress) {
        leaveThrottling(asset);
    }
    ++downloadedAssetsCount;
    if (Module.onDownloadResourceProgress) {
        Module.onDownloadResourceProgress(downloadedAssetsCount, totalAssetsToDownload);
    }
    // release memory
    asset.buffer = null!;
    asset.pendingDownload = undefined;
}

export function verifyAllAssetsDownloaded(): void {
    dotnetAssert.check(downloadedAssetsCount === totalAssetsToDownload, `Not all assets were downloaded. Downloaded ${downloadedAssetsCount} out of ${totalAssetsToDownload}`);
}

const behaviorToBlazorAssetTypeMap: { [key: string]: WebAssemblyBootResourceType | undefined } = {
    "resource": "assembly",
    "assembly": "assembly",
    "pdb": "pdb",
    "icu": "globalization",
    "vfs": "configuration",
    "manifest": "manifest",
    "symbols": "pdb",
    "dotnetwasm": "dotnetwasm",
    "webcil": "assembly",
    "js-module-dotnet": "dotnetjs",
    "js-module-native": "dotnetjs",
    "js-module-runtime": "dotnetjs",
    "js-module-threads": "dotnetjs"
};

const behaviorToContentTypeMap: { [key: string]: string | undefined } = {
    "resource": "application/octet-stream",
    "assembly": "application/octet-stream",
    "pdb": "application/octet-stream",
    "icu": "application/octet-stream",
    "vfs": "application/octet-stream",
    "manifest": "application/json",
    "symbols": "text/plain; charset=utf-8",
    "dotnetwasm": "application/wasm",
    "webcil": "application/wasm",
};

const noRetry: { [key: string]: number | undefined } = {
    "dotnetwasm": 1,
    "symbols": 1,
};

const noBuffer: { [key: string]: number | undefined } = {
    "dotnetwasm": 1,
    "symbols": 1,
    "webcil": 1,
};

const leaveAfterInstantiation: { [key: string]: number | undefined } = {
    "dotnetwasm": 1,
    "webcil": 1,
};
