// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

function getQueryParam(prameterName) {
    const params = new URLSearchParams(location.search);
    const paramValue = params.get(prameterName);
    if (!paramValue)
        throw new Error(`Missing query parameter '${prameterName}' in query: ${params}`);
    return paramValue;
}

function getUrl() {
    return window.location.origin;
}

try {
    const dotnetRuntime = await dotnet
        .withElementOnExit()
        .withExitCodeLogging()
        .withExitOnUnhandledError()
        .create();

        dotnetRuntime.setModuleImports("main.js", {
            Program: {
                getQueryParam,
                getUrl
            }
    });
    const config = dotnetRuntime.getConfig();

    await dotnetRuntime.runMainAndExit(config.mainAssemblyName);

}
catch (err) {
    exit(2, err);
}