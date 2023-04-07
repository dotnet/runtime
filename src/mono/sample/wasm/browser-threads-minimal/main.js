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

    //console.log ("XYZ: running hello");
    //await exports.Sample.Test.Hello();
    //console.log ("XYZ: hello done");

    console.log ("XYZ: running FetchBackground");
    let s = await exports.Sample.Test.FetchBackground("./blurst.txt");
    console.log ("XYZ: FetchBackground done");
    if (s !== "It was the best of times, it was the blurst of times.\n") {
        const msg = `Unexpected FetchBackground result ${s}`;
        document.getElementById("out").innerHTML = msg;
        throw new Error (msg);
    }

    console.log ("XYZ: running FetchBackground(missing)");
    s = await exports.Sample.Test.FetchBackground("./missing.txt");
    console.log ("XYZ: FetchBackground(missing) done");
    if (s !== "not-ok") {
        const msg = `Unexpected FetchBackground(missing) result ${s}`;
        document.getElementById("out").innerHTML = msg;
        throw new Error (msg);
    }
    
    //console.log ("HHH: running TaskRunCompute");
    //const r1 = await exports.Sample.Test.RunBackgroundTaskRunCompute();
    //if (r1 !== 524) {
    //    const msg = `Unexpected result ${r1} from RunBackgorundTaskRunCompute()`;
    //    document.getElementById("out").innerHTML = msg;
    //    throw new Error(msg);
    //}
    //console.log ("HHH: TaskRunCompute done");


    let exit_code = await runMain(assemblyName, []);
    exit(exit_code);
} catch (err) {
    exit(2, err);
}
