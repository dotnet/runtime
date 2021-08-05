// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var Module = { 

    config: null,

    preInit: async function() {
        await MONO.mono_wasm_load_config("./mono-config.json"); // sets Module.config implicitly
    },

    onRuntimeInitialized: function () {
        if (!Module.config || Module.config.error) {
            console.log("No config found");
            test_exit(1);
            throw(Module.config.error);
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

        if (Module.config.environment_variables !== undefined) {
            console.log ("expected environment variables to be undefined, but they're: ", Module.config.environment_variables);
            test_exit(1);
        }
        Module.config.environment_variables = {
            "DOTNET_MODIFIABLE_ASSEMBLIES": "debug"
        };

        try
        {
            MONO.mono_load_runtime_and_bcl_args (Module.config);
        } catch (error) {
            test_exit(1);
            throw(error);
        }
    },
};
