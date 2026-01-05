// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-undef */
/* eslint-disable space-before-function-paren */
/* eslint-disable @typescript-eslint/no-unused-vars */
var fetch = fetch || undefined; var dotnetNativeModuleLoaded = false; var dotnetInternals = null;
export function selfRun() {
    const Module = {};
    const corePreRun = () => {
        // copy all node/shell env variables to emscripten env
        if (globalThis.process && globalThis.process.env) {
            for (const [key, value] of Object.entries(process.env)) {
                Module.ENV[key] = value;
            }
        }

        Module.ENV["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "true";
    };
    Module.preRun = [corePreRun];

    const runtimeApi = {
        Module,
        INTERNAL: {},
        runtimeId: 0,
        runtimeBuildInfo: {
            productVersion: "corerun",
            gitHash: null,
            buildConfiguration: "corerun",
            wasmEnableThreads: false,
            wasmEnableSIMD: true,
            wasmEnableExceptionHandling: true,
        },
    };
    dotnetInternals = [
        runtimeApi,
        [],
    ];

    createDotnetRuntime(runtimeApi.Module);
}
selfRun();
