// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";
import createRuntime from './dotnet.js'

export const { MONO, INTERNAL, BINDING, Module } = await createRuntime(({ MONO, INTERNAL, BINDING, Module }) => ({
    disableDotNet6Compatibility: true,
    configSrc: "./mono-config.json",
    onAbort: function () {
        test_exit(1);
    },
}));