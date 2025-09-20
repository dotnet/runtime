// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JSFnHandle, JSMarshalerArguments } from "../interop/types";

import { Logger } from "../cross-module";

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function SystemInteropJS_InvokeJSImportST(function_handle: JSFnHandle, args: JSMarshalerArguments) {
    // WASMTODO implementation
    Logger.error("SystemInteropJS_InvokeJSImportST called");
    return - 1;
}
SystemInteropJS_InvokeJSImportST["__deps"] = ["loadedAssemblies"];
