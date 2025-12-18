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
        SystemInteropJS_CallDelegate: (args) => _SystemInteropJS_CallDelegate(args),
        SystemInteropJS_CompleteTask: (args) => _SystemInteropJS_CompleteTask(args),
        SystemInteropJS_ReleaseJSOwnedObjectByGCHandle: (args) => _SystemInteropJS_ReleaseJSOwnedObjectByGCHandle(args),
        SystemInteropJS_BindAssemblyExports: (args) => _SystemInteropJS_BindAssemblyExports(args),
        SystemInteropJS_CallJSExport: (methodHandle, args) => _SystemInteropJS_CallJSExport(methodHandle, args),
    });
    function interopJavaScriptExportsToTable(map: InteropJavaScriptExports): InteropJavaScriptExportsTable {
        // keep in sync with interopJavaScriptExportsFromTable()
        return [
            map.SystemInteropJS_GetManagedStackTrace,
            map.SystemInteropJS_CallDelegate,
            map.SystemInteropJS_CompleteTask,
            map.SystemInteropJS_ReleaseJSOwnedObjectByGCHandle,
            map.SystemInteropJS_BindAssemblyExports,
            map.SystemInteropJS_CallJSExport,
        ];
    }
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);
}
