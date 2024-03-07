// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

let progressElement = null;
let inputElement = null;
let exports = null;
const assemblyName = "Wasm.Browser.Threads.Sample.dll";

try {
    progressElement = document.getElementById("progressElement");
    inputElement = document.getElementById("inputN");

    const { setModuleImports, getAssemblyExports, runMain } = await dotnet
        .withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
        .withElementOnExit()
        .withExitCodeLogging()
        .withExitOnUnhandledError()
        .create();

    setModuleImports("main.js", {
        Sample: {
            Test: {
                updateProgress
            }
        }
    });

    exports = await getAssemblyExports(assemblyName);

    await doSlowMath();
    setEditable(true);
    inputElement.addEventListener("change", onInputValueChanged);

    let exit_code = await runMain(assemblyName, []);
    // comment out the following line for interactive testing, otherwise further call would be rejected by runtime
    exit(exit_code);
} catch (err) {
    exit(2, err);
}

async function doSlowMath() {
    const N = parseInt(document.getElementById("inputN").value);
    const resultPromise = exports.Sample.Test.Fib(N);

    while (true) {
        await delay(50);
        const isRunning = exports.Sample.Test.Progress();
        if (!isRunning)
            break;
    }
    const answer = await resultPromise;
    document.getElementById("out").innerText = `Fib(${N}) =  ${answer}`;

}

export async function updateProgress(status) {
    if (progressElement) {
        progressElement.innerText = status;
    } else {
        console.log("Progress: " + status);
    }
}

function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function setEditable(isEditable) {
    inputElement.disabled = !isEditable;
}

async function onInputValueChanged() {
    setEditable(false);
    await doSlowMath(exports);
    setEditable(true);
}
