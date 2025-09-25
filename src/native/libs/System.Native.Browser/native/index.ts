// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, NativeBrowserExports } from "../types";

export { SystemJS_RandomBytes } from "./crypto";

export function netInitializeModule(internals: InternalExchange): void {
    const nativeBrowserExportsLocal: NativeBrowserExports = {
    };
    netSetInternals(internals);
    internals.netNativeBrowserExportsTable = [...netTabulateNBE(nativeBrowserExportsLocal)];
    internals.netInternalUpdates.push(netUpdateModuleInternals);
    netUpdateAllInternals();
}


// see also `reserved` in `rollup.config.defines.js`
export * as cross from "../cross-module";
