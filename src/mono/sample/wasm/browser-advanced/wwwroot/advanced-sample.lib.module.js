// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function onRuntimeConfigLoaded(config) {
    console.log("advanced-sample onRuntimeConfigLoaded")
}

export async function onRuntimeReady({ getAssemblyExports, getConfig }) {
    console.log("advanced-sample onRuntimeReady")
}