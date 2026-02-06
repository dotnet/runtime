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
    if (_ems_.ABORT) {
        // runtime is shutting down
        return;
    }
    try {
        _ems_._SystemJS_ExecuteTimerCallback();
        _ems_._SystemJS_ExecuteBackgroundJobCallback();
        _ems_._SystemJS_ExecuteFinalizationCallback();
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (!error || typeof error.status !== "number") {
            _ems_.dotnetApi.exit(1, error);
            throw error;
        }
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
    if (_ems_.DOTNET.lastScheduledFinalizationId) {
        globalThis.clearTimeout(_ems_.DOTNET.lastScheduledFinalizationId);
        _ems_.runtimeKeepalivePop();
        _ems_.DOTNET.lastScheduledFinalizationId = undefined;
    }
}

export function abortPosix(exitCode: number, reason: any, nativeReady: boolean): void {
    try {
        _ems_.ABORT = true;
        _ems_.EXITSTATUS = exitCode;
        if (exitCode === 0 && nativeReady) {
            _ems_._exit(0);
            return;
        } else if (nativeReady) {
            _ems_.___trap();
        } else {
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
