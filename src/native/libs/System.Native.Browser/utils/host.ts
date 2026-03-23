// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { _ems_ } from "../../Common/JavaScript/ems-ambient";

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function setEnvironmentVariable(name: string, value: string): void {
    // TODO-WASM: implement setEnvironmentVariable
    throw new Error("Not implemented");
}

export function getExitStatus(): new (exitCode: number) => any {
    return _ems_.ExitStatus as any;
}

export function abortPosix(exitCode: number, reason: any, nativeReady: boolean): void {
    try {
        _ems_.EXITSTATUS = exitCode;
        _ems_.DOTNET.isAborting = true;
        if (exitCode === 0 && nativeReady) {
            _ems_._exit(0);
            _ems_.ABORT = true;
            return;
        } else if (nativeReady) {
            _ems_.ABORT = true;
            _ems_.___trap();
        } else {
            _ems_.ABORT = true;
            _ems_.abort(reason);
        }
        throw reason;
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (typeof error === "object" && (typeof error.status === "number" || error instanceof WebAssembly.RuntimeError)) {
            return;
        }
        throw error;
    }
}
