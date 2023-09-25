// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { verifyEnvironment } from "./polyfills";
import { dotnet, exit, legacyEntrypoint } from "./run";


verifyEnvironment();

export { dotnet, exit };
export default legacyEntrypoint;
