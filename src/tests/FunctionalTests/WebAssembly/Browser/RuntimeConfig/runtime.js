// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var Module = {
    no_global_exports: true,
    config: null,

    preInit: async function () {
        await Module.MONO.mono_wasm_load_config("./mono-config.json"); // sets Module.MONO.config implicitly
    },

    onRuntimeInitialized: function () {
        if (!Module.MONO.config || Module.MONO.config.error) {
            console.log("No config found");
            test_exit(1);
            throw (Module.MONO.config.error);
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

        try {
            Module.MONO.mono_load_runtime_and_bcl_args(Module.MONO.config);
        } catch (error) {
            test_exit(1);
            throw (error);
        }
    },
};
