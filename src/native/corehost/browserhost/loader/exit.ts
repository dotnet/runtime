// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetLogger } from "./cross-module";
import { ENVIRONMENT_IS_NODE } from "./per-module";

export const runtimeState = {
    exitCode: undefined as number | undefined,
    exitReason: undefined as any,
    runtimeReady: false,
    originalOnAbort: undefined as ((reason: any, extraJson?: string) => void) | undefined,
    originalOnExit: undefined as ((code: number) => void) | undefined,
    onExitListeners: [] as OnExitListener[],
};

export function isExited() {
    return runtimeState.exitCode !== undefined;
}

export function isRuntimeRunning() {
    return runtimeState.runtimeReady && !isExited();
}

export function addOnExitListener(cb: OnExitListener) {
    runtimeState.onExitListeners.push(cb);
}

export function registerExit() {
    runtimeState.originalOnAbort = Module.onAbort;
    runtimeState.originalOnExit = Module.onExit;
    Module.onAbort = onEmAbort;
    Module.onExit = onEmExit;
}

function unregisterExit() {
    if (Module.onAbort === onEmAbort) {
        Module.onAbort = runtimeState.originalOnAbort;
    }
    if (Module.onExit === onEmExit) {
        Module.onExit = runtimeState.originalOnExit;
    }
}

function onEmExit(code: number) {
    if (runtimeState.originalOnExit) {
        runtimeState.originalOnExit(code);
    }
    exit(code, runtimeState.exitReason);
}

function onEmAbort(reason: any) {
    if (runtimeState.originalOnAbort) {
        runtimeState.originalOnAbort(reason || runtimeState.exitReason);
    }
    exit(1, reason || runtimeState.exitReason);
}

function createExitStatus(exitCode: number, message: string): any {
    const ExitStatus = dotnetBrowserUtilsExports.getExitStatus();
    const ex = typeof ExitStatus === "function"
        ? new ExitStatus(exitCode)
        : new Error("Exit with code " + exitCode + " " + message);
    ex.message = message;
    ex.toString = () => message;
    return ex;
}

// WASM-TODO: raise ExceptionHandling.RaiseAppDomainUnhandledExceptionEvent() - also for JS unhandled exceptions ?
export function exit(exitCode: number, reason: any): void {
    if (reason) {
        const reasonStr = (typeof reason === "object") ? `${reason.message || ""}\n${reason.stack || ""}` : reason.toString();
        dotnetLogger.error(reasonStr);
    }
    if (ENVIRONMENT_IS_NODE) {
        (globalThis as any).process.exit(exitCode);
    }
}
