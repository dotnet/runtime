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

    const r1 = await exports.Sample.Test.RunBackgroundThreadCompute();
    if (r1 !== 524) {
        const msg = `Unexpected result ${r1} from RunBackgroundThreadCompute()`;
        document.getElementById("out").innerHTML = msg;
        throw new Error(msg);
    }
    const r2 = await exports.Sample.Test.RunBackgroundLongRunningTaskCompute();
    if (r2 !== 524) {
        const msg = `Unexpected result ${r2} from RunBackgorundLongRunningTaskCompute()`;
        document.getElementById("out").innerHTML = msg;
        throw new Error(msg);
    }

    let exit_code = await runMain(assemblyName, []);
    exit(exit_code);
} catch (err) {
    exit(2, err);
}
