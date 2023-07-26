// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetModuleInternal } from "../../types/internal";
import { GlobalizationMode, type AssetBehaviours, type AssetEntry, type LoadingResource, type WebAssemblyBootResourceType, MonoConfig } from "../../types";

import { ENVIRONMENT_IS_WEB, loaderHelpers, mono_assert } from "../globals";
import { loadResource } from "../resourceLoader";
import { appendUniqueQuery } from "../assets";
import { deep_merge_config } from "../config";

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

export function mapResourcesToAssets(loadedConfig: MonoConfig) {
    mono_assert(loadedConfig.resources, "Loaded config does not contain resources");

    const config = loaderHelpers.config;
    const resources = loadedConfig.resources;

    deep_merge_config(config, loadedConfig);

    const assets = config.assets!;

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
    const icuDataResourceName = getICUResourceName(loadedConfig, config, applicationCulture);
    let hasIcuData = false;
    for (const name in resources.runtime) {
        const behavior = behaviorByName(name) as any;
        let loadRemote = false;
        if (behavior === "icu") {
            if (loadedConfig.globalizationMode === GlobalizationMode.Invariant) {
                continue;
            }
            if (name !== icuDataResourceName) {
                continue;
            }
            loadRemote = true;
            hasIcuData = true;
        } else if (behavior === "js-module-dotnet") {
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

    if (loadedConfig.config) {
        for (let i = 0; i < loadedConfig.config.length; i++) {
            const configUrl = loadedConfig.config[i];
            const configFileName = fileName(configUrl);
            if (configFileName === "appsettings.json" || configFileName === `appsettings.${config.applicationEnvironment}.json`) {
                assets.push({
                    name: configFileName,
                    resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(configUrl), "vfs"),
                    behavior: "vfs",
                });
            }
        }
    }

    if (resources.vfs) {
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
    }

    if (!hasIcuData) {
        config.globalizationMode = GlobalizationMode.Invariant;
    }
}

function fileName(name: string) {
    let lastIndexOfSlash = name.lastIndexOf("/");
    if (lastIndexOfSlash >= 0) {
        lastIndexOfSlash++;
    }
    return name.substring(lastIndexOfSlash);
}

function getICUResourceName(loadedConfig: MonoConfig, config: MonoConfig, culture: string | undefined): string {
    mono_assert(loadedConfig.resources?.runtime, "Boot config does not contain runtime resources");

    if (loadedConfig.globalizationMode === GlobalizationMode.Custom) {
        const icuFiles = Object
            .keys(loadedConfig.resources.runtime)
            .filter(n => n.startsWith("icudt") && n.endsWith(".dat"));
        if (icuFiles.length === 1) {
            config.globalizationMode = GlobalizationMode.Custom;
            const customIcuFile = icuFiles[0];
            return customIcuFile;
        }
    }

    if (loadedConfig.globalizationMode === GlobalizationMode.Hybrid) {
        config.globalizationMode = GlobalizationMode.Hybrid;
        const reducedICUResourceName = "icudt_hybrid.dat";
        return reducedICUResourceName;
    }

    if (!culture || loadedConfig.globalizationMode === GlobalizationMode.All) {
        config.globalizationMode = GlobalizationMode.All;
        const combinedICUResourceName = "icudt.dat";
        return combinedICUResourceName;
    }

    config.globalizationMode = GlobalizationMode.Sharded;
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

