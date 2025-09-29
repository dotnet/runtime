// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    dotnetAssert, dotnetLogger, Module,
    dotnetInternals, dotnetLoaderExports, dotnetApi, dotnetNativeBrowserExports, dotnetRuntimeExports, dotnetBrowserHostExports, dotnetInteropJSExports,
    dotnetGetInternals, dotnetUpdateInternals, dotnetUpdateInternalsSubscriber,
} from "../cross-module";

import { } from "../../Common/JavaScript/cross-linked";

// this is dummy function that references all cross-linked symbols, so that the bundler does not drop them as unused
export function crossLink() {
    return [
        dotnetAssert, dotnetLogger, Module,
        dotnetInternals, dotnetLoaderExports, dotnetApi, dotnetNativeBrowserExports, dotnetRuntimeExports, dotnetBrowserHostExports, dotnetInteropJSExports,
        dotnetGetInternals, dotnetUpdateInternals, dotnetUpdateInternalsSubscriber,
    ];
}
