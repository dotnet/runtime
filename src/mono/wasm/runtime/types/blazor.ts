// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Keep in sync with Microsoft.NET.Sdk.WebAssembly.BootJsonData from the WasmSDK
export interface BootJsonData {
    readonly entryAssembly: string;
    readonly resources: ResourceGroups;
    /** Gets a value that determines if this boot config was produced from a non-published build (i.e. dotnet build or dotnet run) */
    readonly debugBuild: boolean;
    readonly debugLevel: number;
    readonly linkerEnabled: boolean;
    readonly cacheBootResources: boolean;
    readonly config: string[];
    readonly icuDataMode: ICUDataMode;
    readonly startupMemoryCache: boolean | undefined;
    readonly runtimeOptions: string[] | undefined;
    readonly environmentVariables?: { [name: string]: string };
    readonly diagnosticTracing?: boolean;
    readonly pthreadPoolSize: number;

    // These properties are tacked on, and not found in the boot.json file
    modifiableAssemblies: string | null;
    aspnetCoreBrowserTools: string | null;

    readonly extensions?: { [name: string]: any };
}

export type BootJsonDataExtension = { [extensionName: string]: ResourceList };

export interface ResourceGroups {
    readonly hash?: string;
    readonly assembly: ResourceList;
    readonly lazyAssembly: ResourceList;
    readonly pdb?: ResourceList;
    readonly runtime: ResourceList;
    readonly satelliteResources?: { [cultureName: string]: ResourceList };
    readonly libraryInitializers?: ResourceList,
    readonly libraryStartupModules?: { onRuntimeConfigLoaded: ResourceList, onRuntimeReady: ResourceList },
    readonly extensions?: BootJsonDataExtension
    readonly runtimeAssets: ExtendedResourceList;
    readonly vfs?: { [virtualPath: string]: ResourceList };
}

export type ResourceList = { [name: string]: string };
export type ExtendedResourceList = {
    [name: string]: {
        hash: string,
        behavior: string
    }
};

export enum ICUDataMode {
    Sharded = 0,
    All = 1,
    Invariant = 2,
    Custom = 3,
    Hybrid = 4
}
