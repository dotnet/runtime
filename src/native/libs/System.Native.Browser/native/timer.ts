// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { } from "./cross-linked"; // ensure ambient symbols are declared

export function SystemJS_ScheduleTimer(shortestDueTimeMs: number): void {
    if (DOTNET.lastScheduledTimerId) {
        globalThis.clearTimeout(DOTNET.lastScheduledTimerId);
        Module.runtimeKeepalivePop();
        DOTNET.lastScheduledTimerId = undefined;
    }
    DOTNET.lastScheduledTimerId = safeSetTimeout(SystemJS_ScheduleTimerTick, shortestDueTimeMs);

    function SystemJS_ScheduleTimerTick(): void {
        DOTNET.lastScheduledTimerId = undefined;
        _SystemJS_ExecuteTimerCallback();
    }
}
SystemJS_ScheduleTimer["__deps"] = ["SystemJS_ExecuteTimerCallback"];

export function SystemJS_ScheduleBackgroundJob(): void {
    if (DOTNET.lastScheduledThreadPoolId) {
        globalThis.clearTimeout(DOTNET.lastScheduledThreadPoolId);
        Module.runtimeKeepalivePop();
        DOTNET.lastScheduledThreadPoolId = undefined;
    }
    DOTNET.lastScheduledThreadPoolId = safeSetTimeout(SystemJS_ScheduleBackgroundJobTick, 0);

    function SystemJS_ScheduleBackgroundJobTick(): void {
        DOTNET.lastScheduledThreadPoolId = undefined;
        _SystemJS_ExecuteBackgroundJobCallback();
    }
}
SystemJS_ScheduleBackgroundJob["__deps"] = ["SystemJS_ExecuteBackgroundJobCallback"];
