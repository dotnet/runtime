// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { commandStopTracing, commandGcHeapDump, } from "./client-commands";
import { IDiagClient, IDiagServer, IDiagSession } from "./common";
import { mono_log_warn } from "../logging";
import { Module } from "../globals";

export class GcDumpDiagClient implements IDiagClient {
    private firstAdvert = false;
    private firstSession = false;
    private stopSent = false;
    private stopDelayedAfterLastMessage:number = undefined as any;
    onAdvertise (server: IDiagServer): void {
        if (!this.firstAdvert) {
            this.firstAdvert = true;
            server.createSession(commandGcHeapDump());
        }
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    onSessionStart (server: IDiagServer, session: IDiagSession): void {
        if (!this.firstSession) {
            this.firstSession = true;
        }
    }
    onData (server: IDiagServer, session: IDiagSession, message: Uint8Array): void {
        session.store(message);
        if (this.firstAdvert && this.firstSession && !this.stopSent) {
            if (this.stopDelayedAfterLastMessage) {
                clearTimeout(this.stopDelayedAfterLastMessage);
            }
            this.stopDelayedAfterLastMessage = Module.safeSetTimeout(() => {
                this.stopSent = true;
                server.sendCommand(commandStopTracing(session.session_id));
            }, 500);
        }
    }
    onError (server: IDiagServer, session: IDiagSession, message: Uint8Array): void {
        mono_log_warn("Diagnostic session " + session.session_id + " error : " + message.toString());
    }
}
