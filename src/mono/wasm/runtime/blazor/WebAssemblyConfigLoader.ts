// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WebAssemblyStartOptions } from "../types-api";
import { BootJsonData } from "./BootConfig";

export type ConfigFile = {
    name: string,
    content: Uint8Array
}

export class WebAssemblyConfigLoader {
    static async initAsync(bootConfig: BootJsonData, applicationEnvironment: string, startOptions: Partial<WebAssemblyStartOptions>): Promise<Array<ConfigFile>> {
        const configFiles = await Promise.all((bootConfig.config || [])
            .filter(name => name === "appsettings.json" || name === `appsettings.${applicationEnvironment}.json`)
            .map(async name => ({ name, content: await getConfigBytes(name) })));

        async function getConfigBytes(file: string): Promise<Uint8Array> {
            // Allow developers to override how the config is loaded
            if (startOptions.loadBootResource) {
                const customLoadResult = startOptions.loadBootResource("configuration", file, file, "");
                if (customLoadResult instanceof Promise) {
                    // They are supplying an entire custom response, so just use that
                    return new Uint8Array(await (await customLoadResult).arrayBuffer());
                } else if (typeof customLoadResult === "string") {
                    // They are supplying a custom URL, so use that with the default fetch behavior
                    file = customLoadResult;
                }
            }

            const response = await fetch(file, {
                method: "GET",
                credentials: "include",
                cache: "no-cache",
            });

            return new Uint8Array(await response.arrayBuffer());
        }

        return configFiles;
    }
}
