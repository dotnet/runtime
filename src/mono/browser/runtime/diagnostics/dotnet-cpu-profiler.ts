// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { commandStopTracing, commandSampleProfiler } from "./client-commands";
import { loaderHelpers, Module } from "./globals";
import { serverSession, setup_js_client } from "./diag-js";
import { IDiagSession } from "./common";

export function collectCpuSamples (durationMs:number, skipDownload?:boolean):Promise<Uint8Array[]> {
    if (!serverSession) {
        throw new Error("No active JS diagnostic session");
    }

    const onClosePromise = loaderHelpers.createPromiseController<Uint8Array[]>();
    function onSessionStart (session: IDiagSession): void {
        // stop tracing after period of monitoring
        Module.safeSetTimeout(() => {
            session.sendCommand(commandStopTracing(session.session_id));
        }, durationMs);
    }

    setup_js_client({
        onClosePromise:onClosePromise.promise_control,
        skipDownload,
        commandOnAdvertise:commandSampleProfiler,
        onSessionStart,
    });
    return onClosePromise.promise;
}
