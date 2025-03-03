// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

let exports = undefined,
    setenv = undefined;

window.addEventListener("load", onLoad);

try {
    const { setModuleImports, getAssemblyExports, setEnvironmentVariable, getConfig } = await dotnet
        .withModuleConfig()
        .withExitOnUnhandledError()
        .withExitCodeLogging()
        .withElementOnExit()
        .withOnConfigLoaded(() => {
            // you can test abort of the startup by opening http://localhost:8000/?throwError=true
            const params = new URLSearchParams(location.search);
            if (params.get("throwError") === "true") {
                throw new Error("Error thrown from OnConfigLoaded");
            }
        })
        .create();

    setModuleImports("main.js", {
        timerTick: (i) => {
            document.querySelector("#timer-value").textContent = i;
        },
    });

    setenv = setEnvironmentVariable;
    const config = getConfig();
    exports = await getAssemblyExports(config.mainAssemblyName);
}
catch (err) {
    exit(2, err);
}

function onLoad() {
    document.querySelector("#throw-managed-exc").addEventListener("click", () => {
        try {
            exports.Sample.Test.ThrowManagedException();
            alert("No JS exception was thrown!");
        } catch (exc) {
            alert(exc);
        }
    });
    document.querySelector("#trigger-failfast").addEventListener("click", () => {
        try {
            exports.Sample.Test.CallFailFast();
            alert("No JS exception was thrown!");
        } catch (exc) {
            alert(exc);
        }
    });
    document.querySelector("#start-timer").addEventListener("click", () => {
        try {
            exports.Sample.Test.StartTimer();
        } catch (exc) {
            alert(exc);
        }
    });
    document.querySelector("#trigger-native-assert").addEventListener("click", () => {
        try {
            setenv(null, null);
            alert("No JS exception was thrown!");
        } catch (exc) {
            alert(exc);
        }
    });
    document.querySelector("#call-jsexport").addEventListener("click", () => {
        try {
            exports.Sample.Test.DoNothing();
        } catch (exc) {
            alert(exc);
        }
    });
    document.querySelector("#call-exit").addEventListener("click", () => {
        try {
            exit(7, "User clicked exit");
        } catch (exc) {
            alert(exc);
        }
    });
}
