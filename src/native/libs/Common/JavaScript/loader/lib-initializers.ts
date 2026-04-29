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
        const mod = asset.moduleExports;
        if (mod && typeof mod[functionName] === "function") {
            const name = asset.name || asset.resolvedUrl || "unknown";
            promises.push(
                invokeWithErrorHandling(name, functionName, () => mod[functionName](...args))
            );
        }
    }
    await Promise.all(promises);
}

async function invokeWithErrorHandling(
    scriptName: string, methodName: string, callback: () => Promise<void> | undefined
): Promise<void> {
    try {
        await callback();
    } catch (err) {
        dotnetLogger.warn(
            `Failed to invoke '${methodName}' on library initializer '${scriptName}': ${err}`
        );
        exit(1, err);
        throw err;
    }
}
