// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoadBootResourceCallback, JsModuleExports, JsAsset, AssemblyAsset, PdbAsset, WasmAsset, IcuAsset, EmscriptenModuleInternal } from "./types";

import { dotnetAssert, dotnetJSEngine, dotnetGetInternals, dotnetBrowserHostExports, dotnetUpdateAllInternals } from "./cross-module";
import { getLoaderConfig } from "./config";
import { BrowserHost_InitializeCoreCLR } from "./run";
import { createPromiseController } from "./promise-controller";

const scriptUrlQuery = /*! webpackIgnore: true */import.meta.url;
const queryIndex = scriptUrlQuery.indexOf("?");
const modulesUniqueQuery = queryIndex > 0 ? scriptUrlQuery.substring(queryIndex) : "";
const scriptUrl = normalizeFileUrl(scriptUrlQuery);
const scriptDirectory = normalizeDirectoryUrl(scriptUrl);

const nativeModulePromiseController = createPromiseController<EmscriptenModuleInternal>(() => {
    dotnetUpdateAllInternals();
});

// WASM-TODO: retry logic
// WASM-TODO: throttling logic
// WASM-TODO: load icu data
// WASM-TODO: invokeLibraryInitializers
// WASM-TODO: webCIL
// WASM-TODO: downloadOnly - blazor render mode auto pre-download. Really no start.

export async function createRuntime(downloadOnly: boolean, loadBootResource?: LoadBootResourceCallback): Promise<any> {
    const config = getLoaderConfig();
    if (!config.resources || !config.resources.coreAssembly || !config.resources.coreAssembly.length) throw new Error("Invalid config, resources is not set");

    const coreAssembliesPromise = Promise.all(config.resources.coreAssembly.map(fetchDll));
    const assembliesPromise = Promise.all(config.resources.assembly.map(fetchDll));
    const runtimeModulePromise = loadJSModule(config.resources.jsModuleRuntime[0], loadBootResource);
    const nativeModulePromise = loadJSModule(config.resources.jsModuleNative[0], loadBootResource);
    // WASM-TODO fetchWasm(config.resources.wasmNative[0]);// start loading early, no await

    const nativeModule = await nativeModulePromise;
    const modulePromise = nativeModule.dotnetInitializeModule<EmscriptenModuleInternal>(dotnetGetInternals());
    nativeModulePromiseController.propagateFrom(modulePromise);

    const runtimeModule = await runtimeModulePromise;
    const runtimeModuleReady = runtimeModule.dotnetInitializeModule<void>(dotnetGetInternals());

    await nativeModulePromiseController.promise;
    await coreAssembliesPromise;

    if (!downloadOnly) {
        BrowserHost_InitializeCoreCLR();
    }

    await assembliesPromise;
    await runtimeModuleReady;
}

async function loadJSModule(asset: JsAsset, loadBootResource?: LoadBootResourceCallback): Promise<JsModuleExports> {
    if (loadBootResource) throw new Error("TODO: loadBootResource is not implemented yet");
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    if (!asset.resolvedUrl) throw new Error("Invalid config, resources is not set");
    return await import(/* webpackIgnore: true */ asset.resolvedUrl);
}

async function fetchDll(asset: AssemblyAsset): Promise<void> {
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    const bytes = await fetchBytes(asset);
    await nativeModulePromiseController.promise;

    dotnetBrowserHostExports.registerDllBytes(bytes, asset);
}

async function fetchBytes(asset: WasmAsset|AssemblyAsset|PdbAsset|IcuAsset): Promise<Uint8Array> {
    dotnetAssert.check(asset && asset.resolvedUrl, "Bad asset.resolvedUrl");
    if (dotnetJSEngine.IS_NODE) {
        const { promises: fs } = await import("fs");
        const { fileURLToPath } = await import(/*! webpackIgnore: true */"url");
        const isFileUrl = asset.resolvedUrl!.startsWith("file://");
        if (isFileUrl) {
            asset.resolvedUrl = fileURLToPath(asset.resolvedUrl!);
        }
        const buffer = await fs.readFile(asset.resolvedUrl!);
        return new Uint8Array(buffer);
    } else {
        const response = await fetch(asset.resolvedUrl!);
        if (!response.ok) {
            throw new Error(`Failed to load ${asset.resolvedUrl} with ${response.status} ${response.statusText}`);
        }
        const buffer = await response.arrayBuffer();
        return new Uint8Array(buffer);
    }
}

function locateFile(path: string) {
    if ("URL" in globalThis) {
        return new URL(path, scriptDirectory).toString();
    }

    if (isPathAbsolute(path)) return path;
    return scriptDirectory + path + modulesUniqueQuery;
}

function normalizeFileUrl(filename: string) {
    // unix vs windows
    // remove query string
    return filename.replace(/\\/g, "/").replace(/[?#].*/, "");
}

function normalizeDirectoryUrl(dir: string) {
    return dir.slice(0, dir.lastIndexOf("/")) + "/";
}

const protocolRx = /^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//;
const windowsAbsoluteRx = /[a-zA-Z]:[\\/]/;
function isPathAbsolute(path: string): boolean {
    if (dotnetJSEngine.IS_NODE || dotnetJSEngine.IS_SHELL) {
        // unix /x.json
        // windows \x.json
        // windows C:\x.json
        // windows C:/x.json
        return path.startsWith("/") || path.startsWith("\\") || path.indexOf("///") !== -1 || windowsAbsoluteRx.test(path);
    }

    // anything with protocol is always absolute
    // windows file:///C:/x.json
    // windows http://C:/x.json
    return protocolRx.test(path);
}
