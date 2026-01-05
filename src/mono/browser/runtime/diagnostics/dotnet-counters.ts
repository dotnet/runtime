// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticCommandOptions } from "../types";

import { commandStopTracing, commandCounters } from "./client-commands";
import { IDiagnosticSession } from "./common";
import { Module } from "./globals";
import { serverSession, setupJsClient } from "./diagnostics-js";
import { loaderHelpers } from "./globals";

export function collectMetrics (options?:DiagnosticCommandOptions):Promise<Uint8Array[]> {
    if (!options) options = {};
    if (!serverSession) {
        throw new Error("No active JS diagnostic session");
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
        commandOnAdvertise:() => commandCounters(options),
        onSessionStart,
    });
    return onClosePromise.promise;
}
