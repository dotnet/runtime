// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "../globals";
import { WebAssemblyBootResourceType } from "../types-api";

type LoadBootResourceCallback = (type: WebAssemblyBootResourceType, name: string, defaultUri: string, integrity: string) => string | Promise<Response> | null | undefined;

export class BootConfigResult {
    private constructor(public bootConfig: BootJsonData, public applicationEnvironment: string) {
    }

    static async initAsync(loadBootResource?: LoadBootResourceCallback, environment?: string): Promise<BootConfigResult> {
        const loaderResponse = loadBootResource !== undefined ?
            loadBootResource("manifest", "blazor.boot.json", "_framework/blazor.boot.json", "") :
            defaultLoadBlazorBootJson("_framework/blazor.boot.json");

        let bootConfigResponse: Response;

        if (!loaderResponse) {
            bootConfigResponse = await defaultLoadBlazorBootJson("_framework/blazor.boot.json");
        } else if (typeof loaderResponse === "string") {
            bootConfigResponse = await defaultLoadBlazorBootJson(loaderResponse);
        } else {
            bootConfigResponse = await loaderResponse;
        }

        const applicationEnvironment = environment || (Module.getApplicationEnvironment && Module.getApplicationEnvironment(bootConfigResponse)) || "Production";
        const bootConfig: BootJsonData = await bootConfigResponse.json();
        bootConfig.modifiableAssemblies = bootConfigResponse.headers.get("DOTNET-MODIFIABLE-ASSEMBLIES");
        bootConfig.aspnetCoreBrowserTools = bootConfigResponse.headers.get("ASPNETCORE-BROWSER-TOOLS");

        return new BootConfigResult(bootConfig, applicationEnvironment);

        function defaultLoadBlazorBootJson(url: string): Promise<Response> {
            return fetch(url, {
                method: "GET",
                credentials: "include",
                cache: "no-cache",
            });
        }
    }
}

// Keep in sync with Microsoft.NET.Sdk.WebAssembly.BootJsonData from the WasmSDK
export interface BootJsonData {
    readonly entryAssembly: string;
    readonly resources: ResourceGroups;
    /** Gets a value that determines if this boot config was produced from a non-published build (i.e. dotnet build or dotnet run) */
    readonly debugBuild: boolean;
    readonly linkerEnabled: boolean;
    readonly cacheBootResources: boolean;
    readonly config: string[];
    readonly icuDataMode: ICUDataMode;
    readonly startupMemoryCache: boolean | undefined;
    readonly runtimeOptions: string[] | undefined;

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
    Invariant,
    Custom
}
