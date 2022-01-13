import createDotnetRuntime from './dotnet.js'

function wasm_exit(exit_code) {
    console.log(`WASM EXIT ${exit_code}`);
}

function saveProfile() {
    const a = document.createElement('a');
    const blob = new Blob([INTERNAL.aot_profile_data]);
    a.href = URL.createObjectURL(blob);
    a.download = "data.aotprofile";
    // Append anchor to body.
    document.body.appendChild(a);
    a.click();

    // Remove anchor from body
    document.body.removeChild(a);
}

try {
    const { MONO, BINDING, Module } = await createDotnetRuntime(({ MONO }) => ({
        configSrc: "./mono-config.json",
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
        saveProfile();
    }

    let exit_code = ret == 42 ? 0 : 1;
    wasm_exit(exit_code);
} catch (err) {
    console.log(`WASM ERROR ${err}`);
}
