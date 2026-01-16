// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JsModuleExports, JsAsset, AssemblyAsset, WasmAsset, IcuAsset, EmscriptenModuleInternal, InstantiateWasmSuccessCallback, WebAssemblyBootResourceType, AssetEntryInternal, PromiseCompletionSource, LoadBootResourceCallback } from "./types";

import { dotnetAssert, dotnetLogger, dotnetInternals, dotnetBrowserHostExports, dotnetUpdateInternals, Module } from "./cross-module";
import { ENVIRONMENT_IS_WEB, ENVIRONMENT_IS_SHELL, ENVIRONMENT_IS_NODE } from "./per-module";
import { createPromiseCompletionSource, delay } from "./promise-completion-source";
import { locateFile, makeURLAbsoluteWithApplicationBase } from "./bootstrap";
import { fetchLike } from "./polyfills";
import { loaderConfig } from "./config";

let throttlingPCS: PromiseCompletionSource<void> | undefined;
let currentParallelDownloads = 0;
let downloadedAssetsCount = 0;
let totalAssetsToDownload = 0;
let loadBootResourceCallback: LoadBootResourceCallback | undefined = undefined;

export function setLoadBootResourceCallback(callback: LoadBootResourceCallback | undefined): void {
    loadBootResourceCallback = callback;
}
let instantiateStreaming = typeof WebAssembly !== "undefined" && typeof WebAssembly.instantiateStreaming === "function";
export let wasmBinaryPromise: Promise<Response> | undefined = undefined;
export const nativeModulePromiseController = createPromiseCompletionSource<EmscriptenModuleInternal>(() => {
    dotnetUpdateInternals(dotnetInternals);
});

export async function loadDotnetModule(asset: JsAsset): Promise<JsModuleExports> {
    return loadJSModule(asset);
}

export async function loadJSModule(asset: JsAsset): Promise<any> {
    let mod: JsModuleExports = asset.moduleExports;
    totalAssetsToDownload++;
    if (!mod) {
        const assetInternal = asset as AssetEntryInternal;
        if (assetInternal.name && !asset.resolvedUrl) {
            asset.resolvedUrl = locateFile(assetInternal.name, true);
        }
        assetInternal.behavior = "js-module-dotnet";
        if (typeof loadBootResourceCallback === "function") {
            const type = runtimeToBlazorAssetTypeMap[assetInternal.behavior];
            dotnetAssert.check(type, `Unsupported asset behavior: ${assetInternal.behavior}`);
            const customLoadResult = loadBootResourceCallback(type, assetInternal.name, asset.resolvedUrl!, assetInternal.integrity!, assetInternal.behavior);
            dotnetAssert.check(typeof customLoadResult === "string", "loadBootResourceCallback for JS modules must return string URL");
            asset.resolvedUrl = makeURLAbsoluteWithApplicationBase(customLoadResult);
        }

        if (!asset.resolvedUrl) throw new Error("Invalid config, resources is not set");
        mod = await import(/* webpackIgnore: true */ asset.resolvedUrl);
    }
    onDownloadedAsset();
    return mod;
}

export function fetchWasm(asset: WasmAsset): Promise<Response> {
    totalAssetsToDownload++;
    const assetInternal = asset as AssetEntryInternal;
    if (assetInternal.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetInternal.name);
    }
    assetInternal.behavior = "dotnetwasm";
    if (!asset.resolvedUrl) throw new Error("Invalid config, resources is not set");
    wasmBinaryPromise = loadResource(assetInternal);
    if (assetInternal.buffer) {
        instantiateStreaming = false;
    }
    return wasmBinaryPromise;
}

export async function instantiateWasm(imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback): Promise<void> {
    if (!instantiateStreaming) {
        const res = await checkResponseOk();
        const data = await res.arrayBuffer();
        const module = await WebAssembly.compile(data);
        const instance = await WebAssembly.instantiate(module, imports);
        onDownloadedAsset();
        successCallback(instance, module);
    } else {
        const instantiated = await WebAssembly.instantiateStreaming(wasmBinaryPromise!, imports);
        await checkResponseOk();
        onDownloadedAsset();
        successCallback(instantiated.instance, instantiated.module);
    }

    async function checkResponseOk(): Promise<Response> {
        dotnetAssert.check(wasmBinaryPromise, "WASM binary promise was not initialized");
        const res = await wasmBinaryPromise;
        if (res.ok === false) {
            throw new Error(`Failed to load WebAssembly module. HTTP status: ${res.status} ${res.statusText}`);
        }
        const contentType = res.headers && res.headers.get ? res.headers.get("Content-Type") : undefined;
        if (ENVIRONMENT_IS_WEB && contentType !== "application/wasm") {
            dotnetLogger.warn("WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.");
        }
        return res;
    }
}

export async function fetchIcu(asset: IcuAsset): Promise<void> {
    totalAssetsToDownload++;
    const assetInternal = asset as AssetEntryInternal;
    if (assetInternal.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetInternal.name);
    }
    assetInternal.behavior = "icu";
    const bytes = await fetchBytes(assetInternal);
    await nativeModulePromiseController.promise;
    onDownloadedAsset();
    if (bytes) {
        dotnetBrowserHostExports.loadIcuData(bytes);
    }
}

