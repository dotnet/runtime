// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";
var Module = {
    config: null,

    preInit: async function () {
        await MONO.mono_wasm_load_config("./mono-config.json"); // sets MONO.config implicitly
    },

    // Called when the runtime is initialized and wasm is ready
    onRuntimeInitialized: function () {
        if (!MONO.config || MONO.config.error) {
            console.log("An error occured while loading the config file");
            return;
        }

        MONO.config.loaded_cb = function () {
            App.init();
        };
        MONO.config.environment_variables = {
            "DOTNET_MODIFIABLE_ASSEMBLIES": "debug"
        };
        MONO.config.fetch_file_cb = function (asset) {
            return fetch(asset, { credentials: 'same-origin' });
        }

        MONO.mono_load_runtime_and_bcl_args(MONO.config);
    },
};
