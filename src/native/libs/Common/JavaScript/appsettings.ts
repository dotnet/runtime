// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AppSettings, LoaderConfig } from "./types";

const appSettingsBytes = new Map<string, Uint8Array>();

export function getAppSettingsAssets(config: LoaderConfig): AppSettings[] {
    const assets = config.resources?.appsettings?.length
        ? config.resources.appsettings
        : (config.appsettings || []).map(name => ({
            name,
            virtualPath: name.substring(name.lastIndexOf("/") + 1),
            loadIntoVfs: true,
        }));
    return assets.filter(asset => asset.virtualPath === "appsettings.json"
        || asset.virtualPath === `appsettings.${config.applicationEnvironment}.json`);
}

export function storeAppSettingsBytes(fileName: string, bytes: Uint8Array): void {
    appSettingsBytes.set(fileName, bytes);
}

export function getAppSettingsFiles(): string[] {
    return [...appSettingsBytes.keys()].sort((a, b) => a === b ? 0 : a === "appsettings.json" ? -1 : b === "appsettings.json" ? 1 : a.localeCompare(b));
}

export function getAppSettingsBytes(fileName: string): Uint8Array | undefined {
    return appSettingsBytes.get(fileName);
}
