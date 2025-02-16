// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

function displayMeaning(meaning) {
    document.getElementById("out").innerHTML = `${meaning}`;
}

try {
    const { setModuleImports, runMain, getAssemblyExports, getConfig } = await dotnet
        //.withEnvironmentVariable("MONO_DIAGNOSTICS", "--diagnostic-mono-profiler=enable")// --diagnostic-ports=mock:../mock.js,suspend
        //.withEnvironmentVariable("DOTNET_DiagnosticPorts", "ws://127.0.0.1:8088/diagnostics,suspend")
        //.withEnvironmentVariable("DOTNET_DiagnosticPorts", "ws://127.0.0.1:8088/diagnostics")
        // dotnet-trace collect --providers Microsoft-Windows-DotNETRuntime:0x1980001:5 -p 41732
        // dotnet-gcdump collect -p 41732
        // dotnet-counters
        //.withEnvironmentVariable("DOTNET_DiagnosticPorts", "download:gcdump")
        //.withEnvironmentVariable("DOTNET_DiagnosticPorts", "download:counters")
        .withEnvironmentVariable("DOTNET_DiagnosticPorts", "download:samples")
        //.withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
        //.withEnvironmentVariable("MONO_LOG_MASK", "all")
        .withElementOnExit()
        .withExitOnUnhandledError()
        .create();

    setModuleImports("main.js", {
        Sample: {
            Test: {
                displayMeaning
            }
        }
    });
    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);
    const sayHi = exports.Sample.Test.SayHi;
    const sayHiAsync = exports.Sample.Test.SayHiAsync;

    document.querySelector("#hello-button").addEventListener("click", () => {
        try {
            sayHi();
        } catch (exc) {
            alert(exc);
        }
    });

    sayHi();
    sayHiAsync();

    await runMain();

    setInterval(async () => {
        sayHi();
        await sayHiAsync();
    }, 0);
}
catch (err) {
    exit(2, err);
}
