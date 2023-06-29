// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetModuleInternal, MonoConfigInternal } from "../../types/internal";
import type { AssetBehaviours, AssetEntry, LoadingResource, WebAssemblyBootResourceType, WebAssemblyStartOptions } from "../../types";
import type { BootJsonData } from "../../types/blazor";

import { ENVIRONMENT_IS_WEB, INTERNAL, loaderHelpers } from "../globals";
import { BootConfigResult } from "./BootConfig";
import { WebAssemblyResourceLoader } from "./WebAssemblyResourceLoader";
import { hasDebuggingEnabled } from "./_Polyfill";
import { ICUDataMode } from "../../types/blazor";

let resourceLoader: WebAssemblyResourceLoader;

export async function loadBootConfig(config: MonoConfigInternal, module: DotnetModuleInternal) {
    const bootConfigPromise = BootConfigResult.initAsync(config.startupOptions?.loadBootResource, config.applicationEnvironment);
    const bootConfigResult: BootConfigResult = await bootConfigPromise;
    await initializeBootConfig(bootConfigResult, module, config.startupOptions);
}

export async function initializeBootConfig(bootConfigResult: BootConfigResult, module: DotnetModuleInternal, startupOptions?: Partial<WebAssemblyStartOptions>) {
    INTERNAL.resourceLoader = resourceLoader = await WebAssemblyResourceLoader.initAsync(bootConfigResult.bootConfig, startupOptions ?? {});
    mapBootConfigToMonoConfig(loaderHelpers.config, bootConfigResult.applicationEnvironment);

    if (ENVIRONMENT_IS_WEB) {
        setupModuleForBlazor(module);
    }
}

let resourcesLoaded = 0;
let totalResources = 0;

const behaviorByName = (name: string): AssetBehaviours | "other" => {
    return name === "dotnet.native.wasm" ? "dotnetwasm"
        : (name.startsWith("dotnet.native.worker") && name.endsWith(".js")) ? "js-module-threads"
            : (name.startsWith("dotnet.native") && name.endsWith(".js")) ? "js-module-native"
                : (name.startsWith("dotnet.runtime") && name.endsWith(".js")) ? "js-module-runtime"
                    : (name.startsWith("dotnet") && name.endsWith(".js")) ? "js-module-dotnet"
                        : (name.startsWith("dotnet.native") && name.endsWith(".symbols")) ? "symbols"
                            : name.startsWith("icudt") ? "icu"
                                : "other";
};

const monoToBlazorAssetTypeMap: { [key: string]: WebAssemblyBootResourceType | undefined } = {
    "assembly": "assembly",
    "pdb": "pdb",
    "icu": "globalization",
    "vfs": "configuration",
    "dotnetwasm": "dotnetwasm",
};

export function setupModuleForBlazor(module: DotnetModuleInternal) {
    // it would not `loadResource` on types for which there is no typesMap mapping
    const downloadResource = (asset: AssetEntry): LoadingResource | undefined => {
        // GOTCHA: the mapping to blazor asset type may not cover all mono owned asset types in the future in which case:
        // A) we may need to add such asset types to the mapping and to WebAssemblyBootResourceType
        // B) or we could add generic "runtime" type to WebAssemblyBootResourceType as fallback
        // C) or we could return `undefined` and let the runtime to load the asset. In which case the progress will not be reported on it and blazor will not be able to cache it.
        const type = monoToBlazorAssetTypeMap[asset.behavior];
        if (type !== undefined) {
            const res = resourceLoader.loadResource(asset.name, asset.resolvedUrl!, asset.hash!, type);
            asset.pendingDownload = res;

            totalResources++;
            res.response.then(() => {
                resourcesLoaded++;
                if (module.onDownloadResourceProgress)
                    module.onDownloadResourceProgress(resourcesLoaded, totalResources);
            });

            return res;
        }
        return undefined;
    };

    loaderHelpers.downloadResource = downloadResource; // polyfills were already assigned
}

