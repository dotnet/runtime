// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var Module = { 
    onRuntimeInitialized: function () {
        config.loaded_cb = function () {
            App.init ();
        };
        config.fetch_file_cb = function (asset) {
            return fetch (asset, { credentials: 'same-origin' });
        }

        MONO.mono_load_runtime_and_bcl_args (config);
    },
};
