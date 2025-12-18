// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { } from "../../Common/JavaScript/cross-linked";
import type { CSFnHandle, JSMarshalerArguments } from "../interop/types";

declare global {
    export function _SystemInteropJS_GetManagedStackTrace(args: JSMarshalerArguments): void;
    export function _SystemInteropJS_CallDelegate(args: JSMarshalerArguments): void;
    export function _SystemInteropJS_CompleteTask(args: JSMarshalerArguments): void;
    export function _SystemInteropJS_ReleaseJSOwnedObjectByGCHandle(args: JSMarshalerArguments): void;
    export function _SystemInteropJS_BindAssemblyExports(args: JSMarshalerArguments): void;
    export function _SystemInteropJS_CallJSExport(methodHandle: CSFnHandle, args: JSMarshalerArguments): void;
}
