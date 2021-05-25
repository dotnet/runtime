// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
var Module = { 
    is_testing: false,

    onRuntimeInitialized: function () {
        JSSupportLib.load_config(Module.setupConfig);
    },

    setupConfig: function (config) {
        config.loaded_cb = function () {
            try {
                Module.init();
            } catch (error) {
                Module.test_exit(1);
                throw (error);
            }
        };
        config.fetch_file_cb = function (asset) {
            return fetch (asset, { credentials: 'same-origin' });
        }

        if (config.enable_profiler)
        {
            config.aot_profiler_options = {
                write_at:"Sample.Test::StopProfile",
                send_to: "System.Runtime.InteropServices.JavaScript.Runtime::DumpAotProfileData"
            }
        }

        try
        {
            MONO.mono_load_runtime_and_bcl_args (config);
        } catch (error) {
            Module.test_exit(1);
            throw(error);
        }
    },

    init: function () {
        console.log("not ready yet")
        var ret = BINDING.call_static_method("[Wasm.BrowserProfile.Sample] Sample.Test:TestMeaning", []);
        document.getElementById("out").innerHTML = ret;
        console.log ("ready");

        if (Module.is_testing)
        {
          console.debug(`ret: ${ret}`);
          let exit_code = ret == 42 ? 0 : 1;
          Module.test_exit(exit_code);
        }

        if (config.enable_profiler) {
          BINDING.call_static_method("[Wasm.BrowserProfile.Sample] Sample.Test:StopProfile", []);
          Module.saveProfile();
        }
    },

    onLoad: function() {
        var url = new URL(decodeURI(window.location));
        let args = url.searchParams.getAll('arg');
        is_testing = args !== undefined && (args.find(arg => arg == '--testing') !== undefined);
    },
  
    test_exit: function(exit_code) {
        if (!Module.is_testing) {
            console.log(`test_exit: ${exit_code}`);
            return;
        }

        /* Set result in a tests_done element, to be read by xharness */
        var tests_done_elem = document.createElement("label");
        tests_done_elem.id = "tests_done";
        tests_done_elem.innerHTML = exit_code.toString();
        document.body.appendChild(tests_done_elem);

        console.log(`WASM EXIT ${exit_code}`);
    },
  
    saveProfile: function () {
        var a = document.createElement('a');
        var blob = new Blob([Module.aot_profile_data]);
        a.href = URL.createObjectURL(blob);
        a.download = "data.aotprofile";
        // Append anchor to body.
        document.body.appendChild(a);
        a.click();

        // Remove anchor from body
        document.body.removeChild(a);
    }
};