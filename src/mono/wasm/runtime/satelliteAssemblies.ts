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
        .map(culture => {
            const promises: LoadingResource[] = [];
            loaderHelpers.enumerateResources(satelliteResources[culture], (name, hash) => {
                const asset = loaderHelpers.ensureAssetResolvedUrl({
                    name,
                    hash,
                    behavior: "resource",
                    culture
                });

                promises.push(loaderHelpers.loadResource(asset.name, asset.resolvedUrl!, asset.hash ?? "", asset.behavior));
            });
            return promises;
        })
        .reduce((previous, next) => previous.concat(next), new Array<LoadingResource>())
        .map(async resource => {
            const response = await resource.response;
            const bytes = await response.arrayBuffer();
            runtimeHelpers.javaScriptExports.load_satellite_assembly(new Uint8Array(bytes));
        }));
}