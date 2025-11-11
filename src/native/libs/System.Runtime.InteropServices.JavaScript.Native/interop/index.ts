// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, RuntimeAPI, RuntimeExports, RuntimeExportsTable } from "./types";
import { InternalExchangeIndex } from "../types";
import { dotnetUpdateInternals, dotnetUpdateInternalsSubscriber } from "./cross-module";
import { bindJSImportST, invokeJSFunction, invokeJSImportST, setModuleImports } from "./invoke-js";
import { getAssemblyExports } from "./invoke-cs";
import { initializeMarshalersToJs, resolveOrRejectPromise } from "./marshal-to-js";
import { initializeMarshalersToCs } from "./marshal-to-cs";
import { releaseCSOwnedObject } from "./gc-handles";
import { cancelPromise } from "./cancelable-promise";

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
        bindJSImportST,
        invokeJSImportST,
        releaseCSOwnedObject,
        resolveOrRejectPromise,
        cancelPromise,
        invokeJSFunction,
    });
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);

    initializeMarshalersToJs();
    initializeMarshalersToCs();

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function runtimeExportsToTable(map: RuntimeExports): RuntimeExportsTable {
        // keep in sync with runtimeExportsFromTable()
        return [
            bindJSImportST,
            invokeJSImportST,
            releaseCSOwnedObject,
            resolveOrRejectPromise,
            cancelPromise,
            invokeJSFunction,
        ];
    }
}
