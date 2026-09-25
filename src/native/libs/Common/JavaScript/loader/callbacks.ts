// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfig } from "./types";

export const loaderCallbacks: {
    configLoaded?: (config: LoaderConfig) => void | Promise<void>;
    dotnetReady?: () => void | Promise<void>;
    downloadResourceProgress?: (resourcesLoaded: number, totalResources: number) => void;
} = {};
