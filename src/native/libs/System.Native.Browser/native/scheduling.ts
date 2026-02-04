// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { _ems_ } from "../../Common/JavaScript/ems-ambient";

export function SystemJS_ScheduleTimer(shortestDueTimeMs: number): void {
    if (_ems_.DOTNET.lastScheduledTimerId) {
        globalThis.clearTimeout(_ems_.DOTNET.lastScheduledTimerId);
        _ems_.runtimeKeepalivePop();
        _ems_.DOTNET.lastScheduledTimerId = undefined;
    }
    _ems_.DOTNET.lastScheduledTimerId = _ems_.safeSetTimeout(SystemJS_ScheduleTimerTick, shortestDueTimeMs);

    function SystemJS_ScheduleTimerTick(): void {
        try {
            _ems_.DOTNET.lastScheduledTimerId = undefined;
            _ems_._SystemJS_ExecuteTimerCallback();
        } catch (err) {
            _ems_.dotnetApi.exit(1, err);
        }
    }
}

export function SystemJS_ScheduleBackgroundJob(): void {
    if (_ems_.DOTNET.lastScheduledThreadPoolId) {
        globalThis.clearTimeout(_ems_.DOTNET.lastScheduledThreadPoolId);
        _ems_.runtimeKeepalivePop();
        _ems_.DOTNET.lastScheduledThreadPoolId = undefined;
    }
    _ems_.DOTNET.lastScheduledThreadPoolId = _ems_.safeSetTimeout(SystemJS_ScheduleBackgroundJobTick, 0);

    function SystemJS_ScheduleBackgroundJobTick(): void {
        try {
            _ems_.DOTNET.lastScheduledThreadPoolId = undefined;
            _ems_._SystemJS_ExecuteBackgroundJobCallback();
        } catch (err) {
            _ems_.dotnetApi.exit(1, err);
        }
    }
}

export function SystemJS_ScheduleFinalization(): void {
    if (_ems_.DOTNET.lastScheduledFinalizationId) {
        globalThis.clearTimeout(_ems_.DOTNET.lastScheduledFinalizationId);
        _ems_.runtimeKeepalivePop();
        _ems_.DOTNET.lastScheduledFinalizationId = undefined;
    }
    _ems_.DOTNET.lastScheduledFinalizationId = _ems_.safeSetTimeout(SystemJS_ScheduleFinalizationTick, 0);

    function SystemJS_ScheduleFinalizationTick(): void {
        try {
            _ems_.DOTNET.lastScheduledFinalizationId = undefined;
            _ems_._SystemJS_ExecuteFinalizationCallback();
        } catch (err) {
            _ems_.dotnetApi.exit(1, err);
        }
    }
}
