// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { INTERNAL, loaderHelpers, runtimeHelpers } from "./globals";
import type { WebAssemblyResourceLoader } from "./loader/blazor/WebAssemblyResourceLoader";
import { LoadingResource } from "./types";

export async function loadSatelliteAssemblies(culturesToLoad: string[]): Promise<void> {
    const resourceLoader: WebAssemblyResourceLoader = INTERNAL.resourceLoader;
    const satelliteResources = resourceLoader.bootConfig.resources.satelliteResources;
    if (!satelliteResources) {
        return;
    }

    await Promise.all(culturesToLoad!
        .filter(culture => Object.prototype.hasOwnProperty.call(satelliteResources, culture))
        .map(culture => resourceLoader.loadResources(satelliteResources[culture], fileName => loaderHelpers.locateFile(fileName), "assembly"))
        .reduce((previous, next) => previous.concat(next), new Array<LoadingResource>())
        .map(async resource => {
            const response = await resource.response;
            const bytes = await response.arrayBuffer();
            runtimeHelpers.javaScriptExports.load_satellite_assembly(new Uint8Array(bytes));
        }));
}