export async function fetchDll(asset: AssemblyAsset): Promise<void> {
    totalAssetsToDownload++;
    const assetInternal = asset as AssetEntryInternal;
    if (assetInternal.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetInternal.name);
    }
    assetInternal.behavior = "assembly";
    const bytes = await fetchBytes(assetInternal);
    await nativeModulePromiseController.promise;

    onDownloadedAsset();
    if (bytes) {
        dotnetBrowserHostExports.registerDllBytes(bytes, asset);
    }
}

export async function fetchPdb(asset: AssemblyAsset): Promise<void> {
    totalAssetsToDownload++;
    const assetInternal = asset as AssetEntryInternal;
    if (assetInternal.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetInternal.name);
    }
    assetInternal.behavior = "pdb";
    assetInternal.isOptional = assetInternal.isOptional || loaderConfig.ignorePdbLoadErrors;
    const bytes = await fetchBytes(assetInternal);
    await nativeModulePromiseController.promise;

    onDownloadedAsset();
    if (bytes) {
        dotnetBrowserHostExports.registerPdbBytes(bytes, asset);
    }
}

export async function fetchVfs(asset: AssemblyAsset): Promise<void> {
    totalAssetsToDownload++;
    const assetInternal = asset as AssetEntryInternal;
    if (assetInternal.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetInternal.name);
    }
    assetInternal.behavior = "vfs";
    const bytes = await fetchBytes(assetInternal);
    await nativeModulePromiseController.promise;
    onDownloadedAsset();
    if (bytes) {
        dotnetBrowserHostExports.installVfsFile(bytes, asset);
    }
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

function loadResource(asset: AssetEntryInternal): Promise<Response> {
    if ("dotnetwasm" === asset.behavior) {
        // `response.arrayBuffer()` can't be called twice.
        return loadResourceFetch(asset);
    }
    if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE || asset.resolvedUrl && asset.resolvedUrl.indexOf("file://") !== -1) {
        // no need to retry or throttle local file access
        return loadResourceFetch(asset);
    }
    if (!loaderConfig.enableDownloadRetry) {
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
                response = {
                    ok: false,
                    status: -1,
                    statusText: "No response",
                } as any;
            }
        } catch (err: any) {
            response = {
                ok: false,
                status: -1,
                statusText: err.message || "Exception during fetch",
            } as any;
        }
        return response;
    }
}

// in order to prevent net::ERR_INSUFFICIENT_RESOURCES if we start downloading too many files at same time on a device with low resources
async function loadResourceThrottle(asset: AssetEntryInternal): Promise<Response> {
    while (throttlingPCS) {
        await throttlingPCS.promise;
    }
    try {
        ++currentParallelDownloads;
        if (currentParallelDownloads === loaderConfig.maxParallelDownloads) {
            dotnetLogger.debug("Throttling further parallel downloads");
            throttlingPCS = createPromiseCompletionSource<void>();
        }
        const responsePromise = loadResourceFetch(asset);
        const response = await responsePromise;
        dotnetAssert.check(response, "Bad response in loadResourceThrottle");

        asset.buffer = await response.arrayBuffer();
        return response;
    } finally {
        --currentParallelDownloads;
        if (throttlingPCS && currentParallelDownloads == loaderConfig.maxParallelDownloads! - 1) {
            dotnetLogger.debug("Resuming more parallel downloads");
            const oldThrottlingPCS = throttlingPCS;
            throttlingPCS = undefined;
            oldThrottlingPCS.resolve();
        }
    }
}

async function loadResourceFetch(asset: AssetEntryInternal): Promise<Response> {
    if (asset.buffer) {
        return <Response><any>{
            ok: true,
            headers: {
                length: 0,
                get: () => null
            },
            url: asset.resolvedUrl,
            arrayBuffer: () => Promise.resolve(asset.buffer!),
            json: () => {
                throw new Error("NotImplementedException");
            },
            text: () => {
                throw new Error("NotImplementedException");
            }
        };
    }
    if (asset.pendingDownload) {
        return asset.pendingDownload.response;
    }
    if (typeof loadBootResourceCallback === "function") {
        const type = runtimeToBlazorAssetTypeMap[asset.behavior];
        dotnetAssert.check(type, `Unsupported asset behavior: ${asset.behavior}`);
        const customLoadResult = loadBootResourceCallback(type, asset.name, asset.resolvedUrl!, asset.integrity!, asset.behavior);
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

    return fetchLike(asset.resolvedUrl!, fetchOptions);
}

function onDownloadedAsset() {
    ++downloadedAssetsCount;
    if (Module.onDownloadResourceProgress) {
        Module.onDownloadResourceProgress(downloadedAssetsCount, totalAssetsToDownload);
    }
}

export function verifyAllAssetsDownloaded(): void {
    dotnetAssert.check(downloadedAssetsCount === totalAssetsToDownload, `Not all assets were downloaded. Downloaded ${downloadedAssetsCount} out of ${totalAssetsToDownload}`);
}

const runtimeToBlazorAssetTypeMap: { [key: string]: WebAssemblyBootResourceType | undefined } = {
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
