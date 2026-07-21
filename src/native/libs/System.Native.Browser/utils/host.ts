// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { _ems_ } from "../../Common/JavaScript/ems-ambient";

export function getExitStatus(): new (exitCode: number) => any {
    return _ems_.ExitStatus as any;
}

export function abortPosix(exitCode: number, reason: any, nativeReady: boolean): void {
    try {
        _ems_.EXITSTATUS = exitCode;
        _ems_.DOTNET.isAborting = true;
        if (_ems_.dotnetBrowserUtilsExports.abortBackgroundTimers) {
            _ems_.dotnetBrowserUtilsExports.abortBackgroundTimers();
        }
        if (exitCode === 0 && nativeReady) {
            _ems_._exit(0);
            _ems_.ABORT = true;
            return;
        } else if (nativeReady) {
            _ems_.___funcs_on_exit = () => { };
            _ems_.ABORT = true;
            _ems_.___trap();
        } else {
            _ems_.___funcs_on_exit = () => { };
            _ems_.ABORT = true;
            _ems_.abort(reason);
        }
        throw reason;
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (error && (typeof error.status === "number" || error instanceof WebAssembly.RuntimeError)) {
            return;
        }
        throw error;
    }
}
