// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from "./dotnet.js";
import { config } from "./dotnet.boot.js";
/* eslint-disable no-console */

try {
    await dotnet
        .withConfig(config)
        .run();
} catch (err) {
    console.error(err);
    exit(2, err);
}
