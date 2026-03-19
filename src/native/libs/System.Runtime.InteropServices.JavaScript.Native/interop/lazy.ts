// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetLoaderExports } from "./cross-module";

export async function loadSatelliteAssemblies(culturesToLoad: string[]): Promise<void> {
    await dotnetLoaderExports.fetchSatelliteAssemblies(culturesToLoad);
}

export async function loadLazyAssembly(assemblyNameToLoad: string): Promise<boolean> {
    return dotnetLoaderExports.fetchLazyAssembly(assemblyNameToLoad);
}
