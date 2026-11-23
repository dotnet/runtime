// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { TimeStamp } from "./types";

import { dotnetAssert, dotnetLoaderExports } from "./cross-module";
import { jsInteropState } from "./marshal";
import { ENVIRONMENT_IS_WEB } from "./per-module";
import Wasm64 from "consts:wasm64";

export function fixupPointer(pointer: any, shiftAmount: number): any {
    if (!Wasm64) {
        return ((pointer as any) >>> shiftAmount) as any;
    }
    const address = Number(pointer);
    if (!Number.isSafeInteger(address) || address < 0) {
        throw new RangeError("Invalid Wasm address: " + pointer);
    }
    return Math.floor(address / (2 ** shiftAmount));
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
