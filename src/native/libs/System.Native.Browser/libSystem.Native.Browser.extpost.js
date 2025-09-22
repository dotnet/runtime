// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var fetch = fetch || undefined; var _nativeModuleLoaded = false; var dotnetInternals = null;
export function initialize(internals) {
    if (_nativeModuleLoaded) throw new Error("Native module already loaded");
    dotnetInternals = internals;
    _nativeModuleLoaded = true;
    return createDotnetRuntime(dotnetInternals.runtimeApi.Module);
}
