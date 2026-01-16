// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { TimeStamp } from "./types";

import { dotnetAssert, dotnetDiagnosticsExports, dotnetLoaderExports } from "./cross-module";
import { jsInteropState } from "./marshal";
import { ENVIRONMENT_IS_WEB } from "./per-module";

export function fixupPointer(signature: any, shiftAmount: number): any {
    return ((signature as any) >>> shiftAmount) as any;
}

export function normalizeException(ex: any) {
    let res = "unknown exception";
    if (ex) {
        res = ex.toString();
        const stack = ex.stack;
        if (stack) {
            // Some JS runtimes insert the error message at the top of the stack, some don't,
            //  so normalize it by using the stack as the result if it already contains the error
            if (stack.startsWith(res))
                res = stack;
            else
                res += "\n" + stack;
        }
        if (dotnetDiagnosticsExports.symbolicateStackTrace) {
            res = dotnetDiagnosticsExports.symbolicateStackTrace(res);
        }
    }
    return res;
}

export function isRuntimeRunning(): boolean {
    return dotnetLoaderExports.isRuntimeRunning();
}

export function assertRuntimeRunning(): void {
    dotnetAssert.check(isRuntimeRunning(), "The runtime is not running.");
}

export function assertJsInterop(): void {
    dotnetAssert.check(isRuntimeRunning() && jsInteropState.isInitialized, "The runtime is not running.");
}


export function startMeasure(): TimeStamp {
    if (jsInteropState.enablePerfMeasure) {
        return globalThis.performance.now() as any;
    }
    return undefined as any;
}

export function endMeasure(start: TimeStamp, block: string, id?: string) {
    if (jsInteropState.enablePerfMeasure && start) {
        // API is slightly different between web and Nodejs
        const options = ENVIRONMENT_IS_WEB
            ? { start: start as any }
            : { startTime: start as any };
        const name = id ? `${block}${id} ` : block;
        globalThis.performance.measure(name, options);
    }
}
