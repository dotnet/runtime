// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { } from "./cross-linked"; // ensure ambient symbols are declared

export function SystemJS_ScheduleTimer(shortestDueTimeMs: number): void {
    if (DOTNET.lastScheduledTimerId) {
        globalThis.clearTimeout(DOTNET.lastScheduledTimerId);
        DOTNET.lastScheduledTimerId = undefined;
    }
    DOTNET.lastScheduledTimerId = Module.safeSetTimeout(timerTick, shortestDueTimeMs);

    function timerTick(): void {
        Module.maybeExit();
        _SystemJS_ExecuteTimerCallback();
    }
}
SystemJS_ScheduleTimer["__deps"] = ['SystemJS_ExecuteTimerCallback'];

export function SystemJS_ScheduleBackgroundJob(): void {
    if (DOTNET.lastScheduledThreadPoolId) {
        globalThis.clearTimeout(DOTNET.lastScheduledThreadPoolId);
        DOTNET.lastScheduledThreadPoolId = undefined;
    }
    DOTNET.lastScheduledThreadPoolId = Module.safeSetTimeout(timerTick, 0);

    function timerTick(): void {
        Module.maybeExit();
        _SystemJS_ExecuteBackgroundJobCallback();
    }
}
SystemJS_ScheduleBackgroundJob["__deps"] = ['SystemJS_ExecuteBackgroundJobCallback'];
