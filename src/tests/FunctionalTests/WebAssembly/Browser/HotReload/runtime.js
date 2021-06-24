// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var Module = { 
    onRuntimeInitialized: function () {
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

        if (config.environment_variables !== undefined) {
            console.log ("expected environment variables to be undefined, but they're: ", config.environment_variables);
            test_exit(1);
        }
        config.environment_variables = {
            "DOTNET_MODIFIABLE_ASSEMBLIES": "debug"
        };

        try
        {
            MONO.mono_load_runtime_and_bcl_args (config);
        } catch (error) {
            test_exit(1);
            throw(error);
        }
    },
};
