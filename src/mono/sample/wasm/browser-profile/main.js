// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";
var Module = {
    configSrc: "./mono-config.json",
    onConfigLoaded: () => {
        if (MONO.config.enable_profiler) {
            MONO.config.aot_profiler_options = {
                write_at: "Sample.Test::StopProfile",
                send_to: "System.Runtime.InteropServices.JavaScript.Runtime::DumpAotProfileData"
            }
        }
    },
    onDotnetReady: () => {
        try {
            Module.init();
        } catch (error) {
            set_exit_code(1, error);
            throw (error);
        }
    },
    onAbort: (error) => {
        set_exit_code(1, error);
    },

    init: () => {
        console.log("not ready yet")
        const testMeaning = BINDING.bind_static_method("[Wasm.BrowserProfile.Sample] Sample.Test:TestMeaning");
        const stopProfile = BINDING.bind_static_method("[Wasm.BrowserProfile.Sample] Sample.Test:StopProfile");
        const ret = testMeaning();
        document.getElementById("out").innerHTML = ret;
        console.log("ready");

        console.debug(`ret: ${ret}`);
        let exit_code = ret == 42 ? 0 : 1;
        Module.set_exit_code(exit_code);

        if (MONO.config.enable_profiler) {
            stopProfile();
            Module.saveProfile();
        }
    },

    set_exit_code: (exit_code, reason) => {
        /* Set result in a tests_done element, to be read by xharness */
        const tests_done_elem = document.createElement("label");
        tests_done_elem.id = "tests_done";
        tests_done_elem.innerHTML = exit_code.toString();
        document.body.appendChild(tests_done_elem);

        console.log(`WASM EXIT ${exit_code}`);
    },

    saveProfile: () => {
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
};
