// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

const { getAssemblyExports, getConfig, INTERNAL } = await dotnet
    .withElementOnExit()
    .withExitCodeLogging()
    .withExitOnUnhandledError()
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

try {
    const params = new URLSearchParams(location.search);
    const testCase = params.get("test");
    if (testCase == null) {
        exit(2, new Error("Missing test scenario. Supply query argument 'test'."));
    }

    switch (testCase) {
        case "SatelliteAssembliesTest":
            await exports.SatelliteAssembliesTest.Run();
            exit(0);
            break;
        case "LazyLoadingTest":
            await INTERNAL.loadLazyAssembly("System.Text.Json.wasm");
            exports.LazyLoadingTest.Run();
            exit(0);
            break;
        case "LibraryInitializerTest":
            exit(0);
            break;
    }
} catch (e) {
    exit(1, e);
}
