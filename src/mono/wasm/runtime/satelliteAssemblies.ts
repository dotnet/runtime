// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderHelpers, runtimeHelpers } from "./globals";
import { LoadingResource } from "./types";

export async function loadSatelliteAssemblies(culturesToLoad: string[]): Promise<void> {
    const satelliteResources = loaderHelpers.config.resources!.satelliteResources;
    if (!satelliteResources) {
        return;
    }

    await Promise.all(culturesToLoad!
        .filter(culture => Object.prototype.hasOwnProperty.call(satelliteResources, culture))
        .map(culture => loaderHelpers.loadResources(satelliteResources[culture], fileName => loaderHelpers.locateFile(fileName), "resource"))
        .reduce((previous, next) => previous.concat(next), new Array<LoadingResource>())
        .map(async resource => {
            const response = await resource.response;
            const bytes = await response.arrayBuffer();
            runtimeHelpers.javaScriptExports.load_satellite_assembly(new Uint8Array(bytes));
        }));
}