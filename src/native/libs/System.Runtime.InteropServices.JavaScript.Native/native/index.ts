// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, InteropJavaScriptExports, JSFnHandle, JSMarshalerArguments } from "../interop/types";
import { InternalExchangeIndex } from "../types";
import { } from "./cross-linked"; // ensure ambient symbols are declared

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function SystemInteropJS_InvokeJSImportST(function_handle: JSFnHandle, args: JSMarshalerArguments) {
    // WASM-TODO implementation
    Logger.error("SystemInteropJS_InvokeJSImportST called");
    return - 1;
}

export function netInitializeModule(internals: InternalExchange): void {
    const interopJavaScriptNativeExportsLocal: InteropJavaScriptExports = {
    };
    netSetInternals(internals);
    internals[InternalExchangeIndex.InteropJavaScriptExportsTable] = netTabulateIJSE(interopJavaScriptNativeExportsLocal);
    const updates = internals[InternalExchangeIndex.InternalUpdatesCallbacks];
    if (!updates.includes(netUpdateModuleInternals)) updates.push(netUpdateModuleInternals);
    netUpdateAllInternals();
}
