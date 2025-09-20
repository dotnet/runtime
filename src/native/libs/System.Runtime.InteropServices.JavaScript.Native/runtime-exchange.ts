// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalApis, RuntimeAPI, RuntimeExports } from "./types";
import { runtimeExports, runtimeExportsToTable, setInternals, updateInternals, updateInternalsImpl } from "./cross-module";
import { getAssemblyExports, setModuleImports } from "./interop";

export async function initialize(internals: InternalApis): Promise<void> {
    const runtimeApiFunctions: Partial<RuntimeAPI> = {
        getAssemblyExports,
        setModuleImports,
    };

    const runtimeExportsFunctions: RuntimeExports = {
    };
    setInternals(internals);
    Object.assign(internals.runtimeApi, runtimeApiFunctions);
    Object.assign(runtimeExports, runtimeExports);
    internals.runtimeExportsTable = [...runtimeExportsToTable(runtimeExportsFunctions)];
    internals.updates.push(updateInternalsImpl);
    updateInternals();
}
