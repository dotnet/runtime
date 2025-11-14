// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { type LoadBootResourceCallback, type JsModuleExports, type JsAsset, type AssemblyAsset, type PdbAsset, type WasmAsset, type IcuAsset, type EmscriptenModuleInternal, type LoaderConfig, type DotnetHostBuilder, GlobalizationMode } from "./types";

import { dotnetAssert, dotnetGetInternals, dotnetBrowserHostExports, dotnetUpdateInternals } from "./cross-module";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL } from "./per-module";
import { getLoaderConfig } from "./config";
import { BrowserHost_InitializeCoreCLR } from "./run";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { nodeFs } from "./polyfills";

const scriptUrlQuery = /*! webpackIgnore: true */import.meta.url;
const queryIndex = scriptUrlQuery.indexOf("?");
const modulesUniqueQuery = queryIndex > 0 ? scriptUrlQuery.substring(queryIndex) : "";
const scriptUrl = normalizeFileUrl(scriptUrlQuery);
const scriptDirectory = normalizeDirectoryUrl(scriptUrl);

const nativeModulePromiseController = createPromiseCompletionSource<EmscriptenModuleInternal>(() => {
    dotnetUpdateInternals(dotnetGetInternals());
});

// WASM-TODO: retry logic
// WASM-TODO: throttling logic
// WASM-TODO: invokeLibraryInitializers
// WASM-TODO: webCIL
// WASM-TODO: downloadOnly - blazor render mode auto pre-download. Really no start.

export async function createRuntime(downloadOnly: boolean, loadBootResource?: LoadBootResourceCallback): Promise<any> {
    const config = getLoaderConfig();
    if (!config.resources || !config.resources.coreAssembly || !config.resources.coreAssembly.length) throw new Error("Invalid config, resources is not set");

    const nativeModulePromise = loadJSModule(config.resources.jsModuleNative[0], loadBootResource);
    const runtimeModulePromise = loadJSModule(config.resources.jsModuleRuntime[0], loadBootResource);
    const coreAssembliesPromise = Promise.all(config.resources.coreAssembly.map(fetchDll));
    const coreVfsPromise = Promise.all((config.resources.coreVfs || []).map(fetchVfs));
    const assembliesPromise = Promise.all(config.resources.assembly.map(fetchDll));
    const vfsPromise = Promise.all((config.resources.vfs || []).map(fetchVfs));
    const icuResourceName = getIcuResourceName(config);
    const icuDataPromise = icuResourceName ? Promise.all((config.resources.icu || []).filter(asset => asset.name === icuResourceName).map(fetchIcu)) : Promise.resolve([]);
    // WASM-TODO fetchWasm(config.resources.wasmNative[0]);// start loading early, no await

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

async function fetchIcu(asset: IcuAsset): Promise<void> {
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    const bytes = await fetchBytes(asset);
    await nativeModulePromiseController.promise;
    dotnetBrowserHostExports.loadIcuData(bytes, asset);
}

function getIcuResourceName(config: LoaderConfig): string | null {
    if (config.resources?.icu && config.globalizationMode != GlobalizationMode.Invariant) {
        const culture = config.applicationCulture;
        if (!config.applicationCulture) {
            config.applicationCulture = culture;
        }

        const icuFiles = config.resources.icu;
        let icuFile = null;
        if (config.globalizationMode === GlobalizationMode.Custom) {
            // custom ICU file is saved in the resources with fingerprinting and does not require mapping
            if (icuFiles.length >= 1) {
                return icuFiles[0].name;
            }
        } else if (!culture || config.globalizationMode === GlobalizationMode.All) {
            icuFile = "icudt.dat";
        } else if (config.globalizationMode === GlobalizationMode.Sharded) {
            icuFile = getShardedIcuResourceName(culture);
        }

        if (icuFile) {
            for (let i = 0; i < icuFiles.length; i++) {
                const asset = icuFiles[i];
                if (asset.virtualPath === icuFile) {
                    return asset.name;
                }
            }
        }
    }

    config.globalizationMode = GlobalizationMode.Invariant;
    return null;
}

function getShardedIcuResourceName(culture: string): string {
    const prefix = culture.split("-")[0];
    if (prefix === "en" || ["fr", "fr-FR", "it", "it-IT", "de", "de-DE", "es", "es-ES"].includes(culture)) {
        return "icudt_EFIGS.dat";
    }

    if (["zh", "ko", "ja"].includes(prefix)) {
        return "icudt_CJK.dat";
    }

    return "icudt_no_CJK.dat";
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
    if (ENVIRONMENT_IS_NODE) {
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
    if (ENVIRONMENT_IS_NODE || ENVIRONMENT_IS_SHELL) {
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

export function isShellHosted(): boolean {
    return ENVIRONMENT_IS_SHELL && typeof (globalThis as any).arguments !== "undefined";
}

export function isNodeHosted(): boolean {
    if (!ENVIRONMENT_IS_NODE || globalThis.process.argv.length < 3) {
        return false;
    }
    const argv1 = globalThis.process.argv[1].toLowerCase();
    const argScript = normalizeFileUrl("file:///" + locateFile(argv1));
    const importScript = normalizeFileUrl(locateFile(scriptUrl.toLowerCase()));

    return argScript === importScript;
}

export async function findResources(dotnet: DotnetHostBuilder): Promise<void> {
    if (!ENVIRONMENT_IS_NODE) {
        return;
    }
    const fs = await nodeFs();
    const mountedDir = "/managed";
    const files: string[] = await fs.promises.readdir(".");
    const assemblies = files
        // TODO-WASM: webCIL
        .filter(file => file.endsWith(".dll"))
        .map(filename => {
            // filename without path
            const name = filename.substring(filename.lastIndexOf("/") + 1);
            return { virtualPath: mountedDir + "/" + filename, name };
        });
    const mainAssemblyName = globalThis.process.argv[2];
    const runtimeConfigName = mainAssemblyName.replace(/\.dll$/, ".runtimeconfig.json");
    let runtimeConfig = {};
    if (fs.existsSync(runtimeConfigName)) {
        const json = await fs.promises.readFile(runtimeConfigName, { encoding: "utf8" });
        runtimeConfig = JSON.parse(json);
    }

    const config: LoaderConfig = {
        mainAssemblyName,
        runtimeConfig,
        virtualWorkingDirectory: mountedDir,
        resources: {
            jsModuleNative: [{ name: "dotnet.native.js" }],
            jsModuleRuntime: [{ name: "dotnet.runtime.js" }],
            wasmNative: [{ name: "dotnet.native.wasm", }],
            coreAssembly: [{ virtualPath: mountedDir + "/System.Private.CoreLib.dll", name: "System.Private.CoreLib.dll" },],
            assembly: assemblies,
        }
    };
    dotnet.withConfig(config);
    dotnet.withApplicationArguments(...globalThis.process.argv.slice(3));
}
