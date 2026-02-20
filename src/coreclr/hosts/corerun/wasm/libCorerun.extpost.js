// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-undef */
/* eslint-disable @typescript-eslint/no-unused-vars */
var fetch = fetch || undefined; var dotnetNativeModuleLoaded = false; var dotnetInternals = null;
export function selfRun() {
    const Module = {};
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
        exit: (exitCode, error) => {
            // do not propagate ExitStatus exception
            if (error && typeof error.status !== "number") {
                console.error(error);
            }
            process.exit(exitCode);
        },
    };
    dotnetInternals = [
        runtimeApi,
        [],
    ];

    // this will create the emscripten emulator and run corerun.cpp main()
    // but the nodejs process will be kept alive by any pending async work
    createDotnetRuntime(runtimeApi.Module);
}

selfRun();
