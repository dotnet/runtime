// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './dotnet.js'

const assemblyName = "Wasm.Browser.Threads.Minimal.Sample.dll";


try {
    const { setModuleImports, getAssemblyExports, runMain } = await dotnet
        .withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
        .withElementOnExit()
        .withExitCodeLogging()
        .create();

    const exports = await getAssemblyExports(assemblyName);

    console.log("smoke: running TestCanStartThread");
    await exports.Sample.Test.TestCanStartThread();
    console.log("smoke: TestCanStartThread done");

    console.log ("smoke: running TestCallSetTimeoutOnWorker");
    await exports.Sample.Test.TestCallSetTimeoutOnWorker();
    console.log ("smoke: TestCallSetTimeoutOnWorker done");

    console.log ("smoke: running FetchBackground(blurst.txt)");
    let s = await exports.Sample.Test.FetchBackground("./blurst.txt");
    console.log ("smoke: FetchBackground(blurst.txt) done");
    if (s !== "It was the best of times, it was the blurst of times.\n") {
        const msg = `Unexpected FetchBackground result ${s}`;
        document.getElementById("out").innerHTML = msg;
        throw new Error (msg);
    }

    console.log ("smoke: running FetchBackground(missing)");
    s = await exports.Sample.Test.FetchBackground("./missing.txt");
    console.log ("smoke: FetchBackground(missing) done");
    if (s !== "not-ok") {
        const msg = `Unexpected FetchBackground(missing) result ${s}`;
        document.getElementById("out").innerHTML = msg;
        throw new Error (msg);
    }

    console.log ("smoke: running TaskRunCompute");
    const r1 = await exports.Sample.Test.RunBackgroundTaskRunCompute();
    if (r1 !== 524) {
        const msg = `Unexpected result ${r1} from RunBackgorundTaskRunCompute()`;
        document.getElementById("out").innerHTML = msg;
        throw new Error(msg);
    }
    console.log ("smoke: TaskRunCompute done");


    let exit_code = await runMain(assemblyName, []);
    exit(exit_code);
} catch (err) {
    exit(2, err);
}
