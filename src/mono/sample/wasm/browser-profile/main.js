import createDotnetRuntime from './dotnet.js'

function wasm_exit(exit_code, reason) {
    /* Set result in a tests_done element, to be read by xharness */
    const tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    if (exit_code) tests_done_elem.style.background = "red";
    document.body.appendChild(tests_done_elem);

    if (reason) console.error(reason);
    console.log(`WASM EXIT ${exit_code}`);
}

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
let enableProfiler = false
try {
    const { BINDING, INTERNAL } = await createDotnetRuntime(({ MONO }) => ({
        configSrc: "./mono-config.json",
        disableDotnet6Compatibility: true,
        onConfigLoaded: (config) => {
            if (config.enableProfiler) {
                enableProfiler = true;
                config.aotProfilerOptions = {
                    writeAt: "Sample.Test::StopProfile",
                    sendTo: "System.Runtime.InteropServices.JavaScript.JavaScriptExports::DumpAotProfileData"
                }
            }
        },
    }));
    console.log("not ready yet")
    const testMeaning = BINDING.bind_static_method("[Wasm.BrowserProfile.Sample] Sample.Test:TestMeaning");
    const stopProfile = BINDING.bind_static_method("[Wasm.BrowserProfile.Sample] Sample.Test:StopProfile");
    console.log("ready");
    const ret = testMeaning();
    document.getElementById("out").innerHTML = ret;
    console.debug(`ret: ${ret}`);

    if (enableProfiler) {
        stopProfile();
        saveProfile(INTERNAL.aotProfileData);
    }

    let exit_code = ret == 42 ? 0 : 1;
    wasm_exit(exit_code);
} catch (err) {
    wasm_exit(-1, err);
}