function appendUniqueQuery(attemptUrl: string): string {
    if (loaderHelpers.assetUniqueQuery) {
        attemptUrl = attemptUrl + loaderHelpers.assetUniqueQuery;
    }

    return attemptUrl;
}

export function mapBootConfigToMonoConfig(moduleConfig: MonoConfigInternal, applicationEnvironment: string) {
    const resources = resourceLoader.bootConfig.resources;

    const assets: AssetEntry[] = [];
    const environmentVariables: any = {
        // From boot config
        ...(resourceLoader.bootConfig.environmentVariables || {}),
        // From JavaScript
        ...(moduleConfig.environmentVariables || {})
    };

    moduleConfig.applicationEnvironment = applicationEnvironment;

    moduleConfig.remoteSources = (resourceLoader.bootConfig.resources as any).remoteSources;
    moduleConfig.assetsHash = resourceLoader.bootConfig.resources.hash;
    moduleConfig.assets = assets;
    moduleConfig.globalizationMode = "icu";
    moduleConfig.debugLevel = hasDebuggingEnabled(resourceLoader.bootConfig) ? resourceLoader.bootConfig.debugLevel : 0;
    moduleConfig.mainAssemblyName = resourceLoader.bootConfig.entryAssembly;

    const anyBootConfig = (resourceLoader.bootConfig as any);
    for (const key in resourceLoader.bootConfig) {
        if (Object.prototype.hasOwnProperty.call(anyBootConfig, key)) {
            if (anyBootConfig[key] === null) {
                delete anyBootConfig[key];
            }
        }
    }

    // FIXME this mix of both formats is ugly temporary hack
    Object.assign(moduleConfig, {
        ...resourceLoader.bootConfig,
    });

    moduleConfig.environmentVariables = environmentVariables;

    if (resourceLoader.bootConfig.startupMemoryCache !== undefined) {
        moduleConfig.startupMemoryCache = resourceLoader.bootConfig.startupMemoryCache;
    }

    if (resourceLoader.bootConfig.runtimeOptions) {
        moduleConfig.runtimeOptions = [...(moduleConfig.runtimeOptions || []), ...resourceLoader.bootConfig.runtimeOptions];
    }

    // any runtime owned assets, with proper behavior already set
    for (const name in resources.runtimeAssets) {
        const asset = resources.runtimeAssets[name] as AssetEntry;
        asset.name = name;
        asset.resolvedUrl = appendUniqueQuery(loaderHelpers.locateFile(name));
        assets.push(asset);
    }
    for (const name in resources.assembly) {
        const asset: AssetEntry = {
            name,
            resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name)),
            hash: resources.assembly[name],
            behavior: "assembly",
        };
        assets.push(asset);
    }
    if (hasDebuggingEnabled(resourceLoader.bootConfig) && resources.pdb) {
        for (const name in resources.pdb) {
            const asset: AssetEntry = {
                name,
                resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name)),
                hash: resources.pdb[name],
                behavior: "pdb",
            };
            assets.push(asset);
        }
    }
    const applicationCulture = resourceLoader.startOptions.applicationCulture || ENVIRONMENT_IS_WEB ? (navigator.languages && navigator.languages[0]) : Intl.DateTimeFormat().resolvedOptions().locale;
    const icuDataResourceName = getICUResourceName(resourceLoader.bootConfig, moduleConfig, applicationCulture);
    let hasIcuData = false;
    for (const name in resources.runtime) {
        const behavior = behaviorByName(name) as any;
        let loadRemote = false;
        if (behavior === "icu") {
            if (resourceLoader.bootConfig.icuDataMode === ICUDataMode.Invariant) {
                continue;
            }
            if (name !== icuDataResourceName) {
                continue;
            }
            loadRemote = true;
            hasIcuData = true;
        } else if (behavior === "js-module-dotnet") {
            continue;
        } else if (behavior === "dotnetwasm") {
            continue;
        }

        const resolvedUrl = appendUniqueQuery(loaderHelpers.locateFile(name));
        const asset: AssetEntry = {
            name,
            resolvedUrl,
            hash: resources.runtime[name],
            behavior,
            loadRemote
        };
        assets.push(asset);
    }

    if (moduleConfig.loadAllSatelliteResources && resources.satelliteResources) {
        for (const culture in resources.satelliteResources) {
            for (const name in resources.satelliteResources[culture]) {
                assets.push({
                    name,
                    culture,
                    behavior: "resource",
                    hash: resources.satelliteResources[culture][name],
                });
            }
        }
    }

    for (let i = 0; i < resourceLoader.bootConfig.config.length; i++) {
        const config = resourceLoader.bootConfig.config[i];
        if (config === "appsettings.json" || config === `appsettings.${applicationEnvironment}.json`) {
            assets.push({
                name: config,
                resolvedUrl: appendUniqueQuery((document ? document.baseURI : "/") + config),
                behavior: "vfs",
            });
        }
    }

    for (const virtualPath in resources.vfs) {
        for (const name in resources.vfs[virtualPath]) {
            const asset: AssetEntry = {
                name,
                resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name)),
                hash: resources.vfs[virtualPath][name],
                behavior: "vfs",
                virtualPath
            };
            assets.push(asset);
        }
    }

    if (!hasIcuData) {
        moduleConfig.globalizationMode = "invariant";
    }

    if (resourceLoader.bootConfig.modifiableAssemblies) {
        // Configure the app to enable hot reload in Development.
        environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = resourceLoader.bootConfig.modifiableAssemblies;
    }

    if (resourceLoader.startOptions.applicationCulture) {
        // If a culture is specified via start options use that to initialize the Emscripten \  .NET culture.
        environmentVariables["LANG"] = `${resourceLoader.startOptions.applicationCulture}.UTF-8`;
    }

    if (resourceLoader.bootConfig.startupMemoryCache !== undefined) {
        moduleConfig.startupMemoryCache = resourceLoader.bootConfig.startupMemoryCache;
    }

    if (resourceLoader.bootConfig.runtimeOptions) {
        moduleConfig.runtimeOptions = [...(moduleConfig.runtimeOptions || []), ...(resourceLoader.bootConfig.runtimeOptions || [])];
    }
}

