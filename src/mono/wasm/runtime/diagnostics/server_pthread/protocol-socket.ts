// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CommonSocket } from "./common-socket";
import {
    BinaryProtocolCommand,
    ParseResultFail,
    ParseResultOk,
} from "./ipc-protocol/types";
import Magic from "./ipc-protocol/magic";
import Parser from "./ipc-protocol/base-parser";
import { assertNever } from "../../types/internal";

export const dotnetDiagnosticsServerProtocolCommandEvent = "dotnet:diagnostics:protocolCommand" as const;

export interface ProtocolCommandEvent extends Event {
    type: typeof dotnetDiagnosticsServerProtocolCommandEvent;
    data: BinaryProtocolCommand;
}

export interface ProtocolSocketEventMap extends WebSocketEventMap {
    [dotnetDiagnosticsServerProtocolCommandEvent]: ProtocolCommandEvent;
}

/// An adapter that takes a websocket connection and converts MessageEvent into ProtocolCommandEvent by
/// parsing the command.
interface ProtocolSocket {
    addEventListener<K extends keyof ProtocolSocketEventMap>(type: K, listener: (this: ProtocolSocket, ev: ProtocolSocketEventMap[K]) => any, options?: boolean | AddEventListenerOptions): void;
    addEventListener(type: string, listener: EventListenerOrEventListenerObject, options?: boolean | AddEventListenerOptions): void;
    removeEventListener<K extends keyof ProtocolSocketEventMap>(type: K, listener: (this: ProtocolSocket, ev: ProtocolSocketEventMap[K]) => any, options?: boolean | EventListenerOptions): void;
    removeEventListener(type: string, listener: EventListenerOrEventListenerObject, options?: boolean | EventListenerOptions): void;
    send(buf: Uint8Array): void;
    dispatchEvent(evt: Event): boolean;
}

enum InState {
    Idle,
    PartialCommand, // we received part of a command, but not the complete thing
    Error, // something went wrong, we won't dispatch any more ProtocolCommandEvents
}

type State = { state: InState.Idle | InState.Error; } | PartialCommandState;

interface PartialCommandState {
    state: InState.PartialCommand;
    buf: Uint8Array; /* partially received command */
    size: number; /* number of bytes of partial command */
}


export interface ParseResultBinaryCommandOk extends ParseResultOk {
    readonly command: BinaryProtocolCommand | undefined;
    readonly newState: State;
}

export type ParseResult = ParseResultBinaryCommandOk | ParseResultFail;

/// A helper object that accumulates command data that is received and provides parsed commands
class StatefulParser {
    private state: State = { state: InState.Idle };

    constructor(private readonly emitCommandCallback: (command: BinaryProtocolCommand) => void) { }

    /// process the data in the given buffer and update the state.
    receiveBuffer(buf: ArrayBuffer): void {
        if (this.state.state == InState.Error) {
            return;
        }
        let result: ParseResult;
        if (this.state.state === InState.Idle) {
            result = this.tryParseHeader(new Uint8Array(buf));
        } else {
            result = this.tryAppendBuffer(new Uint8Array(buf));
        }
        if (result.success) {
            console.debug("MONO_WASM: protocol-socket: got result", result);
            this.setState(result.newState);
            if (result.command) {
                const command = result.command;
                this.emitCommandCallback(command);
            }
        } else {
            console.warn("MONO_WASM: socket received invalid command header", buf, result.error);
            // FIXME: dispatch error event?
            this.setState({ state: InState.Error });
        }
    }

    tryParseHeader(buf: Uint8Array): ParseResult {
        const pos = { pos: 0 };
        if (buf.byteLength < Magic.MinimalHeaderSize) {
            // TODO: we need to see the magic and the size to make a partial commmand
            return { success: false, error: "not enough data" };
        }
        if (!Parser.tryParseHeader(buf, pos)) {
            return { success: false, error: "invalid header" };
        }
        const size = Parser.tryParseSize(buf, pos);
        if (size === undefined || size < Magic.MinimalHeaderSize) {
            return { success: false, error: "invalid size" };
        }
        // make a "partially completed" state with a buffer of the right size and just the header upto the size
        // field filled in.
        const parsedSize = pos.pos;
        const partialBuf = new ArrayBuffer(size);
        const partialBufView = new Uint8Array(partialBuf);
        partialBufView.set(buf.subarray(0, parsedSize));
        const partialState: PartialCommandState = { state: InState.PartialCommand, buf: partialBufView, size: parsedSize };
        return this.continueWithBuffer(partialState, buf.subarray(parsedSize));
    }

    tryAppendBuffer(moreBuf: Uint8Array): ParseResult {
        if (this.state.state !== InState.PartialCommand) {
            return { success: false, error: "not in partial command state" };
        }
        return this.continueWithBuffer(this.state, moreBuf);
    }

