// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, InteropJavaScriptExports, InteropJavaScriptExportsTable, JSFnHandle, JSMarshalerArguments } from "../interop/types";
import { InternalExchangeIndex } from "../types";
import { } from "./cross-linked"; // ensure ambient symbols are declared

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function SystemInteropJS_InvokeJSImportST(function_handle: JSFnHandle, args: JSMarshalerArguments) {
    // WASM-TODO implementation
    dotnetLogger.error("SystemInteropJS_InvokeJSImportST called");
    return - 1;
}

export function dotnetInitializeModule(internals: InternalExchange): void {
    internals[InternalExchangeIndex.InteropJavaScriptExportsTable] = interopJavaScriptExportsToTable({
    });
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function interopJavaScriptExportsToTable(map: InteropJavaScriptExports): InteropJavaScriptExportsTable {
        // keep in sync with interopJavaScriptExportsFromTable()
        return [
        ];
    }
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);
}
