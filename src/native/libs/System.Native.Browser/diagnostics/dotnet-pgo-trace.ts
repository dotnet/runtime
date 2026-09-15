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
    if (pgoSession) {
        throw new Error("A PGO trace collection is already in progress");
    }

    const durationSeconds = options.durationSeconds ?? DEFAULT_PGO_DURATION_SECONDS;

    const onClosePromise = dotnetLoaderExports.createPromiseCompletionSource<Uint8Array[]>();
    let startedSession: IDiagnosticSession | undefined = undefined;
    let stopTimeoutId: number | undefined = undefined;
    function onSessionStart(session: IDiagnosticSession): void {
        startedSession = session;
        pgoSession = session;
        session.sendCommand(commandResumeRuntime());
        // stop and flush the trace once the duration elapses
        stopTimeoutId = Module.safeSetTimeout(() => {
            stopPgoTrace(session);
        }, 1000 * durationSeconds);
    }

    setupJsClient({
        onClosePromise: onClosePromise,
        skipDownload: options.skipDownload,
        commandOnAdvertise: () => commandPgoTrace(options!),
        onSessionStart,
        onClose: () => {
            // clear only if this call's session is still the active one
            if (pgoSession === startedSession) {
                pgoSession = undefined;
            }
            if (stopTimeoutId !== undefined) {
                globalThis.clearTimeout(stopTimeoutId);
                stopTimeoutId = undefined;
            }
        },
    }, startup);
    return onClosePromise.promise;
}

// stops the in-progress PGO trace when the collection duration elapses; pgoSession stays set until onClose
function stopPgoTrace(session: IDiagnosticSession): void {
    // ignore a stale timer whose session was already closed or replaced
    if (pgoSession !== session) {
        return;
    }
    session.sendCommand(commandStopTracing(session.sessionId));
}
