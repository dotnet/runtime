// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, RuntimeAPI, RuntimeExports, RuntimeExportsTable } from "./types";
import { InternalExchangeIndex } from "../types";
import { dotnetSetInternals, dotnetUpdateAllInternals, dotnetUpdateModuleInternals } from "./cross-module";
import { stringToUTF16, stringToUTF16Ptr, utf16ToString } from "./strings";

export function dotnetInitializeModule(internals: InternalExchange): void {
    const runtimeApiLocal: Partial<RuntimeAPI> = {
        getAssemblyExports,
        setModuleImports,
    };
    const runtimeExportsLocal: RuntimeExports = {
        utf16ToString,
        stringToUTF16,
        stringToUTF16Ptr,
    };
    dotnetSetInternals(internals);
    Object.assign(internals[InternalExchangeIndex.RuntimeAPI], runtimeApiLocal);
    internals[InternalExchangeIndex.RuntimeExportsTable] = tabulateRuntimeExports(runtimeExportsLocal);
    internals[InternalExchangeIndex.InternalUpdatesCallbacks].push(dotnetUpdateModuleInternals);
    dotnetUpdateAllInternals();
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function tabulateRuntimeExports(map:RuntimeExports):RuntimeExportsTable {
        // keep in sync with dotnetUpdateModuleInternals()
        return [
            map.utf16ToString,
            map.stringToUTF16,
            map.stringToUTF16Ptr,
        ];
    }
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function getAssemblyExports(assemblyName: string): Promise<any> {
    throw new Error("Not implemented");
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function setModuleImports(moduleName: string, moduleImports: any): void {
    throw new Error("Not implemented");
}

