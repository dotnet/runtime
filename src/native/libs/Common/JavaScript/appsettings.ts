// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

type AppSettingsConfig = {
    appsettings?: string[];
    applicationEnvironment?: string;
};

export type AppSettingsFileSystem = {
    readFile(path: string): Uint8Array;
};

export function getAppSettingsFiles(config: AppSettingsConfig): string[] {
    const files = config.appsettings?.map(url => url.substring(url.lastIndexOf("/") + 1));
    return ["appsettings.json", `appsettings.${config.applicationEnvironment}.json`]
        .filter(file => files?.includes(file));
}

export function getAppSettingsBytes(fs: AppSettingsFileSystem, fileName: string): Uint8Array | undefined {
    try {
        return fs.readFile(fileName);
    } catch (error) {
        const errnoNoEntry = 44; // ENOENT in Emscripten's errno ABI.
        if (typeof error === "object" && error !== null && "errno" in error && error.errno === errnoNoEntry) {
            return undefined;
        }
        throw error;
    }
}
