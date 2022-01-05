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

function pump_message() {
    while (pump_count > 0) {
        --pump_count;
        cwraps.mono_background_exec();
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
            cwraps.mono_set_timeout_exec();
            pump_count++;
            pump_message();
        }, delay);
    }
    spread_timers_maximum = desired_reach_time;
}

export function schedule_background_exec(): void {
    ++pump_count;
    setTimeout(pump_message, 0);
}

let lastScheduledTimeoutId: any = undefined;
export function mono_set_timeout(timeout: number): void {
    function mono_wasm_set_timeout_exec() {
        cwraps.mono_set_timeout_exec();
    }
    if (lastScheduledTimeoutId) {
        clearTimeout(lastScheduledTimeoutId);
        lastScheduledTimeoutId = undefined;
    }
    lastScheduledTimeoutId = setTimeout(mono_wasm_set_timeout_exec, timeout);
}
