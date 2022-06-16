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

function saveProfile(aot_profile_data) {
    if (!aot_profile_data) {
        throw new Error("aot_profile_data not set")
    }
    const a = document.createElement('a');
    const blob = new Blob([aot_profile_data]);
    a.href = URL.createObjectURL(blob);
    a.download = "data.aotprofile";
    // Append anchor to body.
    document.body.appendChild(a);
    a.click();

    // Remove anchor from body
    document.body.removeChild(a);
}

try {
    const { MONO, BINDING, INTERNAL } = await createDotnetRuntime(({ MONO }) => ({
        configSrc: "./mono-config.json",
        disableDotnet6Compatibility: true,
        onConfigLoaded: () => {
            if (MONO.config.enable_profiler) {
                MONO.config.aot_profiler_options = {
                    write_at: "Sample.Test::StopProfile",
                    send_to: "System.Runtime.InteropServices.JavaScript.Runtime::DumpAotProfileData"
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

    if (MONO.config.enable_profiler) {
        stopProfile();
        saveProfile(INTERNAL.aot_profile_data);
    }

    let exit_code = ret == 42 ? 0 : 1;
    wasm_exit(exit_code);
} catch (err) {
    wasm_exit(-1, err);
}