function getICUResourceName(bootConfig: BootJsonData, moduleConfig: MonoConfigInternal, culture: string | undefined): string {
    if (bootConfig.icuDataMode === ICUDataMode.Custom) {
        const icuFiles = Object
            .keys(bootConfig.resources.runtime)
            .filter(n => n.startsWith("icudt") && n.endsWith(".dat"));
        if (icuFiles.length === 1) {
            moduleConfig.globalizationMode = "icu";
            const customIcuFile = icuFiles[0];
            return customIcuFile;
        }
    }

    if (bootConfig.icuDataMode === ICUDataMode.Hybrid) {
        moduleConfig.globalizationMode = "hybrid";
        const reducedICUResourceName = "icudt_hybrid.dat";
        return reducedICUResourceName;
    }

    if (!culture || bootConfig.icuDataMode === ICUDataMode.All) {
        moduleConfig.globalizationMode = "icu";
        const combinedICUResourceName = "icudt.dat";
        return combinedICUResourceName;
    }

    const prefix = culture.split("-")[0];
    if (prefix === "en" ||
        [
            "fr",
            "fr-FR",
            "it",
            "it-IT",
            "de",
            "de-DE",
            "es",
            "es-ES",
        ].includes(culture)) {
        return "icudt_EFIGS.dat";
    }
    if ([
        "zh",
        "ko",
        "ja",
    ].includes(prefix)) {
        return "icudt_CJK.dat";
    }
    return "icudt_no_CJK.dat";
}

