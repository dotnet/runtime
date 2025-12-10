// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoadBootResourceCallback, JsModuleExports, JsAsset, AssemblyAsset, PdbAsset, WasmAsset, IcuAsset, EmscriptenModuleInternal, InstantiateWasmSuccessCallback } from "./types";

import { dotnetAssert, dotnetGetInternals, dotnetBrowserHostExports, dotnetUpdateInternals } from "./cross-module";
import { getIcuResourceName } from "./icu";
import { getLoaderConfig } from "./config";
import { BrowserHost_InitializeCoreCLR } from "./run";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { locateFile } from "./bootstrap";
import { fetchLike } from "./polyfills";

const nativeModulePromiseController = createPromiseCompletionSource<EmscriptenModuleInternal>(() => {
    dotnetUpdateInternals(dotnetGetInternals());
});
let wasmBinaryPromise: any = undefined;

// WASM-TODO: retry logic
// WASM-TODO: throttling logic
// WASM-TODO: invokeLibraryInitializers
// WASM-TODO: webCIL
// WASM-TODO: downloadOnly - blazor render mode auto pre-download. Really no start.
// WASM-TODO: no-cache, force-cache, integrity
// WASM-TODO: LoadBootResourceCallback
// WASM-TODO: fail fast for missing WASM features - SIMD, EH, BigInt detection

export async function createRuntime(downloadOnly: boolean, loadBootResource?: LoadBootResourceCallback): Promise<any> {
    if (loadBootResource) throw new Error("TODO: loadBootResource is not implemented yet");
    const config = getLoaderConfig();
    if (!config.resources || !config.resources.coreAssembly || !config.resources.coreAssembly.length) throw new Error("Invalid config, resources is not set");

    const nativeModulePromise = loadJSModule(config.resources.jsModuleNative[0]);
    const runtimeModulePromise = loadJSModule(config.resources.jsModuleRuntime[0]);
    const wasmNativePromise = fetchWasm(config.resources.wasmNative[0]);

    const coreAssembliesPromise = Promise.all(config.resources.coreAssembly.map(fetchDll));
    const coreVfsPromise = Promise.all((config.resources.coreVfs || []).map(fetchVfs));
    const assembliesPromise = Promise.all(config.resources.assembly.map(fetchDll));
    const vfsPromise = Promise.all((config.resources.vfs || []).map(fetchVfs));
    const icuResourceName = getIcuResourceName(config);
    const icuDataPromise = icuResourceName ? Promise.all((config.resources.icu || []).filter(asset => asset.name === icuResourceName).map(fetchIcu)) : Promise.resolve([]);

    const nativeModule = await nativeModulePromise;
    const modulePromise = nativeModule.dotnetInitializeModule<EmscriptenModuleInternal>(dotnetGetInternals());
    nativeModulePromiseController.propagateFrom(modulePromise);

    const runtimeModule = await runtimeModulePromise;
    const runtimeModuleReady = runtimeModule.dotnetInitializeModule<void>(dotnetGetInternals());

    await nativeModulePromiseController.promise;
    await coreAssembliesPromise;
    await coreVfsPromise;
    await vfsPromise;
    await icuDataPromise;
    await wasmNativePromise; // this is just to propagate errors
    if (!downloadOnly) {
        BrowserHost_InitializeCoreCLR();
    }

    await assembliesPromise;
    await runtimeModuleReady;
}

async function loadJSModule(asset: JsAsset): Promise<JsModuleExports> {
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    if (!asset.resolvedUrl) throw new Error("Invalid config, resources is not set");
    return await import(/* webpackIgnore: true */ asset.resolvedUrl);
}

function fetchWasm(asset: WasmAsset): Promise<Response> {
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    if (!asset.resolvedUrl) throw new Error("Invalid config, resources is not set");
    wasmBinaryPromise = fetchLike(asset.resolvedUrl);
    return wasmBinaryPromise;
}

export async function instantiateWasm(imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback): Promise<void> {
    if (wasmBinaryPromise instanceof globalThis.Response === false || !WebAssembly.instantiateStreaming) {
        const res = await wasmBinaryPromise;
        const data = await res.arrayBuffer();
        const module = await WebAssembly.compile(data);
        const instance = await WebAssembly.instantiate(module, imports);
        successCallback(instance, module);
    } else {
        const res = await WebAssembly.instantiateStreaming(wasmBinaryPromise, imports);
        successCallback(res.instance, res.module);
    }
}

async function fetchIcu(asset: IcuAsset): Promise<void> {
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    const bytes = await fetchBytes(asset);
    await nativeModulePromiseController.promise;
    dotnetBrowserHostExports.loadIcuData(bytes);
}

async function fetchDll(asset: AssemblyAsset): Promise<void> {
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    const bytes = await fetchBytes(asset);
    await nativeModulePromiseController.promise;

    dotnetBrowserHostExports.registerDllBytes(bytes, asset);
}

async function fetchVfs(asset: AssemblyAsset): Promise<void> {
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    const bytes = await fetchBytes(asset);
    await nativeModulePromiseController.promise;

    dotnetBrowserHostExports.installVfsFile(bytes, asset);
}

async function fetchBytes(asset: WasmAsset | AssemblyAsset | PdbAsset | IcuAsset): Promise<Uint8Array> {
    dotnetAssert.check(asset && asset.resolvedUrl, "Bad asset.resolvedUrl");
    const response = await fetchLike(asset.resolvedUrl);
    const buffer = await response.arrayBuffer();
    return new Uint8Array(buffer);
}
