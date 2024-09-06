// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

function saveProfile(Module) {
    let profileData = readProfileFile(Module);

    const a = document.createElement('a');
    const blob = new Blob([profileData]);
    a.href = URL.createObjectURL(blob);
    a.download = "output.mlpd";
    // Append anchor to body.
    document.body.appendChild(a);
    a.click();

    // Remove anchor from body
    document.body.removeChild(a);
}

function readProfileFile(Module) {
    let profileFilePath="output.mlpd";

    var stat = Module.FS.stat(profileFilePath);

    if (stat && stat.size > 0) {
        return Module.FS.readFile(profileFilePath);
    }
    else {
        console.debug(`Unable to fetch the profile file ${profileFilePath} as it is empty`);
        return null;
    }
}

try {
    const { INTERNAL, Module, getAssemblyExports: getAssemblyExports } = await dotnet
        .withElementOnExit()
        .withExitCodeLogging()
        .withConfig({
            logProfilerOptions: {
                configuration: "log:alloc,output=output.mlpd"
            }
        })
        .create();

    console.log("not ready yet")
    const exports = await getAssemblyExports("Wasm.BrowserLogProfile.Sample");
    const testMeaning = exports.Sample.Test.TestMeaning;
    const createSnapshot = exports.Sample.Test.CreateSnapshot;
    console.log("ready");

    dotnet.run();

    const ret = testMeaning();
    document.getElementById("out").innerHTML = ret;
    console.debug(`ret: ${ret}`);

    createSnapshot();
    saveProfile(Module);

    let exit_code = ret == 42 ? 0 : 1;
    exit(exit_code);
} catch (err) {
    exit(-1, err);
}
