// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type {
    EventPipeSessionIDImpl,
} from "../shared/types";

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

export class EventPipeServerConnection {
    readonly type = "eventpipe";
    private _sessionID: EventPipeSessionIDImpl | null = null;
    private _state: ListenerState;
    constructor(readonly socket: WebSocket) {
        this._state = ListenerState.AwaitingCommand;
    }
    get sessionID(): EventPipeSessionIDImpl | null {
        return this._sessionID;
    }
    setSessionID(sessionID: EventPipeSessionIDImpl): void {
        if (this._sessionID !== null)
            throw new Error("Session ID already set");
        this._sessionID = sessionID;
    }

    close(): void {
        switch (this._state) {
            case ListenerState.Error:
                return;
            case ListenerState.Closed:
                return;
            default:
                this._state = ListenerState.Closed;
                this.socket.close();
                return;
        }
    }

    postMessage(message: string): boolean {
        switch (this._state) {
            case ListenerState.AwaitingCommand:
                throw new Error("Unexpected postMessage: " + message);
            case ListenerState.DispatchedCommand:
                this._state = ListenerState.SendingTrailingData;
                this.socket.send(message);
                return true;
            case ListenerState.SendingTrailingData:
                this.socket.send(message);
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

    addListeners(): void {
        this.socket.addEventListener("message", this._onMessage.bind(this));
        this.socket.addEventListener("close", this._onClose.bind(this));
        this.socket.addEventListener("error", this._onError.bind(this));
    }
}

