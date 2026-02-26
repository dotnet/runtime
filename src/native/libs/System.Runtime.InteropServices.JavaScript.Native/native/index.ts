// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, InteropJavaScriptExports, InteropJavaScriptExportsTable, JSFnHandle, JSFunctionSignature, JSMarshalerArguments, VoidPtr } from "../interop/types";
import type { GCHandle, JSHandle, CSFnHandle } from "../types";
import { InternalExchangeIndex } from "../types";
import { _ems_ } from "../../Common/JavaScript/ems-ambient";

import GitHash from "consts:gitHash";

export function SystemInteropJS_BindJSImportST(signature: JSFunctionSignature): VoidPtr {
    return _ems_.dotnetRuntimeExports.bindJSImportST(signature);
}

export function SystemInteropJS_InvokeJSImportST(functionHandle: JSFnHandle, args: JSMarshalerArguments): void {
    _ems_.dotnetRuntimeExports.invokeJSImportST(functionHandle, args);
}

export function SystemInteropJS_ReleaseCSOwnedObject(jsHandle: JSHandle): void {
    _ems_.dotnetRuntimeExports.releaseCSOwnedObject(jsHandle);
}

export function SystemInteropJS_ResolveOrRejectPromise(args: JSMarshalerArguments): void {
    _ems_.dotnetRuntimeExports.resolveOrRejectPromise(args);
}

export function SystemInteropJS_CancelPromise(taskHolderGCHandle: GCHandle): void {
    _ems_.dotnetRuntimeExports.cancelPromise(taskHolderGCHandle);
}

export function SystemInteropJS_InvokeJSFunction(functionJSSHandle: JSHandle, args: JSMarshalerArguments): void {
    _ems_.dotnetRuntimeExports.invokeJSFunction(functionJSSHandle, args);
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
        SystemInteropJS_GetManagedStackTrace: (args: JSMarshalerArguments) => _ems_._SystemInteropJS_GetManagedStackTrace(args),
        SystemInteropJS_CallDelegate: (args: JSMarshalerArguments) => _ems_._SystemInteropJS_CallDelegate(args),
        SystemInteropJS_CompleteTask: (args: JSMarshalerArguments) => _ems_._SystemInteropJS_CompleteTask(args),
        SystemInteropJS_ReleaseJSOwnedObjectByGCHandle: (args: JSMarshalerArguments) => _ems_._SystemInteropJS_ReleaseJSOwnedObjectByGCHandle(args),
        SystemInteropJS_BindAssemblyExports: (args: JSMarshalerArguments) => _ems_._SystemInteropJS_BindAssemblyExports(args),
        SystemInteropJS_CallJSExport: (methodHandle: CSFnHandle, args: JSMarshalerArguments) => _ems_._SystemInteropJS_CallJSExport(methodHandle, args),
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
    _ems_.dotnetUpdateInternals(internals, _ems_.dotnetUpdateInternalsSubscriber);
}
