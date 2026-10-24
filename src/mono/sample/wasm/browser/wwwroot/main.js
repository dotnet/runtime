// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

function displayMeaning(meaning) {
    if (meaning === -1) {
        throw new Error("M2 import failure");
    }
    if (meaning === -2) {
        return null;
    }
    if (meaning === -3) {
        return undefined;
    }
    document.getElementById("out").textContent = `${meaning}`;
    return meaning + 1;
}

try {
    const { setModuleImports } = await dotnet
        .withConfig({ appendElementOnExit: true, exitOnUnhandledError: true, logExitCode: true })
        .withWorker()
        .withApplicationArgumentsFromQuery()
        .withDiagnosticTracing(true)
        .create();

    setModuleImports("main.js", {
        Sample: {
            Test: {
                displayMeaning
            }
        }
    });
    const exitCode = await dotnet.runMainAndExit();
    console.log(`Program has exited with code ${exitCode}.`);
    exit(exitCode);
}
catch (err) {
    if (!err || typeof err.status !== "number") {
        exit(2, err);
    }
}