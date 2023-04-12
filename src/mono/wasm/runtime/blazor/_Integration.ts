import { Module } from "../imports";
import { AssetEntry, LoadingResource, MonoConfigInternal } from "../types";
import { BootConfigResult, BootJsonData, ICUDataMode } from "./BootConfig";
import { WebAssemblyConfigLoader } from "./WebAssemblyConfigLoader";
import { WebAssemblyResourceLoader } from "./WebAssemblyResourceLoader";
import { WebAssemblyBootResourceType } from "./WebAssemblyStartOptions";
import { hasDebuggingEnabled } from "./_Polyfill";

export async function loadBootConfig(config: MonoConfigInternal,) {
    // TODO MF: Init WebAssemblyResourceLoader

    const candidateOptions = config.startupOptions ?? {};
    const environment = candidateOptions.environment;
    const bootConfigPromise = BootConfigResult.initAsync(candidateOptions.loadBootResource, environment);

    // TODO MF: Hook WebAssemblyComponentAttacher

    const bootConfigResult: BootConfigResult = await bootConfigPromise;

    // TODO MF: Hook fetchAndInvokeInitializers

    const [resourceLoader] = await Promise.all([
        WebAssemblyResourceLoader.initAsync(bootConfigResult.bootConfig, candidateOptions || {}),
        WebAssemblyConfigLoader.initAsync(bootConfigResult, candidateOptions || {}),
    ]);

    const monoToBlazorAssetTypeMap: { [key: string]: WebAssemblyBootResourceType | undefined } = {
        "assembly": "assembly",
        "pdb": "pdb",
        "icu": "globalization",
        "vfs": "globalization",
        "dotnetwasm": "dotnetwasm",
    };

    Module.downloadResource = (asset: AssetEntry): LoadingResource | undefined => {
        // GOTCHA: the mapping to blazor asset type may not cover all mono owned asset types in the future in which case:
        // A) we may need to add such asset types to the mapping and to WebAssemblyBootResourceType
        // B) or we could add generic "runtime" type to WebAssemblyBootResourceType as fallback
        // C) or we could return `undefined` and let the runtime to load the asset. In which case the progress will not be reported on it and blazor will not be able to cache it.
        const type = monoToBlazorAssetTypeMap[asset.behavior];
        if (type !== undefined) {
            const res = resourceLoader.loadResource(asset.name, asset.resolvedUrl!, asset.hash!, type);
            asset.pendingDownload = res;

            // TODO MF: Hook setProgress
            return res;
        }
        return undefined;
    };

    mapBootConfigToMonoConfig(Module.config as MonoConfigInternal, resourceLoader);

    // TODO MF: Publish resourceLoader
}

export function mapBootConfigToMonoConfig(moduleConfig: MonoConfigInternal, resourceLoader: WebAssemblyResourceLoader) {
    const resources = resourceLoader.bootConfig.resources;

    const assets: AssetEntry[] = [];
    const environmentVariables: any = {};

    moduleConfig.assets = assets;
    moduleConfig.globalizationMode = "icu";
    moduleConfig.environmentVariables = environmentVariables;
    moduleConfig.debugLevel = hasDebuggingEnabled(resourceLoader.bootConfig) ? 1 : 0;
    moduleConfig.maxParallelDownloads = 1000000; // disable throttling parallel downloads
    moduleConfig.enableDownloadRetry = false; // disable retry downloads
    moduleConfig.mainAssemblyName = resourceLoader.bootConfig.entryAssembly;

    const monoToBlazorAssetTypeMap: { [key: string]: WebAssemblyBootResourceType | undefined } = {
        "assembly": "assembly",
        "pdb": "pdb",
        "icu": "globalization",
        "vfs": "globalization",
        "dotnetwasm": "dotnetwasm",
    };

    const behaviorByName = (name: string) => {
        return name === "dotnet.timezones.blat" ? "vfs"
            : name === "dotnet.wasm" ? "dotnetwasm"
                : (name.startsWith("dotnet.worker") && name.endsWith(".js")) ? "js-module-threads"
                    : (name.startsWith("dotnet") && name.endsWith(".js")) ? "js-module-dotnet"
                        : name.startsWith("icudt") ? "icu"
                            : "other";
    };

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

            // TODO MF: Hook setProgress

            return res;
        }
        return undefined;
    };

    Module.downloadResource = downloadResource;
    Module.disableDotnet6Compatibility = false;

    // any runtime owned assets, with proper behavior already set
    for (const name in resources.runtimeAssets) {
        const asset = resources.runtimeAssets[name] as AssetEntry;
        asset.name = name;
        asset.resolvedUrl = `_framework/${name}`;
        assets.push(asset);
        if (asset.behavior === "dotnetwasm") {
            downloadResource(asset);
        }
    }
    for (const name in resources.assembly) {
        const asset: AssetEntry = {
            name,
            resolvedUrl: `_framework/${name}`,
            hash: resources.assembly[name],
            behavior: "assembly",
        };
        assets.push(asset);
        downloadResource(asset);
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
            downloadResource(asset);
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
        const asset: AssetEntry = {
            name,
            resolvedUrl: `_framework/${name}`,
            hash: resources.runtime[name],
            behavior,
        };
        assets.push(asset);
    }

    if (!hasIcuData) {
        moduleConfig.globalizationMode = "invariant";
    }

    if (resourceLoader.bootConfig.modifiableAssemblies) {
        // Configure the app to enable hot reload in Development.
        environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = resourceLoader.bootConfig.modifiableAssemblies;
    }

    if (resourceLoader.bootConfig.icuDataMode === ICUDataMode.Sharded) {
        environmentVariables["__BLAZOR_SHARDED_ICU"] = "1";
    }

    if (resourceLoader.startOptions.applicationCulture) {
        // If a culture is specified via start options use that to initialize the Emscripten \  .NET culture.
        environmentVariables["LANG"] = `${resourceLoader.startOptions.applicationCulture}.UTF-8`;
    }

    if (resourceLoader.bootConfig.aspnetCoreBrowserTools) {
        // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
        environmentVariables["__ASPNETCORE_BROWSER_TOOLS"] = resourceLoader.bootConfig.aspnetCoreBrowserTools;
    }
}

function getICUResourceName(bootConfig: BootJsonData, culture: string | undefined): string {
    const combinedICUResourceName = "icudt.dat";
    if (!culture || bootConfig.icuDataMode === ICUDataMode.All) {
        return combinedICUResourceName;
    }

    const prefix = culture.split("-")[0];
    if ([
        "en",
        "fr",
        "it",
        "de",
        "es",
    ].includes(prefix)) {
        return "icudt_EFIGS.dat";
    } else if ([
        "zh",
        "ko",
        "ja",
    ].includes(prefix)) {
        return "icudt_CJK.dat";
    } else {
        return "icudt_no_CJK.dat";
    }
}