// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr } from "../types";
import { Module } from "./cross-module";
import { ENVIRONMENT_IS_WEB } from "./per-module";


const hasMeasure = globalThis.performance && typeof globalThis.performance.measure === "function";

export function ds_rt_browser_performance_measure(namePtr: CharPtr, start: number): void {
    if (!hasMeasure) {
        return;
    }

    try {
        const fnName = Module.UTF8ToString(namePtr);
        // NodeJs accepts startTime, browsers accepts start
        const options = ENVIRONMENT_IS_WEB ? { start: start } : { startTime: start };
        globalThis.performance.measure(fnName, options);
    } catch {
        // Ignore
    }
}
