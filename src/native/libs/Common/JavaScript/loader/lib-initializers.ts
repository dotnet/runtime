// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderConfig } from "./config";
import { dotnetLogger } from "./cross-module";
import { exit } from "./exit";

export async function invokeLibraryInitializers(functionName: string, args: any[]): Promise<void> {
    const allModules = [
        ...loaderConfig.resources?.modulesAfterConfigLoaded ?? [],
        ...loaderConfig.resources?.modulesAfterRuntimeReady ?? [],
    ];

    const promises: Promise<void>[] = [];
    for (const asset of allModules) {
        promises.push(invokeWithErrorHandling(asset, functionName, args));
    }
    await Promise.all(promises);
}

async function invokeWithErrorHandling(
    asset: { moduleExports?: any; name?: string; resolvedUrl?: string },
    functionName: string, args: any[]
): Promise<void> {
    try {
        const mod = await asset.moduleExports;
        if (mod) {
            asset.moduleExports = mod;
        }
        if (mod && typeof mod[functionName] === "function") {
            await mod[functionName](...args);
        }
    } catch (err) {
        const name = asset.name || asset.resolvedUrl || "unknown";
        const message = err instanceof Error ? err.message : String(err);
        dotnetLogger.warn(
            `Failed to invoke '${functionName}' on library initializer '${name}': ${message}`
        );
        exit(1, err);
        throw err;
    }
}
