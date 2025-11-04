// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { } from "./cross-linked"; // ensure ambient symbols are declared

export function SystemJS_ResolveMainPromise(exitCode: number) {
    if (dotnetLoaderExports.resolveRunMainPromise) {
        dotnetLoaderExports.resolveRunMainPromise(exitCode);
    } else {
        // this is for corerun, which does not use the promise
        Module.exitJS(exitCode, true);
    }
}

export function SystemJS_RejectMainPromise(messagePtr: number, messageLength: number, stackTracePtr: number, stackTraceLength: number) {
    const message = dotnetBrowserUtilsExports.utf16ToString(messagePtr, messagePtr + messageLength * 2);
    const stackTrace = dotnetBrowserUtilsExports.utf16ToString(stackTracePtr, stackTracePtr + stackTraceLength * 2);
    const error = new Error(message);
    error.stack = stackTrace;
    if (dotnetLoaderExports.rejectRunMainPromise) {
        dotnetLoaderExports.rejectRunMainPromise(error);
    } else {
        // this is for corerun, which does not use the promise
        throw error;
    }
}
