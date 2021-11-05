// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";
var Module = {
    config: null,
    configSrc: "./mono-config.json",
    onConfigLoaded: function () {
        if (MONO.config.enable_profiler) {
            MONO.config.aot_profiler_options = {
                write_at: "Sample.Test::StopProfile",
                send_to: "System.Runtime.InteropServices.JavaScript.Runtime::DumpAotProfileData"
            }
        }
    },
    onDotNetReady: function () {
        try {
            App.init();
        } catch (error) {
            test_exit(1);
            throw (error);
        }
    },
    onAbort: function (err) {
        test_exit(1);

    },
};
