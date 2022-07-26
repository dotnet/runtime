// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";
var Module = {
    configSrc: "./mono-config.json",
    onDotnetReady: () => {
        try {
            App.init();
        } catch (error) {
            wasm_exit(1, error);
            throw (error);
        }
    },
    onAbort: (error) => {
        wasm_exit(1, error);
    },
};
