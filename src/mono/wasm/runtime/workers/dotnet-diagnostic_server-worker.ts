// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type {
    EventPipeSessionIDImpl, EventPipeSessionDiagnosticServerID, DiagnosticServerControlCommand,
    /*DiagnosticServerControlCommandStart, DiagnosticServerControlCommandSetSessionID*/
} from "../diagnostic-server-controller-commands";

/// Everything the diagnostic server knows about a connection.
/// The connection has a server ID and a websocket. If it's an eventpipe session, it will also have an eventpipe ID assigned when the runtime starts an EventPipe session.

interface DiagnosticServerConnection {
    readonly type: string;
    get diagnosticSessionID(): EventPipeSessionDiagnosticServerID;
    get socket(): WebSocket;
    addListeners(): void;
    postMessage(message: string): boolean;
}

interface DiagnosticServerEventPipeConnection extends DiagnosticServerConnection {
    type: "eventpipe";
    get sessionID(): EventPipeSessionIDImpl | null;
    setSessionID(sessionID: EventPipeSessionIDImpl): void;
}

interface ServerSessionManager {
    createSession(socket: WebSocket): DiagnosticServerConnection;
    assignEventPipeSessionID(diagnosticServerID: EventPipeSessionDiagnosticServerID, sessionID: EventPipeSessionIDImpl): void;
    getSession(sessionID: EventPipeSessionDiagnosticServerID): DiagnosticServerEventPipeConnection | undefined;
}

enum ListenerState {
    AwaitingCommand,
    DispatchedCommand,
    SendingTrailingData,
    Closed,
    Error
}

function assertNever(x: never): never {
    throw new Error("Unexpected object: " + x);
}

class EventPipeServerConnection implements DiagnosticServerEventPipeConnection {
    readonly type = "eventpipe";
    private _diagnostic_server_id: EventPipeSessionDiagnosticServerID;
    private _sessionID: EventPipeSessionIDImpl | null = null;
    private readonly _socket: WebSocket;
    private _state: ListenerState;
    constructor(socket: WebSocket, diagnostic_server_id: EventPipeSessionDiagnosticServerID) {
        this._socket = socket;
        this._diagnostic_server_id = diagnostic_server_id;
        this._state = ListenerState.AwaitingCommand;
    }
    get diagnosticSessionID(): EventPipeSessionDiagnosticServerID {
        return this._diagnostic_server_id;
    }
    get socket(): WebSocket {
        return this._socket;
    }
    get sessionID(): EventPipeSessionIDImpl | null {
        return this._sessionID;
    }
    setSessionID(sessionID: EventPipeSessionIDImpl) {
        if (this._sessionID !== null)
            throw new Error("Session ID already set");
        this._sessionID = sessionID;
    }

    public close() {
        switch (this._state) {
            case ListenerState.Error:
                return;
            case ListenerState.Closed:
                return;
            default:
                this._state = ListenerState.Closed;
                this._socket.close();
                return;
        }
    }

    public postMessage(message: string): boolean {
        switch (this._state) {
            case ListenerState.AwaitingCommand:
                throw new Error("Unexpected postMessage: " + message);
            case ListenerState.DispatchedCommand:
                this._state = ListenerState.SendingTrailingData;
                this._socket.send(message);
                return true;
            case ListenerState.SendingTrailingData:
                this._socket.send(message);
                return true;
            case ListenerState.Closed:
                // ignore
                return false;
            case ListenerState.Error:
                return false;
        }
    }

    private _onMessage(/*event: MessageEvent*/) {
        switch (this._state) {
            case ListenerState.AwaitingCommand:
                /* TODO process command */
                this._state = ListenerState.DispatchedCommand;
                break;
            case ListenerState.DispatchedCommand:
            case ListenerState.SendingTrailingData:
                /* unexpected message */
                this._state = ListenerState.Error;
                break;
            case ListenerState.Closed:
                /* ignore */
                break;
            case ListenerState.Error:
                /* ignore */
                break;
            default:
                assertNever(this._state);
        }

    }

    private _onClose(/*event: CloseEvent*/) {
        switch (this._state) {
            case ListenerState.Closed:
                return; /* do nothing */
            case ListenerState.Error:
                return; /* do nothing */
            default:
                this._state = ListenerState.Closed;
                // TODO: notify runtime that connection is closed
                return;
        }
    }

    private _onError(/*event: Event*/) {
        this._state = ListenerState.Error;
    }

    public addListeners() {
        this._socket.addEventListener("message", this._onMessage.bind(this));
        this._socket.addEventListener("close", this._onClose.bind(this));
        this._socket.addEventListener("error", this._onError.bind(this));
    }
}

class SessionManager implements ServerSessionManager {
    private readonly sessions: Map<EventPipeSessionDiagnosticServerID, DiagnosticServerEventPipeConnection> = new Map();
    private _nextSessionID: EventPipeSessionDiagnosticServerID = 1;
    createSession(socket: WebSocket): DiagnosticServerConnection {
        const id = this._nextSessionID;
        this._nextSessionID++;
        const session = new EventPipeServerConnection(socket, id);
        this.sessions.set(id, session);
        return session;
    }
    assignEventPipeSessionID(diagnosticServerID: number, sessionID: number): void {
        const session = this.sessions.get(diagnosticServerID);
        if (session) {
            session.setSessionID(sessionID);
        }
    }
    getSession(sessionID: number): DiagnosticServerEventPipeConnection | undefined {
        return this.sessions.get(sessionID);
    }
}

function advertiseSession(session: DiagnosticServerConnection): void {
    // TODO: send ADVR message to client and wait for response
    console.debug("TODO: advertiseSession");
    session.addListeners();
    session.socket.send("ADVR"); // FIXME: this is a dummy response
}
function startServer(url: string): SessionManager {

    const sessionManager = new SessionManager();
    const webSocket = new WebSocket(url);
    webSocket.addEventListener("open", () => {
        console.log("WebSocket opened");
        const session = sessionManager.createSession(webSocket);
        console.log("session created");
        advertiseSession(session);
    });
    console.debug("started server");
    // TODO: connect again and advertise for the next command
    return sessionManager;
}

let sessionManager: SessionManager | null = null;

function controlCommandReceived(event: MessageEvent<DiagnosticServerControlCommand>): void {
    console.debug("get in loser, we're going to vegas", event.data);
    const cmd = event.data;
    if (cmd.type === undefined) {
        console.error("control command has no type property");
        return;
    }

    switch (cmd.type) {
        case "start":
            if (sessionManager !== null)
                throw new Error("server already started");
            sessionManager = startServer(cmd.url);
            break;
        case "set_session_id":
            if (sessionManager === null)
                throw new Error("server not started");
            sessionManager.assignEventPipeSessionID(cmd.diagnostic_server_id, cmd.session_id);
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
