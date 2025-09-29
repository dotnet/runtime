// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    dotnetAssert, dotnetLogger, Module,
    dotnetInternals, dotnetLoaderExports, dotnetApi, dotnetNativeBrowserExports, dotnetRuntimeExports, dotnetBrowserHostExports, dotnetInteropJSExports,
    dotnetGetInternals, dotnetSetInternals, dotnetUpdateAllInternals, dotnetUpdateModuleInternals,
} from "../cross-module";

import { } from "../../Common/JavaScript/cross-linked";

export function crossLink() {
    return [
        dotnetAssert, dotnetLogger, Module,
        dotnetInternals, dotnetLoaderExports, dotnetApi, dotnetNativeBrowserExports, dotnetRuntimeExports, dotnetBrowserHostExports, dotnetInteropJSExports,
        dotnetGetInternals, dotnetSetInternals, dotnetUpdateAllInternals, dotnetUpdateModuleInternals,
    ];
}
