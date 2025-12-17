// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

function displayMeaning(meaning) {
    console.log(`Meaning of life is ${meaning}`);
    document.getElementById("out").innerHTML = `${meaning}`;
}

function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

try {
    const { setModuleImports, getAssemblyExports, runMain } = await dotnet
        .withConfig({ appendElementOnExit: true, exitOnUnhandledError: true, forwardConsoleLogsToWS: true, logExitCode: true })
        .create();

    setModuleImports("main.js", {
        Sample: {
            Test: {
                displayMeaning
            }
        }
    });

    await runMain();

    const exports = await getAssemblyExports("Wasm.Browser.Sample");
    await exports.Sample.Test.PrintMeaning(delay(2000).then(() => 42));
    console.log("Program has exited normally.");
    exit(0);
}
catch (err) {
    exit(2, err);
}