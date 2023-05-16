// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

function saveProfile(aotProfileData) {
    if (!aotProfileData) {
        throw new Error("aotProfileData not set")
    }
    const a = document.createElement('a');
    const blob = new Blob([aotProfileData]);
    a.href = URL.createObjectURL(blob);
    a.download = "data.aotprofile";
    // Append anchor to body.
    document.body.appendChild(a);
    a.click();

    // Remove anchor from body
    document.body.removeChild(a);
}
try {
    const { INTERNAL, getAssemblyExports: getAssemblyExports } = await dotnet
        .withElementOnExit()
        .withExitCodeLogging()
        .withConfig({
            aotProfilerOptions: {
                writeAt: "Sample.Test::StopProfile",
                sendTo: "System.Runtime.InteropServices.JavaScript.JavaScriptExports::DumpAotProfileData"
            }
        })
        .create();

    console.log("not ready yet")
    const exports = await getAssemblyExports("Wasm.BrowserProfile.Sample");
    const testMeaning = exports.Sample.Test.TestMeaning;
    const stopProfile = exports.Sample.Test.StopProfile;
    console.log("ready");
    const ret = testMeaning();
    document.getElementById("out").innerHTML = ret;
    console.debug(`ret: ${ret}`);

    stopProfile();
    saveProfile(INTERNAL.aotProfileData);

    let exit_code = ret == 42 ? 0 : 1;
    exit(exit_code);
} catch (err) {
    exit(-1, err);
}
