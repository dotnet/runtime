// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

createDotnetRuntime(({ MONO, BINDING, Module }) => ({
    disableDotnet6Compatibility: true,
    configSrc: "./mono-config.json",
    onDotnetReady: () => {
        try {
            App.init({ MONO, BINDING, Module });
        } catch (error) {
            set_exit_code(1, error);
            throw (error);
        }
    },
    onAbort: (error) => {
        set_exit_code(1, error);
    },
}));
