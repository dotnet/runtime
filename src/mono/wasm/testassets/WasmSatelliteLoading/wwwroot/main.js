// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

await dotnet
    .withElementOnExit()
    .withExitCodeLogging()
    .withExitOnUnhandledError()
    .create();

try {
    await dotnet.run();
    exit(0);
} catch (e) {
    exit(1, e);
}
