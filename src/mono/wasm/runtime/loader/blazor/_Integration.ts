// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetModuleInternal, MonoConfigInternal } from "../../types/internal";
import type { AssetBehaviours, AssetEntry, LoadingResource, WebAssemblyBootResourceType, WebAssemblyStartOptions } from "../../types";
import type { BootJsonData } from "../../types/blazor";

import { INTERNAL, loaderHelpers } from "../globals";
import { BootConfigResult } from "./BootConfig";
import { WebAssemblyResourceLoader } from "./WebAssemblyResourceLoader";
import { hasDebuggingEnabled } from "./_Polyfill";
import { ICUDataMode } from "../../types/blazor";

let resourceLoader: WebAssemblyResourceLoader;

export async function loadBootConfig(config: MonoConfigInternal, module: DotnetModuleInternal) {
    const candidateOptions = config.startupOptions ?? {};
    const environment = candidateOptions.environment;
    const bootConfigPromise = BootConfigResult.initAsync(candidateOptions.loadBootResource, environment);
    const bootConfigResult: BootConfigResult = await bootConfigPromise;
    await initializeBootConfig(bootConfigResult, module, candidateOptions);
}

export async function initializeBootConfig(bootConfigResult: BootConfigResult, module: DotnetModuleInternal, startupOptions?: Partial<WebAssemblyStartOptions>) {
    INTERNAL.resourceLoader = resourceLoader = await WebAssemblyResourceLoader.initAsync(bootConfigResult.bootConfig, startupOptions ?? {});
    mapBootConfigToMonoConfig(loaderHelpers.config, bootConfigResult.applicationEnvironment);
    setupModuleForBlazor(module);
}

let resourcesLoaded = 0;
let totalResources = 0;

const behaviorByName = (name: string): AssetBehaviours | "other" => {
    return name === "dotnet.native.wasm" ? "dotnetwasm"
        : (name.startsWith("dotnet.native.worker") && name.endsWith(".js")) ? "js-module-threads"
            : (name.startsWith("dotnet.native") && name.endsWith(".js")) ? "js-module-native"
                : (name.startsWith("dotnet.runtime") && name.endsWith(".js")) ? "js-module-runtime"
                    : (name.startsWith("dotnet") && name.endsWith(".js")) ? "js-module-dotnet"
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
    module.disableDotnet6Compatibility = false;
}

export function mapBootConfigToMonoConfig(moduleConfig: MonoConfigInternal, applicationEnvironment: string) {
    const resources = resourceLoader.bootConfig.resources;

    const assets: AssetEntry[] = [];
    const environmentVariables: any = {};

    moduleConfig.applicationEnvironment = applicationEnvironment;

    moduleConfig.assets = assets;
    moduleConfig.globalizationMode = "icu";
    moduleConfig.environmentVariables = environmentVariables;
    moduleConfig.debugLevel = hasDebuggingEnabled(resourceLoader.bootConfig) ? 1 : 0;
    moduleConfig.maxParallelDownloads = 1000000; // disable throttling parallel downloads
    moduleConfig.enableDownloadRetry = false; // disable retry downloads
    moduleConfig.mainAssemblyName = resourceLoader.bootConfig.entryAssembly;

    // FIXME this mix of both formats is ugly temporary hack
    Object.assign(moduleConfig, {
        ...resourceLoader.bootConfig,
    });

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
        asset.resolvedUrl = `_framework/${name}`;
        assets.push(asset);
    }
    for (const name in resources.assembly) {
        const asset: AssetEntry = {
            name,
            resolvedUrl: `_framework/${name}`,
            hash: resources.assembly[name],
            behavior: "assembly",
        };
        assets.push(asset);
    }
    if (hasDebuggingEnabled(resourceLoader.bootConfig) && resources.pdb) {
        for (const name in resources.pdb) {
            const asset: AssetEntry = {
                name,
                resolvedUrl: `_framework/${name}`,
                hash: resources.pdb[name],
                behavior: "pdb",
            };
            assets.push(asset);
        }
    }
    const applicationCulture = resourceLoader.startOptions.applicationCulture || (navigator.languages && navigator.languages[0]);
    const icuDataResourceName = getICUResourceName(resourceLoader.bootConfig, applicationCulture);
    let hasIcuData = false;
    for (const name in resources.runtime) {
        const behavior = behaviorByName(name) as any;
        if (behavior === "icu") {
            if (resourceLoader.bootConfig.icuDataMode === ICUDataMode.Invariant) {
                continue;
            }
            if (name !== icuDataResourceName) {
                continue;
            }
            hasIcuData = true;
        } else if (behavior === "js-module-dotnet") {
            continue;
        } else if (behavior === "dotnetwasm") {
            continue;
        }

        const resolvedUrl = name.endsWith(".js") ? `./${name}` : `_framework/${name}`;
        const asset: AssetEntry = {
            name,
            resolvedUrl,
            hash: resources.runtime[name],
            behavior,
        };
        assets.push(asset);
    }
    for (let i = 0; i < resourceLoader.bootConfig.config.length; i++) {
        const config = resourceLoader.bootConfig.config[i];
        if (config === "appsettings.json" || config === `appsettings.${applicationEnvironment}.json`) {
            assets.push({
                name: config,
                resolvedUrl: config,
                behavior: "vfs",
            });
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

function getICUResourceName(bootConfig: BootJsonData, culture: string | undefined): string {
    if (bootConfig.icuDataMode === ICUDataMode.Custom) {
        const icuFiles = Object
            .keys(bootConfig.resources.runtime)
            .filter(n => n.startsWith("icudt") && n.endsWith(".dat"));
        if (icuFiles.length === 1) {
            const customIcuFile = icuFiles[0];
            return customIcuFile;
        }
    }

    const combinedICUResourceName = "icudt.dat";
    if (!culture || bootConfig.icuDataMode === ICUDataMode.All) {
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

