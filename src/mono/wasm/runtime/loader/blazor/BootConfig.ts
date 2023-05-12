// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { BootJsonData } from "../../types/blazor";
import type { WebAssemblyBootResourceType } from "../../types";
import { loaderHelpers } from "../globals";

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

        const applicationEnvironment = environment || (loaderHelpers.getApplicationEnvironment && loaderHelpers.getApplicationEnvironment(bootConfigResponse)) || "Production";
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

