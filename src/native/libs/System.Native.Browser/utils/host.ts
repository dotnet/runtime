// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";
import { Module, dotnetApi } from "./cross-module";
import { ExitStatus, DOTNET, _exit, _emscripten_force_exit } from "../../Common/JavaScript/cross-linked"; // ensure ambient symbols are declared

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function setEnvironmentVariable(name: string, value: string): void {
    throw new Error("Not implemented");
}

export function getExitStatus(): new (exitCode: number) => any {
    return ExitStatus as any;
}

export function abortTimers(): void {
    if (DOTNET.lastScheduledTimerId) {
        globalThis.clearTimeout(DOTNET.lastScheduledTimerId);
        Module.runtimeKeepalivePop();
        DOTNET.lastScheduledTimerId = undefined;
    }
    if (DOTNET.lastScheduledThreadPoolId) {
        globalThis.clearTimeout(DOTNET.lastScheduledThreadPoolId);
        Module.runtimeKeepalivePop();
        DOTNET.lastScheduledThreadPoolId = undefined;
    }
}

/* eslint-disable @typescript-eslint/no-unused-vars */
declare let ABORT: boolean;
declare let EXITSTATUS: number;
export function abortPosix(exitCode: number): void {
    ABORT = true;
    EXITSTATUS = exitCode;
    try {
        if (BuildConfiguration === "Debug") {
            _exit(exitCode, true);
        } else {
            _emscripten_force_exit(exitCode);
        }
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (error.status === undefined) {
            dotnetApi.exit(1, error);
            throw error;
        }
    }
}
