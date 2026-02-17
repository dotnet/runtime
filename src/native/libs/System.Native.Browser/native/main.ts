// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { _ems_ } from "../../Common/JavaScript/ems-ambient";

export function SystemJS_ResolveMainPromise(exitCode: number): void {
    if (_ems_.dotnetLoaderExports.resolveRunMainPromise) {
        _ems_.dotnetLoaderExports.resolveRunMainPromise(exitCode);
    } else {
        // this is for corerun, which does not use the promise
        _ems_.EXITSTATUS = exitCode;
        _ems_.ABORT = true;
        _ems_.dotnetBrowserUtilsExports.abortBackgroundTimers();
        globalThis.setTimeout(() => {
            _ems_.dotnetBrowserUtilsExports.abortPosix(exitCode, null, true);
        }, 0);
    }
}

export function SystemJS_RejectMainPromise(messagePtr: number, messageLength: number, stackTracePtr: number, stackTraceLength: number): void {
    const message = _ems_.dotnetBrowserUtilsExports.utf16ToString(messagePtr, messagePtr + messageLength * 2);
    const stackTrace = _ems_.dotnetBrowserUtilsExports.utf16ToString(stackTracePtr, stackTracePtr + stackTraceLength * 2);
    const error = new Error(message);
    error.stack = stackTrace;
    if (_ems_.dotnetLoaderExports.rejectRunMainPromise) {
        _ems_.dotnetLoaderExports.rejectRunMainPromise(error);
    } else {
        // this is for corerun, which does not use the promise
        _ems_.EXITSTATUS = -1;
        _ems_.ABORT = true;
        _ems_.dotnetBrowserUtilsExports.abortBackgroundTimers();
        globalThis.setTimeout(() => {
            _ems_.dotnetBrowserUtilsExports.abortPosix(-1, null, true);
        }, 0);
        throw error;
    }
}

export function SystemJS_ConsoleClear(): void {
    // eslint-disable-next-line no-console
    console.clear();
}
