// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **JavaScript module** that would become of `dotnet.js`.
 * It implements host for the browser together with `src/native/corehost/browserhost`.
 * It exposes the public JS runtime APIs that is implemented in `dotnet.runtime.ts`.
 * It's good to keep this file small.
 */

import type { DotnetHostBuilder } from "./types";

import { HostBuilder } from "./host-builder";
import { initPolyfills, initPolyfillsAsync } from "./polyfills";
import { exit } from "./exit";
import { dotnetInitializeModule } from ".";
import { selfHostNodeJS } from "./run";

initPolyfills();
dotnetInitializeModule();
await initPolyfillsAsync();

export const dotnet: DotnetHostBuilder | undefined = new HostBuilder() as DotnetHostBuilder;
export { exit };

dotnet.withConfig(/*! dotnetBootConfig */{});

// Auto-start when in Node.js or Shell environment
selfHostNodeJS(dotnet!).catch();
