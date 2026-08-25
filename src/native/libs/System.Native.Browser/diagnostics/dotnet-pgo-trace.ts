// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticCommandOptions } from "../types";

import { commandResumeRuntime, commandStopTracing, commandPgoTrace } from "./client-commands";
import { dotnetLoaderExports, Module } from "./cross-module";
import { serverSession, setupJsClient } from "./diagnostic-server-js";
import { IDiagnosticSession } from "./types";

// Default trace duration when the caller doesn't specify one.
const DEFAULT_PGO_DURATION_SECONDS = 10;

// the session currently collecting a PGO trace; stopped internally when the duration elapses
let pgoSession: IDiagnosticSession | undefined = undefined;

export function collectPgoTrace(options?: DiagnosticCommandOptions, startup?: boolean): Promise<Uint8Array[]> {
    if (!options) options = {};
    if (!startup && !serverSession) {
        throw new Error("No active JS diagnostic session");
    }

    const durationSeconds = options.durationSeconds ?? DEFAULT_PGO_DURATION_SECONDS;

    const onClosePromise = dotnetLoaderExports.createPromiseCompletionSource<Uint8Array[]>();
    function onSessionStart(session: IDiagnosticSession): void {
        pgoSession = session;
        session.sendCommand(commandResumeRuntime());
        // stop and flush the trace once the duration elapses
        Module.safeSetTimeout(() => {
            stopPgoTrace();
        }, 1000 * durationSeconds);
    }

    setupJsClient({
        onClosePromise: onClosePromise,
        skipDownload: options.skipDownload,
        commandOnAdvertise: () => commandPgoTrace(options!),
        onSessionStart,
        onClose: () => {
            pgoSession = undefined;
        },
    }, startup);
    return onClosePromise.promise;
}

// stops the in-progress PGO trace when the collection duration elapses
function stopPgoTrace(): void {
    if (!pgoSession) {
        return;
    }
    const session = pgoSession;
    pgoSession = undefined;
    session.sendCommand(commandStopTracing(session.sessionId));
}
