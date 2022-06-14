// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { EventPipeSessionIDImpl, EventPipeSessionDiagnosticServerID, DiagnosticServerControlCommand, DiagnosticServerControlCommandStart, DiagnosticServerControlCommandSetSessionID } from "../diagnostic-server-controller-commands";

/// Everything the diagnostic server knows about a connection.
/// The connection has a server ID and a websocket. If it's an eventpipe session, it will also have an eventpipe ID assigned when the runtime starts an EventPipe session.

interface DiagnosticServerConnection {
    readonly type: string;
    get diagnosticSessionID(): EventPipeSessionDiagnosticServerID;
    get socket(): WebSocket;
}

interface DiagnosticServerEventPipeConnection extends DiagnosticServerConnection {
    type: "eventpipe";
    get sessionID(): EventPipeSessionIDImpl;
    set sessionID(sessionID: EventPipeSessionIDImpl);
}

interface ServerSessionManager {
    createSession(): DiagnosticServerConnection;
    assignEventPipeSessionID(diagnosticServerID: EventPipeSessionDiagnosticServerID, sessionID: EventPipeSessionIDImpl): void;
    getSession(sessionID: EventPipeSessionDiagnosticServerID): DiagnosticServerEventPipeConnection | null;
}

function startServer(): void {
    // TODO
    console.debug("TODO: startServer");
}

function setSessionID(diagnosticServerID: EventPipeSessionDiagnosticServerID, sessionID: EventPipeSessionIDImpl): void {
    // TODO
    console.debug("TODO: setSessionID");
}
function controlCommandReceived(event: MessageEvent<DiagnosticServerControlCommand>): void {
    console.debug("get in loser, we're going to vegas", event.data);
    const cmd = event.data;
    if (cmd.type === undefined) {
        console.error("control command has no type property");
        return;
    }
    switch (cmd.type) {
        case "start":
            startServer();
            break;
        case "set_session_id":
            setSessionID(cmd.diagnostic_server_id, cmd.session_id);
            break;
        default:
            console.warn("Unknown control command: " + (<any>cmd).type);
            break;
    }
}

function workerMain() {
    self.addEventListener("message", controlCommandReceived);
}

workerMain();
