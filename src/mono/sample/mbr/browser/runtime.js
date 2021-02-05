// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var Module = { 
    onRuntimeInitialized: function () {
        config.loaded_cb = function () {
            App.init ();
        };
        config.environment_variables = {
            "MONO_METADATA_UPDATE": "1"
        };
        config.fetch_file_cb = function (asset) {
            return fetch (asset, { credentials: 'same-origin' });
        }

        MONO.mono_load_runtime_and_bcl_args (config);
    },
};
