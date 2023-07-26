// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { GlobalizationMode, type AssetEntry, MonoConfig } from "../../types";

import { ENVIRONMENT_IS_WEB, loaderHelpers, mono_assert } from "../globals";
import { appendUniqueQuery } from "../assets";
import { deep_merge_config } from "../config";

export function mapResourcesToAssets(loadedConfig: MonoConfig) {
    mono_assert(loadedConfig.resources, "Loaded config does not contain resources");

    const config = loaderHelpers.config;
    const resources = loadedConfig.resources;
    const nativeResources = resources.native!;

    deep_merge_config(config, loadedConfig);

    if (!config.assets) {
        config.assets = [];
    }

    const assets = config.assets;

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

    for (const name in nativeResources.jsModuleWorker) {
        const behavior = "js-module-threads";
        assets.push({
            name,
            resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name), behavior),
            hash: nativeResources.jsModuleWorker[name],
            behavior
        });
    }

    for (const name in nativeResources.jsModuleNative) {
        const behavior = "js-module-native";
        assets.push({
            name,
            resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name), behavior),
            hash: nativeResources.jsModuleNative[name],
            behavior
        });
    }

    for (const name in nativeResources.jsModuleRuntime) {
        const behavior = "js-module-runtime";
        assets.push({
            name,
            resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name), behavior),
            hash: nativeResources.jsModuleRuntime[name],
            behavior
        });
    }

    for (const name in nativeResources.wasmNative) {
        const behavior = "dotnetwasm";
        assets.push({
            name,
            resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name), behavior),
            hash: nativeResources.wasmNative[name],
            behavior
        });
    }

    for (const name in nativeResources.symbols) {
        const behavior = "symbols";
        assets.push({
            name,
            resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(name), behavior),
            hash: nativeResources.symbols[name],
            behavior
        });
    }

    const applicationCulture = config.applicationCulture || (ENVIRONMENT_IS_WEB ? (navigator.languages && navigator.languages[0]) : Intl.DateTimeFormat().resolvedOptions().locale);
    const icuDataResourceName = getICUResourceName(loadedConfig, config, applicationCulture);
    let hasIcuData = false;
    if (icuDataResourceName != null) {
        const behavior = "icu";
        hasIcuData = true;
        assets.push({
            name: icuDataResourceName,
            resolvedUrl: appendUniqueQuery(loaderHelpers.locateFile(icuDataResourceName), behavior),
            hash: nativeResources.icu[icuDataResourceName],
            behavior,
            loadRemote: true
        });
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

function getICUResourceName(loadedConfig: MonoConfig, config: MonoConfig, culture: string | undefined): string | null {
    if (!loadedConfig.resources?.native?.icu) {
        config.globalizationMode = GlobalizationMode.Invariant;
        return null;
    }

    const icuFiles = Object.keys(loadedConfig.resources.native.icu);

    if (loadedConfig.globalizationMode === GlobalizationMode.Custom) {
        if (icuFiles.length === 1) {
            config.globalizationMode = GlobalizationMode.Custom;
            const customIcuFile = icuFiles[0];
            return customIcuFile;
        }
    }

    const icuFile = getICUResourceNameForCulture(loadedConfig, config, culture);
    if (icuFile && icuFiles.includes(icuFile)) {
        return icuFile;
    }

    config.globalizationMode = GlobalizationMode.Invariant;
    return null;
}

function getICUResourceNameForCulture(loadedConfig: MonoConfig, config: MonoConfig, culture: string | undefined): string | null {
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