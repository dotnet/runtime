// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **JavaScript module** that would become of `dotnet.js`.
 * It implements host for the browser together with `src/native/corehost/browserhost`.
 * It exposes the public JS runtime APIs that is implemented in `dotnet.runtime.ts`.
 * It's good to keep this file small.
 */

import type { DotnetHostBuilder, DotnetModuleConfig, RuntimeAPI } from "./types";

import { Module, dotnetApi } from "./cross-module";

import { HostBuilder } from "./host-builder";
import { initPolyfillsEarly } from "./polyfills";
import { exit } from "./exit";
import { dotnetInitializeModule } from ".";

dotnetInitializeModule();
await initPolyfillsEarly();

export const dotnet: DotnetHostBuilder = new HostBuilder() as DotnetHostBuilder;
export { exit };

dotnet.withConfig(/*! dotnetBootConfig */{});
const legacyExport = async (moduleFactory: DotnetModuleConfig | ((api: RuntimeAPI) => DotnetModuleConfig)): Promise<RuntimeAPI> => {
    let cfg: DotnetModuleConfig = moduleFactory as any;
    if (typeof moduleFactory === "function") {
        cfg = moduleFactory(dotnetApi);
    }
    Object.assign(Module, cfg);
    return dotnet.create();
};
export default legacyExport;
