// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetInteropJSExports, Module } from "./cross-module";
import { allocStackFrame, getArg, setArgType, setGcHandle as setGcHandle } from "./marshal";
import { marshalStringToJs } from "./marshal-to-js";
import { MarshalerType, type GCHandle, type MarshalerToCs, type MarshalerToJs } from "./types";
import { assertRuntimeRunning, isRuntimeRunning } from "./utils";

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function releaseJsOwnedObjectByGcHandle(gcHandle: GCHandle) {
    // TODO-WASM
}

export function getManagedStackTrace(exceptionGCHandle: GCHandle): string {
    assertRuntimeRunning();
    const sp = Module.stackSave();
    try {
        const size = 3;
        const args = allocStackFrame(size);

        const arg1 = getArg(args, 2);
        setArgType(arg1, MarshalerType.Exception);
        setGcHandle(arg1, exceptionGCHandle);

        dotnetInteropJSExports.SystemInteropJS_GetManagedStackTrace(args);

        const res = getArg(args, 1);
        return marshalStringToJs(res)!;
    } finally {
        if (isRuntimeRunning()) Module.stackRestore(sp);
    }
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function callDelegate(callbackGcHandle: GCHandle, arg1Js: any, arg2Js: any, arg3Js: any, resConverter?: MarshalerToJs, arg1Converter?: MarshalerToCs, arg2Converter?: MarshalerToCs, arg3Converter?: MarshalerToCs) {
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function completeTask(holder_gc_handle: GCHandle, error?: any, data?: any, res_converter?: MarshalerToCs) {

}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function bindAssemblyExports(assemblyName: string): Promise<void> {
    // TODO-WASM
    return Promise.resolve();
}
