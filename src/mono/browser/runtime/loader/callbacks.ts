// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { MonoConfig, RuntimeAPI } from "../types";

export const loaderCallbacks: {
    configLoaded?: (config: MonoConfig, api: RuntimeAPI) => void | Promise<void>;
    dotnetReady?: () => void | Promise<void>;
    downloadResourceProgress?: (resourcesLoaded: number, totalResources: number) => void;
} = {};
