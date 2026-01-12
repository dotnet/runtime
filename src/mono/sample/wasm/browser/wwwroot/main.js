// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

function displayMeaning(meaning) {
    document.getElementById("out").innerHTML = `${meaning}`;
}

try {
    const { setModuleImports, getAssemblyExports, runMain } = await dotnet
        .withConfig({ appendElementOnExit: true, exitOnUnhandledError: true, forwardConsole: true, logExitCode: true })
        .create();

    setModuleImports("main.js", {
        Sample: {
            Test: {
                displayMeaning
            }
        }
    });

    await dotnet.run();
}
catch (err) {
    exit(2, err);
}