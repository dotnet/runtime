// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import cwraps from "./cwraps";
import { Module, loaderHelpers } from "./globals";

let spread_timers_maximum = 0;
let lastScheduledTimerId: number | undefined = undefined;
let lastScheduledThreadPoolId: number | undefined = undefined;
let lastScheduledFinalizationId: number | undefined = undefined;
let lastScheduledDiagnosticServerId: number | undefined = undefined;

export function prevent_timer_throttling (): void {
    if (WasmEnableThreads) return;
    if (!loaderHelpers.isChromium) {
        return;
    }

    // this will schedule timers every second for next 6 minutes, it should be called from WebSocket event, to make it work
    // on next call, it would only extend the timers to cover yet uncovered future
    const now = new Date().valueOf();
    const desired_reach_time = now + (1000 * 60 * 6);
    const next_reach_time = Math.max(now + 1000, spread_timers_maximum);
    const light_throttling_frequency = 1000;
    for (let schedule = next_reach_time; schedule < desired_reach_time; schedule += light_throttling_frequency) {
        const delay = schedule - now;
        globalThis.setTimeout(runBackgroundTimers, delay);
    }
    spread_timers_maximum = desired_reach_time;
}

export function runBackgroundTimers (): void {
    if (WasmEnableThreads) return;
    Module.maybeExit();
    if (!loaderHelpers.is_runtime_running()) {
        return;
    }

    cwraps.SystemJS_ExecuteTimerCallback();
    cwraps.SystemJS_ExecuteBackgroundJobCallback();
    cwraps.SystemJS_ExecuteFinalizationCallback();
    cwraps.SystemJS_ExecuteDiagnosticServerCallback();
}

export function SystemJS_ScheduleTimer (shortestDueTimeMs: number): void {
    if (WasmEnableThreads) return;
    Module.maybeExit();
    if (!loaderHelpers.is_runtime_running()) {
        return;
    }

    if (lastScheduledTimerId) {
        globalThis.clearTimeout(lastScheduledTimerId);
        Module.runtimeKeepalivePop();
        lastScheduledTimerId = undefined;
    }
    lastScheduledTimerId = Module.safeSetTimeout(SystemJS_ScheduleTimerTick, shortestDueTimeMs);

    function SystemJS_ScheduleTimerTick (): void {
        try {
            lastScheduledTimerId = undefined;
            cwraps.SystemJS_ExecuteTimerCallback();
        } catch (error: any) {
            // do not propagate ExitStatus exception
            if (!error || typeof error.status !== "number") {
                loaderHelpers.mono_exit(1, error);
                throw error;
            }
        }
    }
}

export function SystemJS_ScheduleBackgroundJob (): void {
    if (WasmEnableThreads) return;
    Module.maybeExit();
    if (!loaderHelpers.is_runtime_running()) {
        return;
    }
    if (lastScheduledThreadPoolId) {
        return;
    }
    lastScheduledThreadPoolId = Module.safeSetTimeout(SystemJS_ScheduleBackgroundJobTick, 0);

    function SystemJS_ScheduleBackgroundJobTick (): void {
        try {
            lastScheduledThreadPoolId = undefined;
            cwraps.SystemJS_ExecuteBackgroundJobCallback();
        } catch (error: any) {
            // do not propagate ExitStatus exception
            if (!error || typeof error.status !== "number") {
                loaderHelpers.mono_exit(1, error);
                throw error;
            }
        }
    }
}

export function SystemJS_ScheduleFinalization (): void {
    if (WasmEnableThreads) return;
    Module.maybeExit();
    if (!loaderHelpers.is_runtime_running()) {
        return;
    }

    if (lastScheduledFinalizationId) {
        return;
    }
    lastScheduledFinalizationId = Module.safeSetTimeout(SystemJS_ScheduleFinalizationTick, 0);

    function SystemJS_ScheduleFinalizationTick (): void {
        try {
            lastScheduledFinalizationId = undefined;
            cwraps.SystemJS_ExecuteFinalizationCallback();
        } catch (error: any) {
            // do not propagate ExitStatus exception
            if (!error || typeof error.status !== "number") {
                loaderHelpers.mono_exit(1, error);
                throw error;
            }
        }
    }
}

export function SystemJS_ScheduleDiagnosticServerJob (): void {
    if (WasmEnableThreads) return;
    Module.maybeExit();
    if (!loaderHelpers.is_runtime_running()) {
        return;
    }

    if (lastScheduledDiagnosticServerId) {
        return;
    }
    lastScheduledDiagnosticServerId = Module.safeSetTimeout(SystemJS_ScheduleDiagnosticServerTick, 0);

    function SystemJS_ScheduleDiagnosticServerTick (): void {
        try {
            lastScheduledDiagnosticServerId = undefined;
            cwraps.SystemJS_ExecuteDiagnosticServerCallback();
        } catch (error: any) {
            // do not propagate ExitStatus exception
            if (!error || typeof error.status !== "number") {
                loaderHelpers.mono_exit(1, error);
                throw error;
            }
        }
    }
}
