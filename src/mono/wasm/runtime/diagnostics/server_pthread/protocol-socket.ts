// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CommonSocket } from "./common-socket";

export const dotnetDiagnosticsServerProtocolCommandEvent = "dotnet:diagnostics:protocolCommand" as const;

// Just the minimal info we can pull from the
export interface BinaryProtocolCommand {
    commandSet: number;
    command: number;
    payload: Uint8Array;
}


export interface ProtcolCommandEvent extends Event {
    type: typeof dotnetDiagnosticsServerProtocolCommandEvent;
    data: BinaryProtocolCommand;
}

export interface ProtocolSocketEventMap extends WebSocketEventMap {
    [dotnetDiagnosticsServerProtocolCommandEvent]: ProtcolCommandEvent;
}

/// An adapter that takes a websocket connection and converts MessageEvent into ProtocolCommandEvent by
/// parsing the command.
interface ProtocolSocket {
    addEventListener<K extends keyof ProtocolSocketEventMap>(type: K, listener: (this: ProtocolSocket, ev: ProtocolSocketEventMap[K]) => any, options?: boolean | AddEventListenerOptions): void;
    addEventListener(type: string, listener: EventListenerOrEventListenerObject, options?: boolean | AddEventListenerOptions): void;
    removeEventListener<K extends keyof ProtocolSocketEventMap>(type: K, listener: (this: ProtocolSocket, ev: ProtocolSocketEventMap[K]) => any, options?: boolean | EventListenerOptions): void;
    removeEventListener(type: string, listener: EventListenerOrEventListenerObject, options?: boolean | EventListenerOptions): void;
    send(buf: Uint8Array): void;
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


interface ParseResultBase {
    success: boolean;
}


interface ParseResultOk extends ParseResultBase {
    success: true
    command: BinaryProtocolCommand | undefined;
    newState: State;
}

interface ParseResultFail extends ParseResultBase {
    success: false;
    error: string;
}

type ParseResult = ParseResultOk | ParseResultFail;

class ProtocolSocketImpl implements ProtocolSocket {
    private state: State = { state: InState.Idle };
    private protocolListeners = 0;
    private readonly messageListener: (this: CommonSocket, ev: MessageEvent) => void = this.onMessage.bind(this);
    constructor(private readonly sock: CommonSocket) {
    }
    onMessage(this: ProtocolSocketImpl, ev: MessageEvent): void {
        console.debug("socket received message", ev.data);
        if (typeof ev.data === "object" && ev.data instanceof ArrayBuffer) {
            this.onArrayBuffer(ev.data);
        } else if (typeof ev.data === "object" && ev.data instanceof Blob) {
            ev.data.arrayBuffer().then(this.onArrayBuffer.bind(this));
        }
        // otherwise it's string, ignore it.
    }

    onArrayBuffer(this: ProtocolSocketImpl, buf: ArrayBuffer) {
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
            this.setState(result.newState);
            if (result.command) {
                const command = result.command;
                queueMicrotask(() => {
                    this.dispatchProtocolCommandEvent(command);
                });
            }
        } else {
            console.warn("socket received invalid command header", buf, result.error);
            // FIXME: dispatch error event?
            this.setState({ state: InState.Error });
        }
    }

    tryParseHeader(buf: Uint8Array): ParseResult {
        const pos = { pos: 0 };
        if (buf.byteLength < Parser.MinimalHeaderSize) {
            // TODO: we need to see the magic and the size to make a partial commmand
            return { success: false, error: "not enough data" };
        }
        if (!Parser.tryParseHeader(buf, pos)) {
            return { success: false, error: "invalid header" };
        }
        const size = Parser.tryParseSize(buf, pos);
        if (!size) {
            return { success: false, error: "invalid size" };
        }
        // make a "partially completed" state with a buffer of the right size and just the header upto the size
        // field filled in.
        const partialBuf = new ArrayBuffer(size);
        const partialBufView = new Uint8Array(partialBuf);
        partialBufView.set(buf.subarray(0, pos.pos));
        const partialState: PartialCommandState = { state: InState.PartialCommand, buf: partialBufView, size: 0 };
        return this.continueWithBuffer(partialState, buf.subarray(pos.pos));
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
            const pos = { pos: Parser.MinimalHeaderSize };
            const result = this.tryParseCompletedBuffer(buf, pos);
            if (overflow) {
                console.warn("additional bytes past command payload", overflow);
                if (result.success) {
                    result.newState = { state: InState.Error };
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
                this.sock.addEventListener("message", this.messageListener);
            }
            this.protocolListeners++;
        }
    }

