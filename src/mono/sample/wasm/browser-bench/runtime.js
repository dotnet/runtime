// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";
var Module = {
    no_global_exports: true,
    config: null,

    preInit: async function () {
        await Module.MONO.mono_wasm_load_config("./mono-config.json"); // sets Module.MONO.config implicitly
    },

    // Called when the runtime is initialized and wasm is ready
    onRuntimeInitialized: function () {
        if (!Module.MONO.config || Module.MONO.config.error) {
            console.log("An error occured while loading the config file");
            return;
        }

        Module.MONO.config.loaded_cb = function () {
            try {
                App.init();
            } catch (error) {
                test_exit(1);
                throw (error);
            }
        };
        Module.MONO.config.fetch_file_cb = function (asset) {
            return fetch(asset, { credentials: 'same-origin' });
        }

        if (Module.MONO.config.enable_profiler) {
            Module.MONO.config.aot_profiler_options = {
                write_at: "Sample.Test::StopProfile",
                send_to: "System.Runtime.InteropServices.JavaScript.Runtime::DumpAotProfileData"
            }
        }

        try {
            Module.MONO.mono_load_runtime_and_bcl_args(Module.MONO.config);
        } catch (error) {
            test_exit(1);
            throw (error);
        }
    },
};
