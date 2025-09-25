//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var fetch = fetch || undefined; var netNativeModuleLoaded = false; var netInternals = null;
export function netInitializeModule(internals) {
    if (netNativeModuleLoaded) throw new Error("Native module already loaded");
    netInternals = internals;
    netNativeModuleLoaded = true;
    return createDotnetRuntime(netInternals[0/*InternalExchangeIndex.RuntimeAPI*/].Module);
}
