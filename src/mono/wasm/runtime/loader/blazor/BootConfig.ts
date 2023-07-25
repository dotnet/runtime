// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { BootJsonData } from "../../types/blazor";
import { loaderHelpers } from "../globals";

export class BootConfigResult {
    private constructor(public bootConfig: BootJsonData, public applicationEnvironment: string) {
    }

    static fromFetchResponse(bootConfigResponse: Response, bootConfig: BootJsonData, environment: string | undefined): BootConfigResult {
        const applicationEnvironment = environment
            || (loaderHelpers.getApplicationEnvironment && loaderHelpers.getApplicationEnvironment(bootConfigResponse))
            || bootConfigResponse.headers.get("Blazor-Environment")
            || bootConfigResponse.headers.get("DotNet-Environment")
            || "Production";

        bootConfig.modifiableAssemblies = bootConfigResponse.headers.get("DOTNET-MODIFIABLE-ASSEMBLIES");
        bootConfig.aspnetCoreBrowserTools = bootConfigResponse.headers.get("ASPNETCORE-BROWSER-TOOLS");

        return new BootConfigResult(bootConfig, applicationEnvironment);
    }
}

