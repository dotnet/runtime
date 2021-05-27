// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var Module = { 

    config: null,

    // Called once the config file is loaded. The contents of the config file
    // are passed as a JS object within the config parameter
    onConfigLoaded: function (config) {
        if (!config || config.error){
            console.log("An error occured while loading the config file");
            return;
        }

        Module.config = config;
    },

    // Called when the runtime is initialized and wasm is ready
    onRuntimeInitialized: function () {
        if (!Module.config || Module.config.error){
            alert("No config found");
            return;
        }

        Module.config.loaded_cb = function () {
            try {
                App.init ();
            } catch (error) {
                test_exit(1);
                throw (error);
            }
        };
        Module.config.fetch_file_cb = function (asset) {
            return fetch (asset, { credentials: 'same-origin' });
        }

        try
        {
            MONO.mono_load_runtime_and_bcl_args (Module.config);
        } catch (error) {
            test_exit(1);
            throw(error);
        }
    },
};
