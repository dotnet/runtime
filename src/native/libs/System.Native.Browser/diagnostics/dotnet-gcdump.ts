// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticCommandOptions } from "../types";

import { commandStopTracing, commandGcHeapDump, } from "./client-commands";
import { dotnetLoaderExports, Module } from "./cross-module";
import { serverSession, setupJsClient } from "./diagnostic-server-js";
import { IDiagnosticSession } from "./types";

export function collectGcDump(options?: DiagnosticCommandOptions): Promise<Uint8Array[]> {
    if (!options) options = {};
    if (!serverSession) {
        throw new Error("No active JS diagnostic session");
    }

    const onClosePromise = dotnetLoaderExports.createPromiseCompletionSource<Uint8Array[]>();
    let stopDelayedAfterLastMessage = 0;
    let stopSent = false;
    function onData(session: IDiagnosticSession, message: Uint8Array): void {
        session.store(message);
        if (!stopSent) {
            // stop 1000ms after last GC message on this session, there will be more messages after that
            if (stopDelayedAfterLastMessage) {
                clearTimeout(stopDelayedAfterLastMessage);
            }
            stopDelayedAfterLastMessage = Module.safeSetTimeout(() => {
                stopSent = true;
                session.sendCommand(commandStopTracing(session.sessionId));
            }, 1000 * (options?.durationSeconds ?? 1));
        }
    }

    setupJsClient({
        onClosePromise: onClosePromise,
        skipDownload: options.skipDownload,
        commandOnAdvertise: () => commandGcHeapDump(options),
        onData,
    });
    return onClosePromise.promise;
}
