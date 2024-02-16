// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderHelpers } from "./globals";
import { load_satellite_assembly } from "./managed-exports";
import { AssetEntry } from "./types";

export async function loadSatelliteAssemblies(culturesToLoad: string[]): Promise<void> {
    const satelliteResources = loaderHelpers.config.resources!.satelliteResources;
    if (!satelliteResources) {
        return;
    }

    await Promise.all(culturesToLoad!
        .filter(culture => Object.prototype.hasOwnProperty.call(satelliteResources, culture))
        .map(culture => {
            const promises: Promise<ArrayBuffer>[] = [];
            for (const name in satelliteResources[culture]) {
                const asset: AssetEntry = {
                    name,
                    hash: satelliteResources[culture][name],
                    behavior: "resource",
                    culture
                };

                promises.push(loaderHelpers.retrieve_asset_download(asset));
            }

            return promises;
        })
        .reduce((previous, next) => previous.concat(next), new Array<Promise<ArrayBuffer>>())
        .map(async bytesPromise => {
            const bytes = await bytesPromise;
            load_satellite_assembly(new Uint8Array(bytes));
        }));
}