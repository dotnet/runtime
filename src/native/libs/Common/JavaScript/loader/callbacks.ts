// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfig } from "./types";

export const loaderCallbacks: {
    configLoaded?: (config: LoaderConfig) => void | Promise<void>;
    dotnetReady?: () => void | Promise<void>;
    downloadResourceProgress?: (resourcesLoaded: number, totalResources: number) => void;
} = {};

type LegacyModuleCallbacks = {
    onConfigLoaded?: (config: LoaderConfig) => void | Promise<void>;
    onDotnetReady?: () => void | Promise<void>;
    onDownloadResourceProgress?: (resourcesLoaded: number, totalResources: number) => void;
};

/**
 * Moves the legacy `Module` callbacks into the loader state, so that they are never stored on the Emscripten module.
 * Returns a copy of the configuration without those callbacks, which can be merged into the Emscripten module.
 */
export function extractLegacyModuleCallbacks<T extends object>(moduleConfig: T): T {
    const legacy = moduleConfig as LegacyModuleCallbacks;
    if (!legacy || (legacy.onConfigLoaded === undefined && legacy.onDotnetReady === undefined && legacy.onDownloadResourceProgress === undefined)) {
        return moduleConfig;
    }

    const { onConfigLoaded, onDotnetReady, onDownloadResourceProgress, ...rest } = moduleConfig as T & LegacyModuleCallbacks;
    if (onConfigLoaded !== undefined) {
        loaderCallbacks.configLoaded = onConfigLoaded;
    }
    if (onDotnetReady !== undefined) {
        loaderCallbacks.dotnetReady = onDotnetReady;
    }
    if (onDownloadResourceProgress !== undefined) {
        loaderCallbacks.downloadResourceProgress = onDownloadResourceProgress;
    }

    return rest as unknown as T;
}
