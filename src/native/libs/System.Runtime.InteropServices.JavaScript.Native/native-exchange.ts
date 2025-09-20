// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalApis, InteropExports } from "./types";
import { interopExportsToTable, interopExports, updateInternals, updateInternalsImpl } from "./cross-module";

export async function initialize(internals: InternalApis): Promise<void> {
    const interopExportsFunctions: InteropExports = {
    };
    Object.assign(interopExports, interopExportsFunctions);
    internals.interopExportsTable = [...interopExportsToTable(interopExportsFunctions)];
    internals.updates.push(updateInternalsImpl);
    updateInternals();
}
