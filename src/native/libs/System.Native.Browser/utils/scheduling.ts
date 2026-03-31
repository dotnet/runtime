// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { _ems_ } from "../../Common/JavaScript/ems-ambient";

export async function runBackgroundTimers(): Promise<void> {
    if (_ems_.ABORT || _ems_.DOTNET.isAborting) {
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

