// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JSMarshalerArguments, GCHandle, MarshalerToCs, MarshalerToJs, CSFnHandle } from "./types";

import { dotnetAssert, dotnetInteropJSExports, Module } from "./cross-module";
import { allocStackFrame, getArg, isArgsException, setArgType, setGcHandle } from "./marshal";
import { marshalExceptionToCs, marshalStringToCs } from "./marshal-to-cs";
import { beginMarshalTaskToJs, endMarshalTaskToJs, marshalExceptionToJs, marshalInt32ToJs, marshalStringToJs } from "./marshal-to-js";
import { assertJsInterop, assertRuntimeRunning, isRuntimeRunning } from "./utils";
import { MarshalerType } from "./types";

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

export function releaseJsOwnedObjectByGcHandle(gcHandle: GCHandle) {
    dotnetAssert.check(gcHandle, "Must be valid gcHandle");
    assertRuntimeRunning();
    const sp = Module.stackSave();
    try {
        const size = 3;
        const args = allocStackFrame(size);
        const arg1 = getArg(args, 2);
        setArgType(arg1, MarshalerType.Object);
        setGcHandle(arg1, gcHandle);
        // this must stay synchronous for freeGcvHandle sake, to not use-after-free
        // also on JSWebWorker, because the message could arrive after the worker is terminated and the GCHandle of JSProxyContext is already freed
        dotnetInteropJSExports.SystemInteropJS_ReleaseJSOwnedObjectByGCHandle(args);
    } finally {
        if (isRuntimeRunning()) Module.stackRestore(sp);

    }
}

export function callDelegate(callbackGcHandle: GCHandle, arg1Js: any, arg2Js: any, arg3Js: any, resConverter?: MarshalerToJs, arg1Converter?: MarshalerToCs, arg2Converter?: MarshalerToCs, arg3Converter?: MarshalerToCs) {
    assertRuntimeRunning();

    const sp = Module.stackSave();
    try {
        const size = 6;
        const args = allocStackFrame(size);

        const arg1 = getArg(args, 2);
        setArgType(arg1, MarshalerType.Object);
        setGcHandle(arg1, callbackGcHandle);
        // payload arg numbers are shifted by one, the real first is a gcHandle of the callback

        if (arg1Converter) {
            const arg2 = getArg(args, 3);
            arg1Converter(arg2, arg1Js);
        }
        if (arg2Converter) {
            const arg3 = getArg(args, 4);
            arg2Converter(arg3, arg2Js);
        }
        if (arg3Converter) {
            const arg4 = getArg(args, 5);
            arg3Converter(arg4, arg3Js);
        }

        dotnetInteropJSExports.SystemInteropJS_CallDelegate(args);
        if (isArgsException(args)) {
            const exc = getArg(args, 0);
            throw marshalExceptionToJs(exc);
        }

        if (resConverter) {
            const res = getArg(args, 1);
            return resConverter(res);
        }
    } finally {
        if (isRuntimeRunning()) Module.stackRestore(sp);

    }
}

export function completeTask(holderGcHandle: GCHandle, error?: any, data?: any, resConverter?: MarshalerToCs) {
    assertRuntimeRunning();
    const sp = Module.stackSave();
    try {
        const size = 5;
        const args = allocStackFrame(size);
        const arg1 = getArg(args, 2);
        setArgType(arg1, MarshalerType.Object);
        setGcHandle(arg1, holderGcHandle);
        const arg2 = getArg(args, 3);
        if (!error) {
            try {
                setArgType(arg2, MarshalerType.None);
                const arg3 = getArg(args, 4);
                dotnetAssert.check(resConverter, "resConverter missing");
                resConverter(arg3, data);
            } catch (e) {
                error = e;
            }
        }
        if (error) {
            marshalExceptionToCs(arg2, error);
        }
        dotnetInteropJSExports.SystemInteropJS_CompleteTask(args);
    } finally {
        if (isRuntimeRunning()) Module.stackRestore(sp);

    }
}

export function bindAssemblyExports(assemblyName: string): Promise<void> {
    assertRuntimeRunning();
    const sp = Module.stackSave();
    try {
        const size = 3;
        const args = allocStackFrame(size);
        const res = getArg(args, 1);
        const arg1 = getArg(args, 2);
        marshalStringToCs(arg1, assemblyName);

        // because this is async, we could pre-allocate the promise
        let promise = beginMarshalTaskToJs(res, MarshalerType.TaskPreCreated);

        dotnetInteropJSExports.SystemInteropJS_BindAssemblyExports(args);
        if (isArgsException(args)) {
            // TODO free pre-created promise
            const exc = getArg(args, 0);
            throw marshalExceptionToJs(exc);
        }

        // in case the C# side returned synchronously
        promise = endMarshalTaskToJs(args, marshalInt32ToJs, promise);

        if (promise === null || promise === undefined) {
            promise = Promise.resolve();
        }
        return promise;
    } finally {
        // synchronously
        if (isRuntimeRunning()) Module.stackRestore(sp);
    }
}

export function invokeJSExport(methodHandle: CSFnHandle, args: JSMarshalerArguments): void {
    assertJsInterop();
    dotnetInteropJSExports.SystemInteropJS_CallJSExport(methodHandle, args);
    if (isArgsException(args)) {
        const exc = getArg(args, 0);
        throw marshalExceptionToJs(exc);
    }
}
