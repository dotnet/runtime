// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, InteropJavaScriptNativeExports, JSFnHandle, JSMarshalerArguments } from "../interop/types";
import { } from "./cross-linked"; // ensure ambient symbols are declared

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function SystemInteropJS_InvokeJSImportST(function_handle: JSFnHandle, args: JSMarshalerArguments) {
    // WASM-TODO implementation
    Logger.error("SystemInteropJS_InvokeJSImportST called");
    return - 1;
}

export function netInitializeModule(internals: InternalExchange): void {
    const interopJavaScriptNativeExportsLocal: InteropJavaScriptNativeExports = {
    };
    netSetInternals(internals);
    internals.netInteropJSExportsTable = [...netTabulateJSNE(interopJavaScriptNativeExportsLocal)];
    internals.netInternalUpdates.push(netUpdateModuleInternals);
    netUpdateAllInternals();
}
