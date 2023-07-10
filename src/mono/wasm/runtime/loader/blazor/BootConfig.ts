// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { BootJsonData } from "../../types/blazor";
import type { WebAssemblyBootResourceType } from "../../types";
import { loaderHelpers } from "../globals";

export type LoadBootResourceCallback = (type: WebAssemblyBootResourceType, name: string, defaultUri: string, integrity: string) => string | Promise<Response> | null | undefined;

export class BootConfigResult {
    private constructor(public bootConfig: BootJsonData, public applicationEnvironment: string) {
    }

    static fromFetchResponse(bootConfigResponse: Response, bootConfig: BootJsonData, environment: string | undefined): BootConfigResult {
        const applicationEnvironment = environment || (loaderHelpers.getApplicationEnvironment && loaderHelpers.getApplicationEnvironment(bootConfigResponse)) || "Production";
        bootConfig.modifiableAssemblies = bootConfigResponse.headers.get("DOTNET-MODIFIABLE-ASSEMBLIES");
        bootConfig.aspnetCoreBrowserTools = bootConfigResponse.headers.get("ASPNETCORE-BROWSER-TOOLS");

        return new BootConfigResult(bootConfig, applicationEnvironment);
    }

    static async initAsync(loadBootResource?: LoadBootResourceCallback, environment?: string): Promise<BootConfigResult> {
        const defaultBootJsonLocation = "_framework/blazor.boot.json";

        const loaderResponse = loadBootResource !== undefined ?
            loadBootResource("manifest", "blazor.boot.json", defaultBootJsonLocation, "") :
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
        return BootConfigResult.fromFetchResponse(bootConfigResponse, bootConfig, environment);

        function defaultLoadBlazorBootJson(url: string): Promise<Response> {
            return fetch(url, {
                method: "GET",
                credentials: "include",
                cache: "no-cache",
            });
        }
    }
}

