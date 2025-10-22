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
