// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { commandStopTracing, commandCounters } from "./client-commands";
import { IDiagClient, IDiagServer, IDiagSession } from "./common";
import { mono_log_warn } from "../logging";
import { Module } from "../globals";

export class CountersClient implements IDiagClient {
    private firstAdvert = false;
    private firstSession = false;
    onAdvertise (server: IDiagServer): void {
        if (!this.firstAdvert) {
            this.firstAdvert = true;
            server.createSession(commandCounters());
        }
    }
    onSessionStart (server: IDiagServer, session: IDiagSession): void {
        if (!this.firstSession) {
            this.firstSession = true;
            // stop tracing after 20 seconds of monitoring
            Module.safeSetTimeout(() => {
                server.sendCommand(commandStopTracing(session.session_id));
            }, 20000);
        }
    }
    onData (server: IDiagServer, session: IDiagSession, message: Uint8Array): void {
        session.store(message);
    }
    onError (server: IDiagServer, session: IDiagSession, message: Uint8Array): void {
        mono_log_warn("Diagnostic session " + session.session_id + " error : " + message.toString());
    }
}
