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
    const interopJavaScriptNativeExportsLocal: InteropJavaScriptExports = {
    };
    dotnetSetInternals(internals);
    internals[InternalExchangeIndex.InteropJavaScriptExportsTable] = tabulateInteropJavaScriptExports(interopJavaScriptNativeExportsLocal);
    const updates = internals[InternalExchangeIndex.InternalUpdatesCallbacks];
    if (!updates.includes(dotnetUpdateModuleInternals)) updates.push(dotnetUpdateModuleInternals);
    dotnetUpdateAllInternals();

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function tabulateInteropJavaScriptExports(map:InteropJavaScriptExports):InteropJavaScriptExportsTable {
        // keep in sync with dotnetUpdateModuleInternals()
        return [
        ];
    }
}
