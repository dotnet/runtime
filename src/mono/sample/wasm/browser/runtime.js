// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var Module = { 
    // Called when the runtime is initialized and wasm is ready
    onRuntimeInitialized: function () {
        JSSupportLib.load_config(this.onConfigLoaded);
    },

    // Called once the config file is loaded. THe contents of the config file
    // are passed as a JS object within the config parameter
    onConfigLoaded: function (config) {
        config.loaded_cb = function () {
            try {
                App.init ();
            } catch (error) {
                test_exit(1);
                throw (error);
            }
        };
        config.fetch_file_cb = function (asset) {
            return fetch (asset, { credentials: 'same-origin' });
        }

        try
        {
            MONO.mono_load_runtime_and_bcl_args (config);
        } catch (error) {
            test_exit(1);
            throw(error);
        }
    },
};
