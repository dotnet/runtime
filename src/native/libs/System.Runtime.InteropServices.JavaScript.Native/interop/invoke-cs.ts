// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { bindAssemblyExports } from "./managed-exports";
import { assertJsInterop } from "./utils";

export const exportsByAssembly: Map<string, any> = new Map();

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function getAssemblyExports(assemblyName: string): Promise<any> {
    assertJsInterop();
    const result = exportsByAssembly.get(assemblyName);
    if (!result) {
        await bindAssemblyExports(assemblyName);
    }

    return exportsByAssembly.get(assemblyName) || {};
}
