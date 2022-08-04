// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

import createDotnetRuntime from './dotnet.js'

try {
    const runtime = await createDotnetRuntime(({ INTERNAL }) => ({
        configSrc: "./mono-config.json",
        onConfigLoaded: (config) => {
            config.environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug";
            /* For custom logging patch the functions below
            config.diagnosticTracing = true;
            config.environmentVariables["MONO_LOG_LEVEL"] = "debug";
            config.environmentVariables["MONO_LOG_MASK"] = "all";
            INTERNAL.logging = {
                trace: (domain, log_level, message, isFatal, dataPtr) => console.log({ domain, log_level, message, isFatal, dataPtr }),
                debugger: (level, message) => console.log({ level, message }),
            };
            */
        },
    }));
    App.runtime = runtime;
    App.init()
} catch (err) {
    console.log(`WASM ERROR ${err}`);
}
