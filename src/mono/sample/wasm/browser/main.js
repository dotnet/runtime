// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

try {
    const { runMain } = await dotnet
        .withElementOnExit()
        .withDiagnosticTracing(true)
        .withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
        .withEnvironmentVariable("MONO_LOG_MASK", "all")
        .withExitOnUnhandledError()
        .create();

    await runMain();
}
catch (err) {
    exit(2, err);
}