// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { VoidPtr } from "../types";
import { Module, dotnetNativeBrowserExports } from "./cross-module";
import { ENVIRONMENT_IS_WEB } from "./per-module";


const hasMeasure = globalThis.performance && typeof globalThis.performance.measure === "function";

// Cache of formatted method names keyed by MethodDesc* pointer. We only call
// back into the runtime (SystemJS_GetMethodName) and decode the UTF-8
// string on a cache miss.
const methodNameCache = new Map<number, string>();

export function ds_rt_browser_performance_measure(methodPtr: VoidPtr, start: number): void {
    if (!hasMeasure) {
        return;
    }

    try {
        const key = methodPtr as unknown as number;
        let fnName = methodNameCache.get(key);
        if (fnName === undefined) {
            const namePtr = dotnetNativeBrowserExports.SystemJS_GetMethodName(key);
            fnName = Module.UTF8ToString(namePtr);
            Module._free(namePtr as unknown as VoidPtr);
            methodNameCache.set(key, fnName);
        }
        // NodeJs accepts startTime, browsers accepts start
        const options = ENVIRONMENT_IS_WEB ? { start: start } : { startTime: start };
        globalThis.performance.measure(fnName, options);
    } catch {
        // Ignore
    }
}
