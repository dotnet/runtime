// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";

let spread_timers_maximum = 0;
export let isChromium = false;
let pump_count = 0;

if (globalThis.navigator) {
    const nav: any = globalThis.navigator;
    if (nav.userAgentData && nav.userAgentData.brands) {
        isChromium = nav.userAgentData.brands.some((i: any) => i.brand == "Chromium");
    }
    else if (nav.userAgent) {
        isChromium = nav.userAgent.includes("Chrome");
    }
}

export function prevent_timer_throttling(): void {
    if (!isChromium) {
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
        setTimeout(() => {
            cwraps.mono_wasm_execute_timer();
            pump_count++;
            mono_background_exec_until_done();
        }, delay);
    }
    spread_timers_maximum = desired_reach_time;
}

function mono_background_exec_until_done() {
    while (pump_count > 0) {
        --pump_count;
        cwraps.mono_background_exec();
    }
}

export function schedule_background_exec(): void {
    ++pump_count;
    setTimeout(mono_background_exec_until_done, 0);
}

let lastScheduledTimeoutId: any = undefined;
export function mono_wasm_schedule_timer(shortestDueTimeMs: number): void {
    if (lastScheduledTimeoutId) {
        clearTimeout(lastScheduledTimeoutId);
        lastScheduledTimeoutId = undefined;
    }
    lastScheduledTimeoutId = setTimeout(cwraps.mono_wasm_execute_timer, shortestDueTimeMs);
}
