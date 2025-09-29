// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, NativeBrowserExports, NativeBrowserExportsTable } from "../types";
import { InternalExchangeIndex } from "../types";

export { SystemJS_RandomBytes } from "./crypto";

export function dotnetInitializeModule(internals: InternalExchange): void {
    internals[InternalExchangeIndex.NativeBrowserExportsTable] = nativeBrowserExportsToTable({
    });
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function nativeBrowserExportsToTable(map:NativeBrowserExports):NativeBrowserExportsTable {
        // keep in sync with nativeBrowserExportsFromTable()
        return [
        ];
    }
}


// see also `reserved` in `rollup.config.defines.js`
export * as cross from "../cross-module";