    removeEventListener<K extends keyof ProtocolSocketEventMap>(type: K, listener: (this: ProtocolSocket, ev: ProtocolSocketEventMap[K]) => any): void;
    removeEventListener(type: string, listener: EventListenerOrEventListenerObject): void {
        if (type === dotnetDiagnosticsServerProtocolCommandEvent) {
            this.protocolListeners--;
            if (this.protocolListeners === 0) {
                this.sock.removeEventListener("message", this.messageListener);
                this.setState({ state: InState.Idle });
            }
        }
        this.sock.removeEventListener(type, listener);
    }

    send(buf: Uint8Array) {
        this.sock.send(buf);
    }

    close() {
        this.sock.close();
    }

    private setState(state: State) {
        this.state = state;
    }
}

export function createProtocolSocket(socket: CommonSocket): ProtocolSocket {
    return new ProtocolSocketImpl(socket);
}

const Parser = {
    magic_buf: null as Uint8Array | null,
    get DOTNET_IPC_V1(): Uint8Array {
        if (Parser.magic_buf === null) {
            const magic = "DOTNET_IPC_V1";
            const magic_len = magic.length + 1; // nul terminated
            Parser.magic_buf = new Uint8Array(magic_len);
            for (let i = 0; i < magic_len; i++) {
                Parser.magic_buf[i] = magic.charCodeAt(i);
            }
            Parser.magic_buf[magic_len - 1] = 0;
        }
        return Parser.magic_buf;
    },
    get MinimalHeaderSize(): number {
        // we just need to see the magic and the size
        const sizeOfSize = 2;
        return Parser.DOTNET_IPC_V1.byteLength + sizeOfSize;
    },
    advancePos(pos: { pos: number }, offset: number) {
        pos.pos += offset;
    },
    tryParseHeader(buf: Uint8Array, pos: { pos: number }): boolean {
        const j = pos.pos;
        for (let i = 0; i < Parser.DOTNET_IPC_V1.length; i++) {
            if (buf[j] !== Parser.DOTNET_IPC_V1[i]) {
                return false;
            }
        }
        Parser.advancePos(pos, Parser.DOTNET_IPC_V1.length);
        return true;
    },
    tryParseSize(buf: Uint8Array, pos: { pos: number }): number | undefined {
        return Parser.tryParseUint16(buf, pos);
    },
    tryParseCommand(buf: Uint8Array, pos: { pos: number }): BinaryProtocolCommand | undefined {
        const commandSet = Parser.tryParseUint8(buf, pos);
        if (commandSet === undefined)
            return undefined;
        const command = Parser.tryParseUint8(buf, pos);
        if (command === undefined)
            return undefined;
        if (Parser.tryParseReserved(buf, pos) === undefined)
            return undefined;
        const payload = buf.slice(pos.pos);
        const result = {
            commandSet,
            command,
            payload
        };
        return result;
    },
    tryParseReserved(buf: Uint8Array, pos: { pos: number }): true | undefined {
        const reservedLength = 2; // 2 bytes reserved, must be 0
        for (let i = 0; i < reservedLength; i++) {
            const reserved = Parser.tryParseUint8(buf, pos);
            if (reserved === undefined || reserved !== 0) {
                return undefined;
            }
        }
        return true;
    },
    tryParseUint8(buf: Uint8Array, pos: { pos: number }): number | undefined {
        const j = pos.pos;
        if (j >= buf.byteLength) {
            return undefined;
        }
        const size = buf[j];
        Parser.advancePos(pos, 1);
        return size;
    },
    tryParseUint16(buf: Uint8Array, pos: { pos: number }): number | undefined {
        const j = pos.pos;
        if (j + 1 >= buf.byteLength) {
            return undefined;
        }
        const size = (buf[j + 1] << 8) | buf[j];
        Parser.advancePos(pos, 2);
        return size;
    },

};
