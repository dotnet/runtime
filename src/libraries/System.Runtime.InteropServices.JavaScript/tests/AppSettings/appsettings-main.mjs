// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from "./_framework/dotnet.js";

dotnet.withApplicationEnvironment("AppSettingsTests");

globalThis.appSettingsTests = {
    hasSharedBytes(fileName) {
        const getBytes = globalThis.App.runtime.INTERNAL.getAppSettingsBytes;
        const bytes = getBytes(fileName);
        if (bytes === undefined) throw new Error(`Missing settings: ${fileName}`);
        return bytes === getBytes(fileName);
    },
    mutationChangesStoredBytes(fileName) {
        const getBytes = globalThis.App.runtime.INTERNAL.getAppSettingsBytes;
        const bytes = getBytes(fileName);
        if (bytes === undefined || bytes.length === 0) throw new Error(`Missing or empty settings: ${fileName}`);
        const originalByte = bytes[0];
        try {
            bytes[0] ^= 0xff;
            return getBytes(fileName)[0] !== originalByte;
        } finally {
            bytes[0] = originalByte;
        }
    },
    hasIndependentFileLists() {
        const getFiles = globalThis.App.runtime.INTERNAL.getAppSettingsFiles;
        const files = getFiles();
        const expected = [...files];
        files.length = 0;
        return JSON.stringify(getFiles()) === JSON.stringify(expected);
    },
    getFilesWithChangedConfig(variation) {
        const runtime = globalThis.App.runtime;
        const config = runtime.getConfig();
        const original = config.appsettings;
        try {
            switch (variation) {
                case "duplicates":
                    config.appsettings = [...original].reverse().concat(original);
                    break;
                case "withoutBase":
                    config.appsettings = original.filter(url => url.substring(url.lastIndexOf("/") + 1) !== "appsettings.json");
                    break;
                case "empty":
                    config.appsettings = [];
                    break;
                case "absent":
                    delete config.appsettings;
                    break;
                default:
                    throw new Error(`Unknown settings variation: ${variation}`);
            }
            return runtime.INTERNAL.getAppSettingsFiles();
        } finally {
            config.appsettings = original;
        }
    }
};

await import("./test-main.mjs");
