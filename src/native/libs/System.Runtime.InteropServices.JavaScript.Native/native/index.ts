// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, InteropJavaScriptExports, InteropJavaScriptExportsTable, JSFnHandle, JSFunctionSignature, JSMarshalerArguments, VoidPtr } from "../interop/types";
import { GCHandle, InternalExchangeIndex, JSHandle } from "../types";
import { } from "./cross-linked"; // ensure ambient symbols are declared

export function SystemInteropJS_BindJSImportST(signature: JSFunctionSignature): VoidPtr {
    return dotnetRuntimeExports.bindJSImportST(signature);
}

export function SystemInteropJS_InvokeJSImportST(functionHandle: JSFnHandle, args: JSMarshalerArguments): void {
    dotnetRuntimeExports.invokeJSImportST(functionHandle, args);
}

export function SystemInteropJS_ReleaseCSOwnedObject(jsHandle: JSHandle): void {
    dotnetRuntimeExports.releaseCSOwnedObject(jsHandle);
}

export function SystemInteropJS_ResolveOrRejectPromise(args: JSMarshalerArguments): void {
    dotnetRuntimeExports.resolveOrRejectPromise(args);
}

export function SystemInteropJS_CancelPromise(taskHolderGCHandle: GCHandle): void {
    dotnetRuntimeExports.cancelPromise(taskHolderGCHandle);
}

export function SystemInteropJS_InvokeJSFunction(functionJSSHandle: JSHandle, args: JSMarshalerArguments): void {
    dotnetRuntimeExports.invokeJSFunction(functionJSSHandle, args);
}

export function dotnetInitializeModule(internals: InternalExchange): void {
    internals[InternalExchangeIndex.InteropJavaScriptExportsTable] = interopJavaScriptExportsToTable({
        SystemInteropJS_GetManagedStackTrace: (args) => _SystemInteropJS_GetManagedStackTrace(args),
    });
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function interopJavaScriptExportsToTable(map: InteropJavaScriptExports): InteropJavaScriptExportsTable {
        // keep in sync with interopJavaScriptExportsFromTable()
        return [
            map.SystemInteropJS_GetManagedStackTrace,
        ];
    }
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);
}
