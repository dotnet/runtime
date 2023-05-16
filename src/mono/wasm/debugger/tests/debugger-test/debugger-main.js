// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

import { dotnet, exit } from './dotnet.js'

try {
    const runtime = await dotnet
        .withEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES", "debug")
        // For custom logging patch the functions below
        //.withDiagnosticTracing(true)
        //.withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
        //.withEnvironmentVariable("MONO_LOG_MASK", "all")
        .create();
    /*runtime.INTERNAL.logging = {
        trace: (domain, log_level, message, isFatal, dataPtr) => console.log({ domain, log_level, message, isFatal, dataPtr }),
        debugger: (level, message) => console.log({ level, message }),
    };*/
    App.runtime = runtime;
    await App.init();
}
catch (err) {
    exit(2, err);
}