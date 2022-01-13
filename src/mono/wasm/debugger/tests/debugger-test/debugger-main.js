// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

import createDotnetRuntime from './dotnet.js'

try {
    const { BINDING, INTERNAL } = await createDotnetRuntime(({ MONO }) => ({
        configSrc: "./mono-config.json",
        onConfigLoaded: () => {
            MONO.config.environment_variables["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug";
            /* For custom logging patch the functions below
            MONO.config.environment_variables["MONO_LOG_LEVEL"] = "debug";
            MONO.config.environment_variables["MONO_LOG_MASK"] = "all";
            INTERNAL.logging = {
                trace: function (domain, log_level, message, isFatal, dataPtr) { },
                debugger: function (level, message) { }
            };
            */
        },
    }));
    App.init({ BINDING, INTERNAL })
} catch (err) {
    console.log(`WASM ERROR ${err}`);
}
