// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var Module = {

    config: null,

    preInit: async function () {
        await MONO.mono_wasm_load_config("./mono-config.json"); // sets MONO.config implicitly
    },

    onRuntimeInitialized: function () {
        if (!MONO.config || MONO.config.error) {
            console.log("No config found");
            test_exit(1);
            throw (MONO.config.error);
        }

        MONO.config.loaded_cb = function () {
            try {
                App.init();
            } catch (error) {
                test_exit(1);
                throw (error);
            }
        };
        MONO.config.fetch_file_cb = function (asset) {
            return fetch(asset, { credentials: 'same-origin' });
        }

        if (MONO.config.environment_variables !== undefined) {
            console.log("expected environment variables to be undefined, but they're: ", MONO.config.environment_variables);
            test_exit(1);
        }
        MONO.config.environment_variables = {
            "DOTNET_MODIFIABLE_ASSEMBLIES": "debug"
        };

        try {
            MONO.mono_load_runtime_and_bcl_args(MONO.config);
        } catch (error) {
            test_exit(1);
            throw (error);
        }
    },
};
