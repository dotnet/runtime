// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './dotnet.js'

function wasm_exit(exit_code) {
    var tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    document.body.appendChild(tests_done_elem);

    console.log(`WASM EXIT ${exit_code}`);
}

let progressElement = null;

function updateProgress(status) {
    if (progressElement) {
        progressElement.innerText = status;
    } else {
        console.log("Progress: " + status);
    }
}

const assemblyName = "Wasm.Browser.Threads.Sample.dll";

function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function Run(exports, N) {
    while (true) {
        await delay(50);
        const p = exports.Sample.Test.Progress();
        if (p === 0)
            break;
    }
    const answer = exports.Sample.Test.GetAnswer();
    document.getElementById("out").innerText = `Fib(${N}) =  ${answer}`;
}

async function doMathSlowly(exports) {
    progressElement = document.getElementById("progressElement");
    const N = parseInt(document.getElementById("inputN").value);
    exports.Sample.Test.Start(N);
    await Run(exports, N);
}

function setEditable(inputElement, isEditable) {
    inputElement.disabled = !isEditable;
}

function onInputValueChanged(exports, inputElement) {
    async function handler() {
        setEditable(inputElement, false);
        await doMathSlowly(exports);
        setEditable(inputElement, true);
    }
    return handler;
}

try {
    const inputElement = document.getElementById("inputN");
    const { setModuleImports, getAssemblyExports, runMain } = await dotnet
        .withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
        .create();

    setModuleImports("main.js", {
        Sample: {
            Test: {
                updateProgress
            }
        }
    });

    const exports = await getAssemblyExports(assemblyName);

    await doMathSlowly(exports);
    setEditable(inputElement, true);
    inputElement.addEventListener("change", onInputValueChanged(exports, inputElement));

    let exit_code = await runMain(assemblyName, []);
    wasm_exit(exit_code);
    exit(exit_code);
} catch (err) {
    wasm_exit(2);
    exit(exit_code, err);
}
