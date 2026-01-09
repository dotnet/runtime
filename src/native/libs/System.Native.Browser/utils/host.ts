// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";
import { _ems_ } from "../../Common/JavaScript/ems-ambient";
// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function setEnvironmentVariable(name: string, value: string): void {
    throw new Error("Not implemented");
}

export function getExitStatus(): new (exitCode: number) => any {
    return _ems_.ExitStatus as any;
}

export function abortTimers(): void {
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

export function abortPosix(exitCode: number): void {
    _ems_.ABORT = true;
    _ems_.EXITSTATUS = exitCode;
    try {
        if (BuildConfiguration === "Debug") {
            _ems_._exit(exitCode, true);
        } else {
            _ems_._emscripten_force_exit(exitCode);
        }
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (error.status === undefined) {
            _ems_.dotnetApi.exit(1, error);
            throw error;
        }
    }
}