    continueWithBuffer(state: PartialCommandState, moreBuf: Uint8Array): ParseResult {
        const buf = state.buf;
        let partialSize = state.size;
        let overflow: Uint8Array | null = null;
        if (partialSize + moreBuf.byteLength <= buf.byteLength) {
            buf.set(moreBuf, partialSize);
            partialSize += moreBuf.byteLength;
        } else {
            const overflowSize = partialSize + moreBuf.byteLength - buf.byteLength;
            const overflowOffset = moreBuf.byteLength - overflowSize;
            buf.set(moreBuf.subarray(0, buf.byteLength - partialSize), partialSize);
            partialSize = buf.byteLength;
            const overflowBuf = new ArrayBuffer(overflowSize);
            overflow = new Uint8Array(overflowBuf);
            overflow.set(moreBuf.subarray(overflowOffset));
        }
        if (partialSize < buf.byteLength) {
            const newState = { state: InState.PartialCommand, buf, size: partialSize };
            return { success: true, command: undefined, newState };
        } else {
            const pos = { pos: Magic.MinimalHeaderSize };
            let result = this.tryParseCompletedBuffer(buf, pos);
            if (overflow) {
                console.warn("MONO_WASM: additional bytes past command payload", overflow);
                if (result.success) {
                    const newResult: ParseResultBinaryCommandOk = { success: true, command: result.command, newState: { state: InState.Error } };
                    result = newResult;
                }
            }
            return result;
        }
    }

    tryParseCompletedBuffer(buf: Uint8Array, pos: { pos: number }): ParseResult {
        const command = Parser.tryParseCommand(buf, pos);
        if (!command) {
            this.setState({ state: InState.Error });
            return { success: false, error: "invalid command" };
        }
        return { success: true, command, newState: { state: InState.Idle } };
    }

    private setState(state: State) {
        this.state = state;
    }

    reset() {
        this.setState({ state: InState.Idle });
    }

}

class ProtocolSocketImpl implements ProtocolSocket {
    private readonly statefulParser = new StatefulParser(this.emitCommandCallback.bind(this));
    private protocolListeners = 0;
    private readonly messageListener: (this: CommonSocket, ev: MessageEvent) => void = this.onMessage.bind(this);
    constructor(private readonly sock: CommonSocket) { }

    onMessage(this: ProtocolSocketImpl, ev: MessageEvent<ArrayBuffer | Blob | string>): void {
        const data = ev.data;
        console.debug("MONO_WASM: protocol socket received message", ev.data);
        if (typeof data === "object" && data instanceof ArrayBuffer) {
            this.onArrayBuffer(data);
        } else if (typeof data === "object" && data instanceof Blob) {
            data.arrayBuffer().then(this.onArrayBuffer.bind(this));
        } else if (typeof data === "string") {
            // otherwise it's string, ignore it.
            console.debug("MONO_WASM: protocol socket received string message; ignoring it", ev.data);
        } else {
            assertNever(data);
        }
    }

    dispatchEvent(evt: Event): boolean {
        return this.sock.dispatchEvent(evt);
    }

    onArrayBuffer(this: ProtocolSocketImpl, buf: ArrayBuffer) {
        console.debug("MONO_WASM: protocol-socket: parsing array buffer", buf);
        this.statefulParser.receiveBuffer(buf);
    }

    // called by the stateful parser when it has a complete command
    emitCommandCallback(this: this, command: BinaryProtocolCommand): void {
        console.debug("MONO_WASM: protocol-socket: queueing command", command);
        queueMicrotask(() => {
            console.debug("MONO_WASM: dispatching protocol event with command", command);
            this.dispatchProtocolCommandEvent(command);
        });
    }


    dispatchProtocolCommandEvent(cmd: BinaryProtocolCommand): void {
        const ev = new Event(dotnetDiagnosticsServerProtocolCommandEvent);
        (<any>ev).data = cmd; // FIXME: use a proper event subclass
        this.sock.dispatchEvent(ev);
    }

    addEventListener<K extends keyof ProtocolSocketEventMap>(type: K, listener: (this: ProtocolSocket, ev: ProtocolSocketEventMap[K]) => any, options?: boolean | AddEventListenerOptions | undefined): void;
    addEventListener(type: string, listener: EventListenerOrEventListenerObject, options?: boolean | AddEventListenerOptions | undefined): void {
        this.sock.addEventListener(type, listener, options);
        if (type === dotnetDiagnosticsServerProtocolCommandEvent) {
            if (this.protocolListeners === 0) {
                console.debug("MONO_WASM: adding protocol listener, with a message chaser");
                this.sock.addEventListener("message", this.messageListener);
            }
            this.protocolListeners++;
        }
    }

    removeEventListener<K extends keyof ProtocolSocketEventMap>(type: K, listener: (this: ProtocolSocket, ev: ProtocolSocketEventMap[K]) => any): void;
    removeEventListener(type: string, listener: EventListenerOrEventListenerObject): void {
        if (type === dotnetDiagnosticsServerProtocolCommandEvent) {
            console.debug("MONO_WASM: removing protocol listener and message chaser");
            this.protocolListeners--;
            if (this.protocolListeners === 0) {
                this.sock.removeEventListener("message", this.messageListener);
                this.statefulParser.reset();
            }
        }
        this.sock.removeEventListener(type, listener);
    }

    send(buf: Uint8Array) {
        this.sock.send(buf);
    }

    close() {
        this.sock.close();
        this.statefulParser.reset();
    }

}

export function createProtocolSocket(socket: CommonSocket): ProtocolSocket {
    return new ProtocolSocketImpl(socket);
}

