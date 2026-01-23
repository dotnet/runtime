// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetApi, dotnetBrowserUtilsExports } from "./cross-module";
import { jsInteropState } from "./marshal";
import { ENVIRONMENT_IS_WEB } from "./per-module";
import { isRuntimeRunning } from "./utils";

let spreadTimersMaximum = 0;
const pendingJsTimers: Set<number> = new Set();

export function initializeScheduling(): void {
    if (ENVIRONMENT_IS_WEB && globalThis.navigator) {
        const navigator: any = globalThis.navigator;
        const brands = navigator.userAgentData && navigator.userAgentData.brands;
        if (brands && brands.length > 0) {
            jsInteropState.isChromium = brands.some((b: any) => b.brand === "Google Chrome" || b.brand === "Microsoft Edge" || b.brand === "Chromium");
        } else if (navigator.userAgent) {
            jsInteropState.isChromium = navigator.userAgent.includes("Chrome");
            jsInteropState.isFirefox = navigator.userAgent.includes("Firefox");
        }
    }
}

export function abortInteropTimers(): void {
    for (const id of pendingJsTimers) {
        globalThis.clearTimeout(id);
    }
    pendingJsTimers.clear();
    spreadTimersMaximum = 0;
}

export function preventTimerThrottling(): void {
    if (!jsInteropState.isChromium) {
        return;
    }

    // this will schedule timers every second for next 6 minutes, it should be called from WebSocket event, to make it work
    // on next call, it would only extend the timers to cover yet uncovered future
    const now = new Date().valueOf();
    const desiredReachTime = now + (1000 * 60 * 6);
    const nextReachTime = Math.max(now + 1000, spreadTimersMaximum);
    const lightThrottlingFrequency = 1000;
    for (let schedule = nextReachTime; schedule < desiredReachTime; schedule += lightThrottlingFrequency) {
        const delay = schedule - now;
        const id = {
            value: -1,
        };
        id.value = dotnetApi.Module.safeSetTimeout(() => preventTimerThrottlingTick(id), delay);
        pendingJsTimers.add(id.value);
    }
    spreadTimersMaximum = desiredReachTime;

    function preventTimerThrottlingTick(id: { value: number }) {
        pendingJsTimers.delete(id.value);
        if (!isRuntimeRunning()) {
            return;
        }
        dotnetBrowserUtilsExports.runBackgroundTimers();
    }
}
