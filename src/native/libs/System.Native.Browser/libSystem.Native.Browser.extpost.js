//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var fetch = fetch || undefined; var netNativeModuleLoaded = false; var dotnetInternals = null;
export function dotnetInitializeModule(internals) {
    if (netNativeModuleLoaded) throw new Error("Native module already loaded");
    dotnetInternals = internals;
    netNativeModuleLoaded = true;
    return createDotnetRuntime(dotnetInternals[0/*InternalExchangeIndex.RuntimeAPI*/].Module);
}
