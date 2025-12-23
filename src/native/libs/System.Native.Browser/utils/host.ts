// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function setEnvironmentVariable(name: string, value: string): void {
    throw new Error("Not implemented");
}

export function abortTimers(): void {
    if (DOTNET.lastScheduledTimerId) {
        globalThis.clearTimeout(DOTNET.lastScheduledTimerId);
        DOTNET.lastScheduledTimerId = undefined;
    }
    if (DOTNET.lastScheduledThreadPoolId) {
        globalThis.clearTimeout(DOTNET.lastScheduledThreadPoolId);
        DOTNET.lastScheduledThreadPoolId = undefined;
    }
}

export function abortPosix(exitCode: number): void {
    ABORT = true;
    EXITSTATUS = exitCode;
    _emscripten_force_exit(exitCode);
}
