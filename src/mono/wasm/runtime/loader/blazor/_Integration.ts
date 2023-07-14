// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetModuleInternal, MonoConfigInternal } from "../../types/internal";
import { GlobalizationMode, type AssetBehaviours, type AssetEntry, type LoadBootResourceCallback, type LoadingResource, type WebAssemblyBootResourceType } from "../../types";
import type { BootJsonData } from "../../types/blazor";

import { ENVIRONMENT_IS_WEB, INTERNAL, loaderHelpers } from "../globals";
import { BootConfigResult } from "./BootConfig";
import { WebAssemblyResourceLoader } from "./WebAssemblyResourceLoader";
import { hasDebuggingEnabled } from "./_Polyfill";
import { ICUDataMode } from "../../types/blazor";
import { appendUniqueQuery } from "../assets";

let resourceLoader: WebAssemblyResourceLoader;

export async function loadBootConfig(config: MonoConfigInternal, module: DotnetModuleInternal) {
    const bootConfigPromise = BootConfigResult.initAsync(loaderHelpers.loadBootResource, config.applicationEnvironment);
    const bootConfigResult: BootConfigResult = await bootConfigPromise;
    await initializeBootConfig(bootConfigResult, module, loaderHelpers.loadBootResource);
}

export async function initializeBootConfig(bootConfigResult: BootConfigResult, module: DotnetModuleInternal, loadBootResource?: LoadBootResourceCallback) {
    INTERNAL.resourceLoader = resourceLoader = await WebAssemblyResourceLoader.initAsync(bootConfigResult.bootConfig, loadBootResource);
    mapBootConfigToMonoConfig(loaderHelpers.config, bootConfigResult.applicationEnvironment);

    if (ENVIRONMENT_IS_WEB) {
        setupModuleForBlazor(module);
    }
}

let resourcesLoaded = 0;
const totalResources = new Set<string>();

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

            totalResources.add(asset.name!);
            res.response.then(() => {
                resourcesLoaded++;
                if (module.onDownloadResourceProgress)
                    module.onDownloadResourceProgress(resourcesLoaded, totalResources.size);
            });

            return res;
        }
        return undefined;
    };

    loaderHelpers.downloadResource = downloadResource; // polyfills were already assigned
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
    moduleConfig.extensions = resourceLoader.bootConfig.extensions;
    moduleConfig.resources = {
        extensions: resources.extensions
    };

    // Default values (when WasmDebugLevel is not set)
    // - Build   (debug)    => debugBuild=true  & debugLevel=-1 => -1
    // - Build   (release)  => debugBuild=true  & debugLevel=0  => 0
    // - Publish (debug)    => debugBuild=false & debugLevel=-1 => 0
    // - Publish (release)  => debugBuild=false & debugLevel=0  => 0
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
        asset.resolvedUrl = appendUniqueQuery(loaderHelpers.locateFile(name), asset.behavior);
        assets.push(asset);
    }
    for (const name in resources.assembly) {
        const asset: AssetEntry = {
            name,
            resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name), "assembly"),
            hash: resources.assembly[name],
            behavior: "assembly",
        };
        assets.push(asset);
    }
    if (hasDebuggingEnabled(resourceLoader.bootConfig) && resources.pdb) {
        for (const name in resources.pdb) {
            const asset: AssetEntry = {
                name,
                resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name), "pdb"),
                hash: resources.pdb[name],
                behavior: "pdb",
            };
            assets.push(asset);
        }
    }
    const applicationCulture = moduleConfig.applicationCulture || (ENVIRONMENT_IS_WEB ? (navigator.languages && navigator.languages[0]) : Intl.DateTimeFormat().resolvedOptions().locale);
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

        const resolvedUrl = appendUniqueQuery(loaderHelpers.locateFile(name), behavior);
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
        const configFileName = fileName(config);
        if (configFileName === "appsettings.json" || configFileName === `appsettings.${applicationEnvironment}.json`) {
            assets.push({
                name: configFileName,
                resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(config), "vfs"),
                behavior: "vfs",
            });
        }
    }

    for (const virtualPath in resources.vfs) {
        for (const name in resources.vfs[virtualPath]) {
            const asset: AssetEntry = {
                name,
                resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name), "vfs"),
                hash: resources.vfs[virtualPath][name],
                behavior: "vfs",
                virtualPath
            };
            assets.push(asset);
        }
    }

    if (!hasIcuData) {
        moduleConfig.globalizationMode = GlobalizationMode.Invariant;
    }

    if (resourceLoader.bootConfig.modifiableAssemblies) {
        // Configure the app to enable hot reload in Development.
        environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = resourceLoader.bootConfig.modifiableAssemblies;
    }

    if (resourceLoader.bootConfig.aspnetCoreBrowserTools) {
        // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
        environmentVariables["__ASPNETCORE_BROWSER_TOOLS"] = resourceLoader.bootConfig.aspnetCoreBrowserTools;
    }

    if (moduleConfig.applicationCulture) {
        // If a culture is specified via start options use that to initialize the Emscripten \  .NET culture.
        environmentVariables["LANG"] = `${moduleConfig.applicationCulture}.UTF-8`;
    }

    if (resourceLoader.bootConfig.startupMemoryCache !== undefined) {
        moduleConfig.startupMemoryCache = resourceLoader.bootConfig.startupMemoryCache;
    }

    if (resourceLoader.bootConfig.runtimeOptions) {
        moduleConfig.runtimeOptions = [...(moduleConfig.runtimeOptions || []), ...(resourceLoader.bootConfig.runtimeOptions || [])];
    }
}

function fileName(name: string) {
    let lastIndexOfSlash = name.lastIndexOf("/");
    if (lastIndexOfSlash >= 0) {
        lastIndexOfSlash++;
    }
    return name.substring(lastIndexOfSlash);
}

function getICUResourceName(bootConfig: BootJsonData, moduleConfig: MonoConfigInternal, culture: string | undefined): string {
    if (bootConfig.icuDataMode === ICUDataMode.Custom) {
        const icuFiles = Object
            .keys(bootConfig.resources.runtime)
            .filter(n => n.startsWith("icudt") && n.endsWith(".dat"));
        if (icuFiles.length === 1) {
            moduleConfig.globalizationMode = GlobalizationMode.Custom;
            const customIcuFile = icuFiles[0];
            return customIcuFile;
        }
    }

    if (bootConfig.icuDataMode === ICUDataMode.Hybrid) {
        moduleConfig.globalizationMode = GlobalizationMode.Hybrid;
        const reducedICUResourceName = "icudt_hybrid.dat";
        return reducedICUResourceName;
    }

    if (!culture || bootConfig.icuDataMode === ICUDataMode.All) {
        moduleConfig.globalizationMode = GlobalizationMode.All;
        const combinedICUResourceName = "icudt.dat";
        return combinedICUResourceName;
    }

    moduleConfig.globalizationMode = GlobalizationMode.Sharded;
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

