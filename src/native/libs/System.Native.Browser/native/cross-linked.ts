// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    Assert, Logger, Module,
    netInternals, netLoaderExports, netPublicApi, netNativeBrowserExports, netRuntimeExports, netJSEngine, netBrowserHostExports, netInteropJSExports,
    netTabulateNBE, netTabulateHE, netTabulateJSNE, netTabulateLE, netTabulateRE,
    getInternals, netSetInternals, netUpdateAllInternals, netUpdateModuleInternals,
} from "../cross-module";

import { } from "../../Common/JavaScript/cross-linked";

// this dummy function helps rollup to keep functions below from trimming
// we are installing them into emscripten closure
export function crossLink() {
    return [
        Assert, Logger, Module,
        netInternals, netLoaderExports, netPublicApi, netNativeBrowserExports, netRuntimeExports, netJSEngine, netBrowserHostExports, netInteropJSExports,
        getInternals, netSetInternals, netUpdateAllInternals, netUpdateModuleInternals,
        netTabulateHE, netTabulateJSNE, netTabulateLE, netTabulateNBE, netTabulateRE
    ];
}
