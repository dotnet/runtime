// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

function displayMeaning(meaning) {
    document.getElementById("out").innerHTML = `${meaning}`;
}

function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

try {
    const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
        .withElementOnExit()
        .create();

    setModuleImports("main.js", {
        Sample: {
            Test: {
                displayMeaning,
                delay,
            }
        }
    });

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);

    const meaning = 42;
    const deepMeaning = new Promise(resolve => setTimeout(() => resolve(meaning), 100));
    exports.Sample.Test.PrintMeaning(deepMeaning);

    await dotnet.run();
}
catch (err) {
    exit(2, err);
}