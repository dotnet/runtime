// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

// Read test case from query string
const params = new URLSearchParams(location.search);
const testCase = params.get("test");
if (testCase == null) {
    exit(2, new Error("Missing test scenario. Supply query argument 'test'."));
}

// Prepare base runtime parameters
dotnet
    .withElementOnExit()
    .withExitCodeLogging()
    .withExitOnUnhandledError();

// Modify runtime start based on test case
switch (testCase) {
    case "AppSettingsTest":
        dotnet.withApplicationEnvironment(params.get("applicationEnvironment"));
        break;
}

const { getAssemblyExports, getConfig, INTERNAL } = await dotnet.create();
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

// Run the test case
try {
    switch (testCase) {
        case "SatelliteAssembliesTest":
            await exports.SatelliteAssembliesTest.Run();
            exit(0);
            break;
        case "LazyLoadingTest":
            if (params.get("loadRequiredAssembly") !== "false") {
                await INTERNAL.loadLazyAssembly("System.Text.Json.wasm");
            }
            exports.LazyLoadingTest.Run();
            exit(0);
            break;
        case "LibraryInitializerTest":
            exit(0);
            break;
        case "AppSettingsTest":
            exports.AppSettingsTest.Run();
            exit(0);
            break;
    }
} catch (e) {
    exit(1, e);
}
