//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-undef */
/* eslint-disable space-before-function-paren */
/* eslint-disable @typescript-eslint/no-unused-vars */
var fetch = fetch || undefined; var dotnetNativeModuleLoaded = false; var dotnetInternals = null;
export function dotnetInitializeModule(internals) {
    if (dotnetNativeModuleLoaded) throw new Error("Native module already loaded");
    dotnetNativeModuleLoaded = true;
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");
    dotnetInternals = internals;
    const runtimeApi = internals[0] = internals[0/*InternalExchangeIndex.RuntimeAPI*/] || {};
    return createDotnetRuntime(runtimeApi.Module || {});
}
