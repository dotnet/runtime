// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { assertNever } from "../../types";
import { MockRemoteSocket } from "../mock";
import { VoidPtr } from "../../types/emscripten";
import { Module } from "../../imports";

enum ListenerState {
    SendingTrailingData,
    Closed,
    Error
}


// the common bits that we depend on from a real WebSocket or a MockRemoteSocket used for testing
interface CommonSocket {
    addEventListener<T extends keyof WebSocketEventMap>(type: T, listener: (this: CommonSocket, ev: WebSocketEventMap[T]) => any, options?: boolean | AddEventListenerOptions): void;
    addEventListener(event: string, listener: EventListenerOrEventListenerObject, options?: boolean | AddEventListenerOptions): void;
    removeEventListener(event: string, listener: EventListenerOrEventListenerObject): void;
    send(data: string | ArrayBuffer | Uint8Array | Blob | DataView): void;
    close(): void;
}

type AssignableTo<T, Q> = Q extends T ? true : false;

function static_assert<Cond extends boolean>(x: Cond): asserts x is Cond { /*empty*/ }

{
    static_assert<AssignableTo<CommonSocket, WebSocket>>(true);
    static_assert<AssignableTo<CommonSocket, MockRemoteSocket>>(true);

    static_assert<AssignableTo<{ x: number }, { y: number }>>(false); // sanity check that static_assert works
}


class SocketGuts {
    constructor(private readonly ws: CommonSocket) { }
    close(): void {
        this.ws.close();
    }
    write(data: VoidPtr, size: number): void {
        const buf = new ArrayBuffer(size);
        const view = new Uint8Array(buf);
        // Can we avoid this copy?
        view.set(new Uint8Array(Module.HEAPU8.buffer, data as unknown as number, size));
        this.ws.send(buf);
    }
}

export class EventPipeSocketConnection {
    private _state: ListenerState;
    readonly stream: SocketGuts;
    constructor(readonly socket: CommonSocket) {
        this._state = ListenerState.SendingTrailingData;
        this.stream = new SocketGuts(socket);
    }

    close(): void {
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
            case ListenerState.SendingTrailingData:
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
            case ListenerState.SendingTrailingData:
                /* unexpected message */
                console.warn("EventPipe session stream received unexpected message from websocket", event);
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

    private _onError(/*event: Event*/) {
        this._state = ListenerState.Error;
        this.stream.close();
        // TODO: notify runtime that connection had an error
    }

    addListeners(): void {
        this.socket.addEventListener("message", this._onMessage.bind(this));
        this.socket.addEventListener("close", this._onClose.bind(this));
        this.socket.addEventListener("error", this._onError.bind(this));
    }
}

export function takeOverSocket(socket: CommonSocket): EventPipeSocketConnection {
    const connection = new EventPipeSocketConnection(socket);
    connection.addListeners();
    return connection;
}
