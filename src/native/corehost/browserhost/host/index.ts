// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, BrowserHostExports, RuntimeAPI, BrowserHostExportsTable } from "./types";
import { InternalExchangeIndex } from "./types";
import { } from "./cross-linked"; // ensure ambient symbols are declared

import { runMain, runMainAndExit, registerDllBytes } from "./host";

export function dotnetInitializeModule(internals: InternalExchange): void {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");
    const runtimeApiLocal: Partial<RuntimeAPI> = {
        runMain,
        runMainAndExit,
    };
    const runtimeApi = internals[InternalExchangeIndex.RuntimeAPI];
    if (typeof runtimeApi !== "object") throw new Error("Expected internals to have RuntimeAPI");
    Object.assign(runtimeApi, runtimeApiLocal);

    internals[InternalExchangeIndex.BrowserHostExportsTable] = browserHostExportsToTable({
        registerDllBytes,
    });
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);
    function browserHostExportsToTable(map:BrowserHostExports):BrowserHostExportsTable {
        // keep in sync with browserHostExportsFromTable()
        return [
            map.registerDllBytes,
        ];
    }
}

export { BrowserHost_ExternalAssemblyProbe, BrowserHost_ResolveMain, BrowserHost_RejectMain } from "./host";
