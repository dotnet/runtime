// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, NativeBrowserExports } from "../types";
import { InternalExchangeIndex } from "../types";

export { SystemJS_RandomBytes } from "./crypto";

export function netInitializeModule(internals: InternalExchange): void {
    const nativeBrowserExportsLocal: NativeBrowserExports = {
    };
    netSetInternals(internals);
    internals[InternalExchangeIndex.NativeBrowserExportsTable] = netTabulateNBE(nativeBrowserExportsLocal);
    const updates = internals[InternalExchangeIndex.InternalUpdatesCallbacks];
    if (!updates.includes(netUpdateModuleInternals)) updates.push(netUpdateModuleInternals);
    netUpdateAllInternals();
}


// see also `reserved` in `rollup.config.defines.js`
export * as cross from "../cross-module";
