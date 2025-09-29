//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-undef */
/* eslint-disable space-before-function-paren */
var fetch = fetch || undefined; var dotnetNativeModuleLoaded = false; var dotnetInternals = null;
export function dotnetInitializeModule(internals) {
    if (dotnetNativeModuleLoaded) throw new Error("Native module already loaded");
    dotnetInternals = internals;
    dotnetNativeModuleLoaded = true;
    return createDotnetRuntime(dotnetInternals[0/*InternalExchangeIndex.RuntimeAPI*/].Module);
}
