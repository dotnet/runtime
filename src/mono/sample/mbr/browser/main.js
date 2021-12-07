// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";
var Module = {
    configSrc: "./mono-config.json",
    onConfigLoaded: function () {
        MONO.config.environment_variables["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug";
    },
    onDotnetReady: function () {
        App.init();
    },
};
