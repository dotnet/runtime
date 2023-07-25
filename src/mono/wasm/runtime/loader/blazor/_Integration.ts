// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetModuleInternal, MonoConfigInternal } from "../../types/internal";
import { GlobalizationMode, type AssetBehaviours, type AssetEntry, type LoadingResource, type WebAssemblyBootResourceType } from "../../types";
import type { BootJsonData } from "../../types/blazor";

import { ENVIRONMENT_IS_WEB, loaderHelpers } from "../globals";
import { initCacheToUseIfEnabled, loadResource } from "../resourceLoader";
import { ICUDataMode } from "../../types/blazor";
import { appendUniqueQuery } from "../assets";
import { hasDebuggingEnabled } from "../config";

export async function loadBootConfig(module: DotnetModuleInternal) {
    const defaultBootJsonLocation = "_framework/blazor.boot.json";
    const loaderResponse = loaderHelpers.loadBootResource !== undefined ?
        loaderHelpers.loadBootResource("manifest", "blazor.boot.json", defaultBootJsonLocation, "") :
        defaultLoadBlazorBootJson(defaultBootJsonLocation);

    let bootConfigResponse: Response;

    if (!loaderResponse) {
        bootConfigResponse = await defaultLoadBlazorBootJson(defaultBootJsonLocation);
    } else if (typeof loaderResponse === "string") {
        bootConfigResponse = await defaultLoadBlazorBootJson(loaderResponse);
    } else {
        bootConfigResponse = await loaderResponse;
    }

    const bootConfig: BootJsonData = await bootConfigResponse.json();

    await initializeBootConfig(bootConfigResponse, bootConfig, module);

    function defaultLoadBlazorBootJson(url: string): Promise<Response> {
        return fetch(url, {
            method: "GET",
            credentials: "include",
            cache: "no-cache",
        });
    }
}

function readBootConfigResponseHeaders(bootConfigResponse: Response) {
    const config = loaderHelpers.config;

    config.applicationEnvironment = config.applicationEnvironment
        || bootConfigResponse.headers.get("Blazor-Environment")
        || bootConfigResponse.headers.get("DotNet-Environment")
        || "Production";

    if (!config.environmentVariables)
        config.environmentVariables = {};

    const modifiableAssemblies = bootConfigResponse.headers.get("DOTNET-MODIFIABLE-ASSEMBLIES");
    if (modifiableAssemblies) {
        // Configure the app to enable hot reload in Development.
        config.environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = modifiableAssemblies;
    }

    const aspnetCoreBrowserTools = bootConfigResponse.headers.get("ASPNETCORE-BROWSER-TOOLS");
    if (aspnetCoreBrowserTools) {
        // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
        config.environmentVariables["__ASPNETCORE_BROWSER_TOOLS"] = aspnetCoreBrowserTools;
    }
}

export async function initializeBootConfig(bootConfigResponse: Response, bootConfig: BootJsonData, module: DotnetModuleInternal) {
    readBootConfigResponseHeaders(bootConfigResponse);
    await initCacheToUseIfEnabled();
    mapBootConfigToMonoConfig(bootConfig);
    hookDownloadResource(module);
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

export function hookDownloadResource(module: DotnetModuleInternal) {
    // it would not `loadResource` on types for which there is no typesMap mapping
    const downloadResource = (asset: AssetEntry): LoadingResource | undefined => {
        // GOTCHA: the mapping to blazor asset type may not cover all mono owned asset types in the future in which case:
        // A) we may need to add such asset types to the mapping and to WebAssemblyBootResourceType
        // B) or we could add generic "runtime" type to WebAssemblyBootResourceType as fallback
        // C) or we could return `undefined` and let the runtime to load the asset. In which case the progress will not be reported on it and blazor will not be able to cache it.
        const type = monoToBlazorAssetTypeMap[asset.behavior];
        if (type !== undefined) {
            const res = loadResource(asset.name, asset.resolvedUrl!, asset.hash!, type);

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

export function mapBootConfigToMonoConfig(bootConfig: BootJsonData) {
    const config = loaderHelpers.config;
    const resources = bootConfig.resources;

    const assets: AssetEntry[] = [];
    const environmentVariables: any = {
        // From boot config
        ...(bootConfig.environmentVariables || {}),
        // From JavaScript
        ...(config.environmentVariables || {})
    };

    config.cacheBootResources = bootConfig.cacheBootResources;
    config.linkerEnabled = bootConfig.linkerEnabled;
    config.remoteSources = (resources as any).remoteSources;
    config.assetsHash = bootConfig.resources.hash;
    config.assets = assets;
    config.extensions = bootConfig.extensions;
    config.resources = {
        extensions: resources.extensions
    };

    // Default values (when WasmDebugLevel is not set)
    // - Build   (debug)    => debugBuild=true  & debugLevel=-1 => -1
    // - Build   (release)  => debugBuild=true  & debugLevel=0  => 0
    // - Publish (debug)    => debugBuild=false & debugLevel=-1 => 0
    // - Publish (release)  => debugBuild=false & debugLevel=0  => 0
    config.debugLevel = hasDebuggingEnabled(config) ? bootConfig.debugLevel : 0;
    config.mainAssemblyName = bootConfig.entryAssembly;

    const anyBootConfig = (bootConfig as any);
    for (const key in bootConfig) {
        if (Object.prototype.hasOwnProperty.call(anyBootConfig, key)) {
            if (anyBootConfig[key] === null) {
                delete anyBootConfig[key];
            }
        }
    }

    // FIXME this mix of both formats is ugly temporary hack
    Object.assign(config, {
        ...bootConfig,
    });

    config.environmentVariables = environmentVariables;

    if (bootConfig.startupMemoryCache !== undefined) {
        config.startupMemoryCache = bootConfig.startupMemoryCache;
    }

    if (bootConfig.runtimeOptions) {
        config.runtimeOptions = [...(config.runtimeOptions || []), ...(bootConfig.runtimeOptions || [])];
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
    if (config.debugLevel != 0 && resources.pdb) {
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
    const applicationCulture = config.applicationCulture || (ENVIRONMENT_IS_WEB ? (navigator.languages && navigator.languages[0]) : Intl.DateTimeFormat().resolvedOptions().locale);
    const icuDataResourceName = getICUResourceName(bootConfig, config, applicationCulture);
    let hasIcuData = false;
    for (const name in resources.runtime) {
        const behavior = behaviorByName(name) as any;
        let loadRemote = false;
        if (behavior === "icu") {
            if (bootConfig.icuDataMode === ICUDataMode.Invariant) {
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

    if (config.loadAllSatelliteResources && resources.satelliteResources) {
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

    for (let i = 0; i < bootConfig.config.length; i++) {
        const configUrl = bootConfig.config[i];
        const configFileName = fileName(configUrl);
        if (configFileName === "appsettings.json" || configFileName === `appsettings.${config.applicationEnvironment}.json`) {
            assets.push({
                name: configFileName,
                resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(configUrl), "vfs"),
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
        config.globalizationMode = GlobalizationMode.Invariant;
    }

    if (bootConfig.modifiableAssemblies) {
        // Configure the app to enable hot reload in Development.
        environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = bootConfig.modifiableAssemblies;
    }

    if (bootConfig.aspnetCoreBrowserTools) {
        // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
        environmentVariables["__ASPNETCORE_BROWSER_TOOLS"] = bootConfig.aspnetCoreBrowserTools;
    }

    if (config.applicationCulture) {
        // If a culture is specified via start options use that to initialize the Emscripten \  .NET culture.
        environmentVariables["LANG"] = `${config.applicationCulture}.UTF-8`;
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

