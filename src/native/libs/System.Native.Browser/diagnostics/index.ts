// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticsExportsTable, InternalExchange, DiagnosticsExports } from "./types";
import { InternalExchangeIndex } from "../types";
import { dotnetUpdateInternals, dotnetUpdateInternalsSubscriber } from "./cross-module";
import { registerExit } from "./exit";
import { symbolicateStackTrace } from "./symbolicate";
import { installLoggingProxy } from "./console-proxy";

export function dotnetInitializeModule(internals: InternalExchange): void {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");

    internals[InternalExchangeIndex.DiagnosticsExportsTable] = diagnosticsExportsToTable({
        symbolicateStackTrace,
    });
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);

    registerExit();
    installLoggingProxy();

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function diagnosticsExportsToTable(map: DiagnosticsExports): DiagnosticsExportsTable {
        // keep in sync with diagnosticsExportsFromTable()
        return [
            map.symbolicateStackTrace,
        ];
    }
}
