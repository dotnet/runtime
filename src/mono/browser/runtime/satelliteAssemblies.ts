// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderHelpers } from "./globals";
import { load_satellite_assembly } from "./managed-exports";
import type { AssemblyAsset } from "./types";
import type { AssetEntryInternal } from "./types/internal";

export async function loadSatelliteAssemblies (culturesToLoad: string[]): Promise<void> {
    const satelliteResources = loaderHelpers.config.resources!.satelliteResources;
    if (!satelliteResources) {
        return;
    }

    await Promise.all(culturesToLoad!
        .filter(culture => Object.prototype.hasOwnProperty.call(satelliteResources, culture))
        .map(culture => {
            const promises: Promise<ArrayBuffer>[] = [];
            for (let i = 0; i < satelliteResources[culture].length; i++) {
                const asset = satelliteResources[culture][i] as AssemblyAsset & AssetEntryInternal;
                asset.behavior = "resource";
                asset.culture = culture;
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
