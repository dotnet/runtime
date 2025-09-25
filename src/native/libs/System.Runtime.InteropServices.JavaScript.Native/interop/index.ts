// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, RuntimeAPI, RuntimeExports } from "./types";
import { InternalExchangeIndex } from "../types";
import { netSetInternals, netUpdateAllInternals, netUpdateModuleInternals, netTabulateRE } from "./cross-module";

export function netInitializeModule(internals: InternalExchange): void {
    const runtimeApiLocal: Partial<RuntimeAPI> = {
        getAssemblyExports,
        setModuleImports,
    };
    const runtimeExportsLocal: RuntimeExports = {
    };
    netSetInternals(internals);
    Object.assign(internals[InternalExchangeIndex.RuntimeAPI], runtimeApiLocal);
    internals[InternalExchangeIndex.RuntimeExportsTable] = netTabulateRE(runtimeExportsLocal);
    internals[InternalExchangeIndex.InternalUpdatesCallbacks].push(netUpdateModuleInternals);
    netUpdateAllInternals();
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function getAssemblyExports(assemblyName: string): Promise<any> {
    throw new Error("Not implemented");
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function setModuleImports(moduleName: string, moduleImports: any): void {
    throw new Error("Not implemented");
}

