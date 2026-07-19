// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticCommandOptions } from "../types";

import { commandResumeRuntime, commandStopTracing, commandSampleProfiler } from "./client-commands";
import { dotnetApi, dotnetLoaderExports, Module } from "./cross-module";
import { serverSession, setupJsClient } from "./diagnostic-server-js";
import { IDiagnosticSession } from "./types";

export function collectCpuSamples(options?: DiagnosticCommandOptions, startup?: boolean): Promise<Uint8Array[]> {
    if (!options) options = {};
    if (!startup && !serverSession) {
        throw new Error("No active JS diagnostic session");
    }
    if (!dotnetApi.getConfig().environmentVariables!["DOTNET_WasmPerformanceInstrumentation"]) {
        throw new Error("method instrumentation is not enabled, please enable it with WasmPerformanceInstrumentation MSBuild property");
    }

    const onClosePromise = dotnetLoaderExports.createPromiseCompletionSource<Uint8Array[]>();
    function onSessionStart(session: IDiagnosticSession): void {
        session.sendCommand(commandResumeRuntime());
        // stop tracing after period of monitoring
        Module.safeSetTimeout(() => {
            session.sendCommand(commandStopTracing(session.sessionId));
        }, 1000 * (options?.durationSeconds ?? 60));
    }

    setupJsClient({
        onClosePromise: onClosePromise,
        skipDownload: options.skipDownload,
        commandOnAdvertise: () => commandSampleProfiler(options),
        onSessionStart,
    }, startup);
    return onClosePromise.promise;
}
