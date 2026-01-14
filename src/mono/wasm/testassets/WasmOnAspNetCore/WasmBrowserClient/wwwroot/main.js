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
    var url = window.location.origin + window.location.pathname;
    await dotnetRuntime.runMainAndExit(config.mainAssemblyName, [url, window.location.href]);

}
catch (err) {
    exit(2, err);
}