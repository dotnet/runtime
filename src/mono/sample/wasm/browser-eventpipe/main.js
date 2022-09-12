// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from "./dotnet.js";

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms))

async function doWork(startWork, stopWork, getIterationsDone) {
    const N = parseInt(document.getElementById("inputN").value);


    const workPromise = startWork(N);

    let btn = document.getElementById("startWork");
    btn.disabled = true;
    btn.innerText = "Working";
    document.getElementById("out").innerHTML = '...';

    console.log("TMP before delay...");
    await delay(5000); // let it run for 5 seconds
    console.log("TMP afeter delay");

    document.getElementById("startWork").innerText = "Stopping";
    document.getElementById("out").innerHTML = '... ...';

    stopWork();

    console.log("TMP getting the answer ...");
    const ret = await workPromise; // get the answer
    console.log("TMP got the answer ...");
    const iterations = getIterationsDone(); // get how many times the loop ran

    btn = document.getElementById("startWork");
    btn.disabled = false;
    btn.innerText = "Start Work";

    document.getElementById("out").innerHTML = `${ret} as computed in ${iterations} iterations`;

    console.debug(`ret: ${ret}`);

    return ret;
}

function getOnClickHandler(startWork, stopWork, getIterationsDone) {
    return async function () {
        await doWork(startWork, stopWork, getIterationsDone);
    }
}

const isTest = (config) => config.environmentVariables["CI_TEST"] === "true";
async function runTest({ StartAsyncWork, StopWork, GetIterationsDone }) {
    console.log("TMP running test");
    const result = await doWork(StartAsyncWork, StopWork, GetIterationsDone);
    console.log("TMP done work");
    const expectedResult = 55; // the default value of `inputN` is 10 (see index.html)
    return result === expectedResult;
}

async function main() {
    console.log("TMP running main");
    const { MONO, Module, getAssemblyExports, getConfig } = await dotnet
        .withElementOnExit()
        .withExitCodeLogging()
        .create();
    console.log("TMP created dotnet");

    globalThis.__Module = Module;
    globalThis.MONO = MONO;

    console.log("TMP getting exports");
    const exports = await getAssemblyExports("Wasm.Browser.EventPipe.Sample.dll");
    console.log("TMP got exports");

    console.log("TMP setting up btn");
    const btn = document.getElementById("startWork");
    btn.style.backgroundColor = "rgb(192,255,192)";
    btn.onclick = getOnClickHandler(exports.Sample.Test.StartAsyncWork, exports.Sample.Test.StopWork, exports.Sample.Test.GetIterationsDone);
    console.log("TMP set up btn");

    const config = getConfig();
    if (isTest(config)) {
        console.log("TMP is test");
        const succeeded = await runTest(exports.Sample.Test);
        console.log("TMP exitting...");
        exit(succeeded ? 0 : 1);
        console.log("TMP exitted");
    }
}

main();
