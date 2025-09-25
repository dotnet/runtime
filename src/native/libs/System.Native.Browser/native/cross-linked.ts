// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    Assert, Logger, Module,
    netInternals, netLoaderExports, netPublicApi, netNativeBrowserExports, netRuntimeExports, netJSEngine, netBrowserHostExports, netInteropJSExports,
    netTabulateNBE, netTabulateBHE, netTabulateIJSE, netTabulateLE, netTabulateRE,
    getInternals, netSetInternals, netUpdateAllInternals, netUpdateModuleInternals,
} from "../cross-module";

import { } from "../../Common/JavaScript/cross-linked";

export function crossLink() {
    return [
        Assert, Logger, Module,
        netInternals, netLoaderExports, netPublicApi, netNativeBrowserExports, netRuntimeExports, netJSEngine, netBrowserHostExports, netInteropJSExports,
        getInternals, netSetInternals, netUpdateAllInternals, netUpdateModuleInternals,
        netTabulateBHE, netTabulateIJSE, netTabulateLE, netTabulateNBE, netTabulateRE
    ];
}
