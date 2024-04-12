// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

try {
    const dotnetRuntime = await dotnet
        .withElementOnExit()
        .withExitCodeLogging()
        .withExitOnUnhandledError()
        .create();
    const config = dotnetRuntime.getConfig();
    await dotnetRuntime.runMainAndExit(config.mainAssemblyName, [window.location.origin, window.location.href]);

}
catch (err) {
    exit(2, err);
}