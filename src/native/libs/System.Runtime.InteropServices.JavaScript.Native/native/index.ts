// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, InteropJavaScriptExports, InteropJavaScriptExportsTable, JSFnHandle, JSMarshalerArguments } from "../interop/types";
import { InternalExchangeIndex } from "../types";
import { _ems_ } from "../../Common/JavaScript/ems-ambient";

import GitHash from "consts:gitHash";

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function SystemInteropJS_InvokeJSImportST(function_handle: JSFnHandle, args: JSMarshalerArguments) {
    // WASM-TODO implementation
    _ems_.dotnetLogger.error("SystemInteropJS_InvokeJSImportST called");
    return - 1;
}

export const gitHash = GitHash;
export function dotnetInitializeModule(internals: InternalExchange): void {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");
    const runtimeApi = internals[InternalExchangeIndex.RuntimeAPI];
    if (typeof runtimeApi !== "object") throw new Error("Expected internals to have RuntimeAPI");

    if (runtimeApi.runtimeBuildInfo.gitHash && runtimeApi.runtimeBuildInfo.gitHash !== _ems_.DOTNET_INTEROP.gitHash) {
        throw new Error(`Mismatched git hashes between loader and runtime. Loader: ${runtimeApi.runtimeBuildInfo.gitHash}, DOTNET_INTEROP: ${_ems_.DOTNET_INTEROP.gitHash}`);
    }

    internals[InternalExchangeIndex.InteropJavaScriptExportsTable] = interopJavaScriptExportsToTable({
    });
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function interopJavaScriptExportsToTable(map: InteropJavaScriptExports): InteropJavaScriptExportsTable {
        // keep in sync with interopJavaScriptExportsFromTable()
        return [
        ];
    }
    _ems_.dotnetUpdateInternals(internals, _ems_.dotnetUpdateInternalsSubscriber);
}
