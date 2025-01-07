// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from "./_framework/dotnet.js";

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms))

async function doWork(startWork, stopWork, getIterationsDone) {
    const N = parseInt(document.getElementById("inputN").value);


    const workPromise = startWork(N);

    let btn = document.getElementById("startWork");
    btn.disabled = true;
    btn.innerText = "Working";
    document.getElementById("out").innerHTML = '...';

    await delay(5000); // let it run for 5 seconds

    document.getElementById("startWork").innerText = "Stopping";
    document.getElementById("out").innerHTML = '... ...';

    stopWork();

    const ret = await workPromise; // get the answer
    const iterations = getIterationsDone(); // get how many times the loop ran

    INTERNAL.diagnosticServerThread.postMessageToWorker({
        type: "diagnostic_server_mock",
        cmd: "fibonacci-done"
    });

    btn = document.getElementById("startWork");
    btn.disabled = false;
    btn.innerText = "Start Work";

    document.getElementById("out").innerHTML = `${ret} as computed in ${iterations} iterations`;

    console.debug(`ret: ${ret}`);

    return ret;
}

const isTest = (config) => config.environmentVariables["CI_TEST"] === "true";
async function runTest({ StartAsyncWork, StopWork, GetIterationsDone }) {
    const result = await doWork(StartAsyncWork, StopWork, GetIterationsDone);
    const expectedResult = 55; // the default value of `inputN` is 10 (see index.html)
    return result === expectedResult;
}

async function main() {
    const { INTERNAL, MONO, Module, getAssemblyExports, getConfig } = await dotnet
        .withElementOnExit()
        .withExitCodeLogging()
        .withDiagnosticTracing(false)
        .create();

    globalThis.__Module = Module;
    globalThis.MONO = MONO;
    globalThis.INTERNAL = INTERNAL;

    const exports = await getAssemblyExports("Wasm.Browser.EventPipe.Sample.dll");

    const btn = document.getElementById("startWork");
    btn.style.backgroundColor = "rgb(192,255,192)";
    btn.onclick = () => doWork(exports.Sample.Test.StartAsyncWork, exports.Sample.Test.StopWork, exports.Sample.Test.GetIterationsDone);

    if (INTERNAL.diagnosticServerThread === undefined) {
        console.warn("please recompile runtime with /p:WasmEnableThreads=true /p:MonoDiagnosticsMock=true" + event.type)
        return;
    }

    INTERNAL.diagnosticServerThread.port.addEventListener("message", (event) => {
        console.warn("diagnosticServerThread" + event.type)

        if (event.data.cmd === "collecting") {
            btn.onclick();
        }

        if (event.data.cmd === "collected") {
            const buffer = event.data.buffer;
            const length = event.data.length;
            console.warn("Downloading trace " + length)
            const view = new Uint8Array(buffer, 0, length)
            const blobUrl = URL.createObjectURL(new Blob([view.slice()]));
            const link = document.createElement("a");
            link.href = blobUrl;
            link.download = "trace." + (new Date()).valueOf() + ".nettrace";
            document.body.appendChild(link);

            link.dispatchEvent(new MouseEvent('click', {
                bubbles: true, cancelable: true, view: window
            }));

            document.body.removeChild(link);
        }
    });

    const config = getConfig();
    if (isTest(config)) {
        const succeeded = await runTest(exports.Sample.Test);
        exit(succeeded ? 0 : 1);
    }
}

main();
