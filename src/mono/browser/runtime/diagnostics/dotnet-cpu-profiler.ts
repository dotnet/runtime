// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticCommandOptions } from "../types";

import { commandStopTracing, commandSampleProfiler } from "./client-commands";
import { loaderHelpers, Module, runtimeHelpers } from "./globals";
import { serverSession, setupJsClient } from "./diagnostics-js";
import { IDiagnosticSession } from "./common";

export function collectCpuSamples (options?:DiagnosticCommandOptions):Promise<Uint8Array[]> {
    if (!options) options = {};
    if (!serverSession) {
        throw new Error("No active JS diagnostic session");
    }
    if (!runtimeHelpers.config.environmentVariables!["DOTNET_WasmPerformanceInstrumentation"]) {
        throw new Error("method instrumentation is not enabled, please enable it with WasmPerformanceInstrumentation MSBuild property");
    }

    const onClosePromise = loaderHelpers.createPromiseController<Uint8Array[]>();
    function onSessionStart (session: IDiagnosticSession): void {
        // stop tracing after period of monitoring
        Module.safeSetTimeout(() => {
            session.sendCommand(commandStopTracing(session.session_id));
        }, 1000 * (options?.durationSeconds ?? 60));
    }

    setupJsClient({
        onClosePromise:onClosePromise.promise_control,
        skipDownload:options.skipDownload,
        commandOnAdvertise: () => commandSampleProfiler(options),
        onSessionStart,
    });
    return onClosePromise.promise;
}
