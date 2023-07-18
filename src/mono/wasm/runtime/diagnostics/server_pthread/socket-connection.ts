// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { assertNever } from "../../types/internal";
import { VoidPtr } from "../../types/emscripten";
import type { CommonSocket } from "./common-socket";
import { mono_log_debug, mono_log_warn } from "../../logging";
import { localHeapViewU8 } from "../../memory";
enum ListenerState {
    Sending,
    Closed,
    Error
}

class SocketGuts {
    constructor(public readonly socket: CommonSocket) { }
    close(): void {
        this.socket.close();
    }
    write(data: VoidPtr, size: number): void {
        const buf = new ArrayBuffer(size);
        const view = new Uint8Array(buf);
        // Can we avoid this copy?
        view.set(new Uint8Array(localHeapViewU8().buffer, data as unknown as number, size));
        this.socket.send(buf);
    }
}


/// A wrapper around a WebSocket that just sends data back to the host.
/// It sets up message and clsoe handlers on the WebSocket tht put it into an idle state
/// if the connection closes or we receive any replies.
export class EventPipeSocketConnection {
    private _state: ListenerState;
    readonly stream: SocketGuts;
    constructor(socket: CommonSocket) {
        this._state = ListenerState.Sending;
        this.stream = new SocketGuts(socket);
    }

    close(): void {
        mono_log_debug("EventPipe session stream closing websocket");
        switch (this._state) {
            case ListenerState.Error:
                return;
            case ListenerState.Closed:
                return;
            default:
                this._state = ListenerState.Closed;
                this.stream.close();
                return;
        }
    }

    write(ptr: VoidPtr, len: number): boolean {
        switch (this._state) {
            case ListenerState.Sending:
                this.stream.write(ptr, len);
                return true;
            case ListenerState.Closed:
                // ignore
                return false;
            case ListenerState.Error:
                return false;
        }
    }

    private _onMessage(event: MessageEvent): void {
        switch (this._state) {
            case ListenerState.Sending:
                /* unexpected message */
                mono_log_warn("EventPipe session stream received unexpected message from websocket", event);
                // TODO notify runtime that the connection had an error
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
                this.stream.close();
                // TODO: notify runtime that connection is closed
                return;
        }
    }

    private _onError(event: Event) {
        mono_log_debug("EventPipe session stream websocket error", event);
        this._state = ListenerState.Error;
        this.stream.close();
        // TODO: notify runtime that connection had an error
    }

    addListeners(): void {
        const socket = this.stream.socket;
        socket.addEventListener("message", this._onMessage.bind(this));
        addEventListener("close", this._onClose.bind(this));
        addEventListener("error", this._onError.bind(this));
    }
}

/// Take over a WebSocket that was used by the diagnostic server to receive the StartCollecting command and
/// use it for sending the event pipe data back to the host.
export function takeOverSocket(socket: CommonSocket): EventPipeSocketConnection {
    const connection = new EventPipeSocketConnection(socket);
    connection.addListeners();
    return connection;
}
