// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import cwraps from "./cwraps";
import { Module, loaderHelpers } from "./globals";
import { forceThreadMemoryViewRefresh } from "./memory";

let spread_timers_maximum = 0;
let pump_count = 0;

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
        globalThis.setTimeout(prevent_timer_throttling_tick, delay);
    }
    spread_timers_maximum = desired_reach_time;
}

function prevent_timer_throttling_tick () {
    if (WasmEnableThreads) return;
    Module.maybeExit();
    if (!loaderHelpers.is_runtime_running()) {
        return;
    }
    try {
        cwraps.mono_wasm_execute_timer();
        pump_count++;
    } catch (ex) {
        loaderHelpers.mono_exit(1, ex);
    }
    mono_background_exec_until_done();
}

function mono_background_exec_until_done () {
    if (WasmEnableThreads) return;
    Module.maybeExit();
    try {
        while (pump_count > 0) {
            --pump_count;
            if (!loaderHelpers.is_runtime_running()) {
                return;
            }
            cwraps.mono_background_exec();
        }
    } catch (ex) {
        loaderHelpers.mono_exit(1, ex);
    }
}

export function schedule_background_exec (): void {
    if (WasmEnableThreads) return;
    ++pump_count;
    Module.safeSetTimeout(mono_background_exec_until_done, 0);
}

let lastScheduledTimeoutId: any = undefined;
export function mono_wasm_schedule_timer (shortestDueTimeMs: number): void {
    if (WasmEnableThreads) return;
    if (lastScheduledTimeoutId) {
        globalThis.clearTimeout(lastScheduledTimeoutId);
        lastScheduledTimeoutId = undefined;
    }
    lastScheduledTimeoutId = Module.safeSetTimeout(mono_wasm_schedule_timer_tick, shortestDueTimeMs);
}

function mono_wasm_schedule_timer_tick () {
    if (WasmEnableThreads) return;
    Module.maybeExit();
    forceThreadMemoryViewRefresh();
    if (!loaderHelpers.is_runtime_running()) {
        return;
    }
    lastScheduledTimeoutId = undefined;
    try {
        cwraps.mono_wasm_execute_timer();
        pump_count++;
    } catch (ex) {
        loaderHelpers.mono_exit(1, ex);
    }
}
