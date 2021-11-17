// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

var Module = {
    configSrc: "./mono-config.json",
    onConfigLoaded: () => {
        MONO.config.environment_variables["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug";
    },
    onDotNetReady: () => {
        try {
            App.init();
        } catch (error) {
            set_exit_code(1, error);
            throw (error);
        }
    },
    onAbort: (error) => {
        set_exit_code(1, error);
    },
};
