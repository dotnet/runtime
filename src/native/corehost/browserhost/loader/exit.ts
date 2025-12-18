// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { OnExitListener } from "../types";
import { dotnetLogger, dotnetLoaderExports, Module, dotnetBrowserHostExports } from "./cross-module";

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
    if (Module.onAbort == onEmAbort) {
        Module.onAbort = runtimeState.originalOnAbort;
    }
    if (Module.onExit == onEmExit) {
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
    const ExitStatus = dotnetBrowserHostExports.getExitStatus();
    const ex = typeof ExitStatus === "function"
        ? new ExitStatus(exitCode)
        : new Error("Exit with code " + exitCode + " " + message);
    ex.message = message;
    ex.toString = () => message;
    return ex;
}

// WASM-TODO: raise ExceptionHandling.RaiseAppDomainUnhandledExceptionEvent() - also for JS unhandled exceptions ?
export function exit(exitCode: number, reason: any): void {
    // unify shape of the reason object
    const is_object = reason && typeof reason === "object";
    exitCode = (is_object && typeof reason.status === "number")
        ? reason.status
        : exitCode === undefined
            ? -1
            : exitCode;
    const message = (is_object && typeof reason.message === "string")
        ? reason.message
        : "" + reason;
    reason = is_object
        ? reason
        : createExitStatus(exitCode, message);
    reason.status = exitCode;
    if (!reason.message) {
        reason.message = message;
    }

    // force stack property to be generated before we shut down managed code, or create current stack if it doesn't exist
    const stack = "" + (reason.stack || (new Error().stack));
    try {
        Object.defineProperty(reason, "stack", {
            get: () => stack
        });
    } catch (e) {
        // ignore
    }

    // don't report this error twice
    const alreadySilent = !!reason.silent;
    const alreadyExisted = isExited();
    reason.silent = true;

    if (!alreadyExisted) {
        runtimeState.exitCode = exitCode;
        if (!runtimeState.exitReason) {
            runtimeState.exitReason = reason;
        }
        unregisterExit();
        if (!alreadySilent) {
            if (runtimeState.onExitListeners.length === 0 && !runtimeState.runtimeReady) {
                dotnetLogger.error(`Exiting during runtime startup: ${message}`);
            }
            for (const listener of runtimeState.onExitListeners) {
                try {
                    listener(exitCode, reason, alreadySilent);
                } catch {
                    // ignore errors from listeners
                }
            }
        }
        try {
            if (!runtimeState.runtimeReady) {
                dotnetLogger.debug(() => `abort_startup, reason: ${reason}`);
                dotnetLoaderExports.abortStartup(reason);
            }
        } catch (err) {
            dotnetLogger.warn("dotnet.js exit() failed", err);
            // don't propagate any failures
        }

        if (runtimeState.runtimeReady) {
            Module.runtimeKeepalivePop();
            quitNow(exitCode, reason);
        }
    } else {
        dotnetLogger.debug("dotnet.js exit() called after exit");
    }
    throw reason;
}

export function quitNow(exitCode: number, reason?: any): void {
    if (Module && typeof Module.abort === "function") {
        // make sure that posix/emscripten knows about the abort
        try {
            Module.abort(reason);
        } catch {
            // ignore
        }
    }
    if (exitCode !== 0 || !ENVIRONMENT_IS_WEB) {
        if (ENVIRONMENT_IS_NODE && globalThis.process && typeof globalThis.process.exit === "function") {
            globalThis.process.exitCode = exitCode;
            globalThis.process.exit(exitCode);
        }
        throw reason;
    }
}
