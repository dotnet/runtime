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

export function runBackgroundTimers(): void {
    try {
        _ems_._SystemJS_ExecuteTimerCallback();
        _ems_._SystemJS_ExecuteBackgroundJobCallback();
        _ems_._SystemJS_ExecuteFinalizationCallback();
    } catch (err) {
        _ems_.dotnetApi.exit(1, err);
    }
}

export function abortBackgroundTimers(): void {
    if (_ems_.DOTNET.lastScheduledTimerId) {
        globalThis.clearTimeout(_ems_.DOTNET.lastScheduledTimerId);
        _ems_.runtimeKeepalivePop();
        _ems_.DOTNET.lastScheduledTimerId = undefined;
    }
    if (_ems_.DOTNET.lastScheduledThreadPoolId) {
        globalThis.clearTimeout(_ems_.DOTNET.lastScheduledThreadPoolId);
        _ems_.runtimeKeepalivePop();
        _ems_.DOTNET.lastScheduledThreadPoolId = undefined;
    }
}

export function abortPosix(exitCode: number, reason: any, nativeReady: boolean): void {
    _ems_.ABORT = true;
    _ems_.EXITSTATUS = exitCode;
    try {
        if (nativeReady) {
            _ems_.___trap();
        } else {
            _ems_.abort(reason);
        }
        throw reason;
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (error.status === undefined) {
            _ems_.dotnetApi.exit(1, error);
            throw error;
        }
    }
}
