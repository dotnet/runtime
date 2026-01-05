// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JsModuleExports, JsAsset, AssemblyAsset, WasmAsset, IcuAsset, EmscriptenModuleInternal, InstantiateWasmSuccessCallback, WebAssemblyBootResourceType, AssetEntryInternal } from "./types";

import { dotnetAssert, dotnetGetInternals, dotnetBrowserHostExports, dotnetUpdateInternals } from "./cross-module";
import { ENVIRONMENT_IS_WEB } from "./per-module";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { locateFile, makeURLAbsoluteWithApplicationBase } from "./bootstrap";
import { fetchLike } from "./polyfills";
import { loadBootResourceCallback } from "./host-builder";
import { loaderConfig } from "./config";

export let wasmBinaryPromise: Promise<Response> | undefined = undefined;
export const nativeModulePromiseController = createPromiseCompletionSource<EmscriptenModuleInternal>(() => {
    dotnetUpdateInternals(dotnetGetInternals());
});

export async function loadJSModule(asset: JsAsset): Promise<JsModuleExports> {
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
    return await import(/* webpackIgnore: true */ asset.resolvedUrl);
}

export function fetchWasm(asset: WasmAsset): Promise<Response> {
    const assetInternal = asset as AssetEntryInternal;
    if (assetInternal.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetInternal.name);
    }
    assetInternal.behavior = "dotnetwasm";
    if (!asset.resolvedUrl) throw new Error("Invalid config, resources is not set");
    wasmBinaryPromise = loadResource(assetInternal);
    return wasmBinaryPromise;
}

export async function instantiateWasm(imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback): Promise<void> {
    if (wasmBinaryPromise instanceof globalThis.Response === false || !WebAssembly.instantiateStreaming) {
        const res = await checkResponseOk();
        const data = await res.arrayBuffer();
        const module = await WebAssembly.compile(data);
        const instance = await WebAssembly.instantiate(module, imports);
        successCallback(instance, module);
    } else {
        const instantiated = await WebAssembly.instantiateStreaming(wasmBinaryPromise, imports);
        await checkResponseOk();
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
    const assetInternal = asset as AssetEntryInternal;
    if (assetInternal.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetInternal.name);
    }
    assetInternal.behavior = "icu";
    const bytes = await fetchBytes(assetInternal);
    await nativeModulePromiseController.promise;
    dotnetBrowserHostExports.loadIcuData(bytes);
}

export async function fetchDll(asset: AssemblyAsset): Promise<void> {
    const assetInternal = asset as AssetEntryInternal;
    if (assetInternal.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetInternal.name);
    }
    assetInternal.behavior = "assembly";
    const bytes = await fetchBytes(assetInternal);
    await nativeModulePromiseController.promise;

    dotnetBrowserHostExports.registerDllBytes(bytes, asset);
}

export async function fetchVfs(asset: AssemblyAsset): Promise<void> {
    const assetInternal = asset as AssetEntryInternal;
    if (assetInternal.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(assetInternal.name);
    }
    assetInternal.behavior = "vfs";
    const bytes = await fetchBytes(assetInternal);
    await nativeModulePromiseController.promise;

    dotnetBrowserHostExports.installVfsFile(bytes, asset);
}

async function fetchBytes(asset: AssetEntryInternal): Promise<Uint8Array> {
    dotnetAssert.check(asset && asset.resolvedUrl, "Bad asset.resolvedUrl");
    const response = await loadResource(asset);
    if (!response.ok) {
        throw new Error(`Failed to load resource '${asset.name}' from '${asset.resolvedUrl}'. HTTP status: ${response.status} ${response.statusText}`);
    }
    const buffer = await response.arrayBuffer();
    return new Uint8Array(buffer);
}

async function loadResource(asset: AssetEntryInternal): Promise<Response> {
    if (typeof loadBootResourceCallback === "function") {
        const type = runtimeToBlazorAssetTypeMap[asset.behavior];
        dotnetAssert.check(type, `Unsupported asset behavior: ${asset.behavior}`);
        const customLoadResult = loadBootResourceCallback(type, asset.name, asset.resolvedUrl!, asset.integrity!, asset.behavior);
        if (typeof customLoadResult === "string") {
            asset.resolvedUrl = makeURLAbsoluteWithApplicationBase(customLoadResult);
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
