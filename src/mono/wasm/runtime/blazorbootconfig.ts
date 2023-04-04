import { AssetEntry, MonoConfigInternal } from "./types";

export function mapBootConfigToMonoConfig(moduleConfig: MonoConfigInternal, bootConfig: BootJsonData) {
    const resources = bootConfig.resources;

    const assets: AssetEntry[] = [];
    const environmentVariables: any = {};

    moduleConfig.assets = assets;
    moduleConfig.globalizationMode = "icu";
    moduleConfig.environmentVariables = environmentVariables;
    moduleConfig.debugLevel = hasDebuggingEnabled(bootConfig) ? 1 : 0;
    moduleConfig.maxParallelDownloads = 1000000; // disable throttling parallel downloads
    moduleConfig.enableDownloadRetry = false; // disable retry downloads
    moduleConfig.mainAssemblyName = bootConfig.entryAssembly;

    const behaviorByName = (name: string) => {
        return name === "dotnet.timezones.blat" ? "vfs"
            : name === "dotnet.wasm" ? "dotnetwasm"
                : (name.startsWith("dotnet.worker") && name.endsWith(".js")) ? "js-module-threads"
                    : (name.startsWith("dotnet") && name.endsWith(".js")) ? "js-module-dotnet"
                        : name.startsWith("icudt") ? "icu"
                            : "other";
    };

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
    if (hasDebuggingEnabled(bootConfig) && resources.pdb) {
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
    const applicationCulture = /*TODO MF: resourceLoader.startOptions.applicationCulture ||*/ (navigator.languages && navigator.languages[0]);
    const icuDataResourceName = getICUResourceName(bootConfig, applicationCulture);
    let hasIcuData = false;
    for (const name in resources.runtime) {
        const behavior = behaviorByName(name) as any;
        if (behavior === "icu") {
            if (bootConfig.icuDataMode === ICUDataMode.Invariant) {
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

    if (bootConfig.modifiableAssemblies) {
        // Configure the app to enable hot reload in Development.
        environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = bootConfig.modifiableAssemblies;
    }

    if (bootConfig.icuDataMode === ICUDataMode.Sharded) {
        environmentVariables["__BLAZOR_SHARDED_ICU"] = "1";
    }

    /*TODO MF: if (resourceLoader.startOptions.applicationCulture) {
        // If a culture is specified via start options use that to initialize the Emscripten \  .NET culture.
        environmentVariables["LANG"] = `${resourceLoader.startOptions.applicationCulture}.UTF-8`;
    }*/

    if (bootConfig.aspnetCoreBrowserTools) {
        // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
        environmentVariables["__ASPNETCORE_BROWSER_TOOLS"] = bootConfig.aspnetCoreBrowserTools;
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

export function hasDebuggingEnabled(bootConfig: BootJsonData): boolean {
    // TODO MF: Copied from MonoDebugger.ts/attachDebuggerHotkey

    const hasReferencedPdbs = !!bootConfig.resources.pdb;
    const debugBuild = bootConfig.debugBuild;

    const navigatorUA = navigator as MonoNavigatorUserAgent;
    const brands = navigatorUA.userAgentData && navigatorUA.userAgentData.brands;
    const currentBrowserIsChromeOrEdge = brands
        ? brands.some(b => b.brand === "Google Chrome" || b.brand === "Microsoft Edge" || b.brand === "Chromium")
        : (window as any).chrome;

    return (hasReferencedPdbs || debugBuild) && (currentBrowserIsChromeOrEdge || navigator.userAgent.includes("Firefox"));
}

// can be removed once userAgentData is part of lib.dom.d.ts
declare interface MonoNavigatorUserAgent extends Navigator {
    readonly userAgentData: MonoUserAgentData;
}

declare interface MonoUserAgentData {
    readonly brands: ReadonlyArray<MonoUserAgentDataBrandVersion>;
    readonly platform: string;
}

declare interface MonoUserAgentDataBrandVersion {
    brand?: string;
    version?: string;
}

// Keep in sync with bootJsonData from the BlazorWebAssemblySDK
export interface BootJsonData {
    readonly entryAssembly: string;
    readonly resources: ResourceGroups;
    /** Gets a value that determines if this boot config was produced from a non-published build (i.e. dotnet build or dotnet run) */
    readonly debugBuild: boolean;
    readonly linkerEnabled: boolean;
    readonly cacheBootResources: boolean;
    readonly config: string[];
    readonly icuDataMode: ICUDataMode;
    readonly startupMemoryCache: boolean | null;
    readonly runtimeOptions: string[] | null;
  
    // These properties are tacked on, and not found in the boot.json file
    modifiableAssemblies: string | null;
    aspnetCoreBrowserTools: string | null;
}
  
export type BootJsonDataExtension = { [extensionName: string]: ResourceList };

export interface ResourceGroups {
    readonly assembly: ResourceList;
    readonly lazyAssembly: ResourceList;
    readonly pdb?: ResourceList;
    readonly runtime: ResourceList;
    readonly satelliteResources?: { [cultureName: string]: ResourceList };
    readonly libraryInitializers?: ResourceList,
    readonly extensions?: BootJsonDataExtension
    readonly runtimeAssets: ExtendedResourceList;
}
  
export type ResourceList = { [name: string]: string };
export type ExtendedResourceList = {
    [name: string]: {
      hash: string,
      behavior: string
    }
};
  
export enum ICUDataMode {
    Sharded,
    All,
    Invariant
}
  
// This type doesn't have to align with anything in BootConfig.
// Instead, this represents the public API through which certain aspects
// of boot resource loading can be customized.
export type WebAssemblyBootResourceType = "assembly" | "pdb" | "dotnetjs" | "dotnetwasm" | "globalization" | "manifest" | "configuration";