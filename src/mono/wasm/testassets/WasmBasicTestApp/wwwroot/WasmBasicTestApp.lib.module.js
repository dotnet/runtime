// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const params = new URLSearchParams(location.search);

export function onRuntimeConfigLoaded(config) {
    config.environmentVariables["LIBRARY_INITIALIZER_TEST"] = "1";

    if (params.get("throwError") === "true") {
        throw new Error("Error thrown from library initializer");
    }
}

export async function onRuntimeReady({ getAssemblyExports, getConfig }) {
    const testCase = params.get("test");
    if (testCase == "LibraryInitializerTest") {
        const config = getConfig();
        const exports = await getAssemblyExports(config.mainAssemblyName);

        exports.LibraryInitializerTest.Run();
    }
}