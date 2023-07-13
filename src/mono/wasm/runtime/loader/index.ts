// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetHostBuilder } from "../types";
import { mono_exit } from "./exit";
import { sanityCheck } from "./polyfills";
import { HostBuilder, createEmscripten } from "./run";

// export external API
const dotnet: DotnetHostBuilder = new HostBuilder();
const exit = mono_exit;
const legacyEntrypoint = createEmscripten;

sanityCheck();

export { dotnet, exit };
export default legacyEntrypoint;
