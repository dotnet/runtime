// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, RuntimeAPI, RuntimeExports, RuntimeExportsTable } from "./types";
import { InternalExchangeIndex } from "../types";
import { dotnetUpdateInternals, dotnetUpdateInternalsSubscriber } from "./cross-module";
import { ENVIRONMENT_IS_NODE } from "./per-module";

export function dotnetInitializeModule(internals: InternalExchange): void {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");
    const runtimeApiLocal: Partial<RuntimeAPI> = {
        getAssemblyExports,
        setModuleImports,
    };
    const runtimeApi = internals[InternalExchangeIndex.RuntimeAPI];
    if (typeof runtimeApi !== "object") throw new Error("Expected internals to have RuntimeAPI");
    Object.assign(runtimeApi, runtimeApiLocal);

    internals[InternalExchangeIndex.RuntimeExportsTable] = runtimeExportsToTable({
    });
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function runtimeExportsToTable(map:RuntimeExports):RuntimeExportsTable {
        // keep in sync with runtimeExportsFromTable()
        return [
        ];
    }
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function getAssemblyExports(assemblyName: string): Promise<any> {
    throw new Error("Not implemented");
    return ENVIRONMENT_IS_NODE; // dummy
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function setModuleImports(moduleName: string, moduleImports: any): void {
    throw new Error("Not implemented");
}

