// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

const assemblyName = "Wasm.Browser.Threads.Minimal.Sample.dll";


try {
    const resolveUrl = (relativeUrl) => (new URL(relativeUrl, window.location.href)).toString()

    const { getAssemblyExports, runMain } = await dotnet
        //.withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
        //.withDiagnosticTracing(true)
        .withElementOnExit()
        .withExitCodeLogging()
        .create();

    globalThis.test1 = { a: 1 };
    globalThis.test2 = { a: 2 };

    const exports = await getAssemblyExports(assemblyName);

    console.log("smoke: running LockTest");
    await exports.Sample.Test.LockTest();
    console.log("smoke: LockTest done ");


    console.log("smoke: running DisposeTest");
    await exports.Sample.Test.DisposeTest();
    console.log("smoke: DisposeTest done ");

    console.log("smoke: running TestHelloWebWorker");
    await exports.Sample.Test.TestHelloWebWorker();
    await exports.Sample.Test.TestHelloWebWorker();
    await exports.Sample.Test.TestHelloWebWorker();
    await exports.Sample.Test.TestHelloWebWorker();
    await exports.Sample.Test.TestHelloWebWorker();
    await exports.Sample.Test.TestHelloWebWorker();
    await exports.Sample.Test.TestHelloWebWorker();
    await exports.Sample.Test.TestHelloWebWorker();
    console.log("smoke: TestHelloWebWorker done");

    console.log("smoke: running TestCanStartThread");
    await exports.Sample.Test.TestCanStartThread();
    console.log("smoke: TestCanStartThread done");

    console.log("smoke: running TestTLS");
    await exports.Sample.Test.TestTLS();
    console.log("smoke: TestTLS done");

    console.log("smoke: running StartTimerFromWorker");
    exports.Sample.Test.StartTimerFromWorker();

    console.log("smoke: running TestCallSetTimeoutOnWorker");
    await exports.Sample.Test.TestCallSetTimeoutOnWorker();
    console.log("smoke: TestCallSetTimeoutOnWorker done");

    console.log("smoke: running HttpClientMain(blurst.txt)");
    let t = await exports.Sample.Test.HttpClientMain(globalThis.document.baseURI + "blurst.txt");
    console.log("smoke: HttpClientMain(blurst.txt) done " + t);

    console.log("smoke: running HttpClientWorker(blurst.txt)");
    let t2 = await exports.Sample.Test.HttpClientWorker(globalThis.document.baseURI + "blurst.txt");
    console.log("smoke: HttpClientWorker(blurst.txt) done " + t2);

    console.log("smoke: running HttpClientPool(blurst.txt)");
    let t3 = await exports.Sample.Test.HttpClientPool(globalThis.document.baseURI + "blurst.txt");
    console.log("smoke: HttpClientPool(blurst.txt) done " + t3);

    console.log("smoke: running HttpClientThread(blurst.txt)");
    let t4 = await exports.Sample.Test.HttpClientThread(globalThis.document.baseURI + "blurst.txt");
    console.log("smoke: HttpClientThread(blurst.txt) done " + t4);

    console.log("smoke: running WsClientMain");
    let w0 = await exports.Sample.Test.WsClientMain("wss://corefx-net-http11.azurewebsites.net/WebSocket/EchoWebSocket.ashx");
    console.log("smoke: WsClientMain done " + w0);

    console.log("smoke: running FetchBackground(blurst.txt)");
    let s = await exports.Sample.Test.FetchBackground(resolveUrl("./blurst.txt"));
    console.log("smoke: FetchBackground(blurst.txt) done");
    if (!s.startsWith("It was the best of times, it was the blurst of times.")) {
        const msg = `Unexpected FetchBackground result ${s}`;
        document.getElementById("out").innerHTML = msg;
        throw new Error(msg);
    }

    console.log("smoke: running FetchBackground(missing)");
    s = await exports.Sample.Test.FetchBackground(resolveUrl("./missing.txt"));
    console.log("smoke: FetchBackground(missing) done");
    if (s !== "not-ok") {
        const msg = `Unexpected FetchBackground(missing) result ${s}`;
        document.getElementById("out").innerHTML = msg;
        throw new Error(msg);
    }

    console.log("smoke: running TaskRunCompute");
    const r1 = await exports.Sample.Test.RunBackgroundTaskRunCompute();
    if (r1 !== 524) {
        const msg = `Unexpected result ${r1} from RunBackgorundTaskRunCompute()`;
        document.getElementById("out").innerHTML = msg;
        throw new Error(msg);
    }
    console.log("smoke: TaskRunCompute done");

    console.log("smoke: running StartAllocatorFromWorker");
    exports.Sample.Test.StartAllocatorFromWorker();

    /* ActiveIssue https://github.com/dotnet/runtime/issues/88663
    await delay(5000);

    console.log("smoke: running GCCollect");
    exports.Sample.Test.GCCollect();

    await delay(5000);

    console.log("smoke: running GCCollect");
    exports.Sample.Test.GCCollect();
    console.log("smoke: running GCCollect done");
    */

    console.log("smoke: running StopTimerFromWorker");
    exports.Sample.Test.StopTimerFromWorker();

    let exit_code = await runMain(assemblyName, []);
    exit(exit_code);
} catch (err) {
    exit(2, err);
}

function delay(timeoutMs) {
    return new Promise(resolve => setTimeout(resolve, timeoutMs));
}

