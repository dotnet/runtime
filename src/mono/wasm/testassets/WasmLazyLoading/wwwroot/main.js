// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

const { getAssemblyExports, getConfig, INTERNAL } = await dotnet
    .withElementOnExit()
    .withExitCodeLogging()
    .withExitOnUnhandledError()
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

try {
    await INTERNAL.loadLazyAssembly("System.Text.Json.wasm");
    const json = await exports.MyClass.GetJson();
    console.log(json);
    exit(0);
} catch (e) {
    exit(1, e);
}
