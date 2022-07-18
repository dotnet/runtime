// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CommonSocket } from "./common-socket";
import type {
    ProtocolClientCommandBase,
    EventPipeClientCommandBase,
    EventPipeCommandCollectTracing2,
    EventPipeCollectTracingCommandProvider,
    EventPipeCommandStopTracing,
    ProcessClientCommandBase,
    ProcessCommandResumeRuntime,
} from "./protocol-client-commands";

export const dotnetDiagnosticsServerProtocolCommandEvent = "dotnet:diagnostics:protocolCommand" as const;

// Just the minimal info we can pull from the
export interface BinaryProtocolCommand {
    commandSet: number;
    command: number;
    payload: Uint8Array;
}

export function isBinaryProtocolCommand(x: object): x is BinaryProtocolCommand {
    return "commandSet" in x && "command" in x && "payload" in x;
}

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


interface ParseResultBase {
    readonly success: boolean;
}

interface ParseResultOk extends ParseResultBase {
    readonly success: true;
}

interface ParseResultBinaryCommandOk extends ParseResultOk {
    readonly command: BinaryProtocolCommand | undefined;
    readonly newState: State;
}

interface ParseResultFail extends ParseResultBase {
    readonly success: false;
    readonly error: string;
}

type ParseResult = ParseResultBinaryCommandOk | ParseResultFail;

class ProtocolSocketImpl implements ProtocolSocket {
    private state: State = { state: InState.Idle };
    private protocolListeners = 0;
    private readonly messageListener: (this: CommonSocket, ev: MessageEvent) => void = this.onMessage.bind(this);
    constructor(private readonly sock: CommonSocket) {
    }
    onMessage(this: ProtocolSocketImpl, ev: MessageEvent): void {
        console.debug("protocol socket received message", ev.data);
        if (typeof ev.data === "object" && ev.data instanceof ArrayBuffer) {
            this.onArrayBuffer(ev.data);
        } else if (typeof ev.data === "object" && ev.data instanceof Blob) {
            ev.data.arrayBuffer().then(this.onArrayBuffer.bind(this));
        }
        // otherwise it's string, ignore it.
    }

    dispatchEvent(evt: Event): boolean {
        return this.sock.dispatchEvent(evt);
    }

    onArrayBuffer(this: ProtocolSocketImpl, buf: ArrayBuffer) {
        console.debug("protocol-socket: parsing array buffer", buf);
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
            console.debug("protocol-socket: got result", result);
            this.setState(result.newState);
            if (result.command) {
                const command = result.command;
                console.debug("protocol-socket: queueing command", command);
                queueMicrotask(() => {
                    console.debug("dispatching protocol event with command", command);
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
        if (size === undefined || size < Parser.MinimalHeaderSize) {
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
            const pos = { pos: Parser.MinimalHeaderSize };
            let result = this.tryParseCompletedBuffer(buf, pos);
            if (overflow) {
                console.warn("additional bytes past command payload", overflow);
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
                console.debug("adding protocol listener, with a message chaser");
                this.sock.addEventListener("message", this.messageListener);
            }
            this.protocolListeners++;
        }
    }

    removeEventListener<K extends keyof ProtocolSocketEventMap>(type: K, listener: (this: ProtocolSocket, ev: ProtocolSocketEventMap[K]) => any): void;
    removeEventListener(type: string, listener: EventListenerOrEventListenerObject): void {
        if (type === dotnetDiagnosticsServerProtocolCommandEvent) {
            console.debug("removing protocol listener and message chaser");
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
        let j = pos.pos;
        for (let i = 0; i < Parser.DOTNET_IPC_V1.length; i++) {
            if (buf[j++] !== Parser.DOTNET_IPC_V1[i]) {
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
    tryParseUint32(buf: Uint8Array, pos: { pos: number }): number | undefined {
        const j = pos.pos;
        if (j + 3 >= buf.byteLength) {
            return undefined;
        }
        const size = (buf[j + 3] << 24) | (buf[j + 2] << 16) | (buf[j + 1] << 8) | buf[j];
        Parser.advancePos(pos, 4);
        return size;
    },
    tryParseUint64(buf: Uint8Array, pos: { pos: number }): [number, number] | undefined {
        const lo = Parser.tryParseUint32(buf, pos);
        if (lo === undefined)
            return undefined;
        const hi = Parser.tryParseUint32(buf, pos);
        if (hi === undefined)
            return undefined;
        return [lo, hi];
    },
    tryParseBool(buf: Uint8Array, pos: { pos: number }): boolean | undefined {
        const r = Parser.tryParseUint8(buf, pos);
        if (r === undefined)
            return undefined;
        return r !== 0;
    },
    tryParseArraySize(buf: Uint8Array, pos: { pos: number }): number | undefined {
        const r = Parser.tryParseUint32(buf, pos);
        if (r === undefined)
            return undefined;
        return r;
    },
    tryParseStringLength(buf: Uint8Array, pos: { pos: number }): number | undefined {
        return Parser.tryParseArraySize(buf, pos);
    },
    tryParseUtf16String(buf: Uint8Array, pos: { pos: number }): string | undefined {
        const length = Parser.tryParseStringLength(buf, pos);
        if (length === undefined)
            return undefined;
        const j = pos.pos;
        if (j + length * 2 > buf.byteLength) {
            return undefined;
        }
        const result = new Array<number>(length);
        for (let i = 0; i < length; i++) {
            result[i] = (buf[j + 2 * i + 1] << 8) | buf[j + 2 * i];
        }
        Parser.advancePos(pos, length * 2);
        return String.fromCharCode.apply(null, result);
    }
};


const enum CommandSetId {
    Reserved = 0,
    Dump = 1,
    EventPipe = 2,
    Profiler = 3,
    Process = 4,
    /* future*/

    // replies
    Server = 0xFF,
}

interface ParseClientCommandResultOk<C = ProtocolClientCommandBase> extends ParseResultOk {
    readonly result: C;
}

export type ParseClientCommandResult<C = ProcessClientCommandBase> = ParseClientCommandResultOk<C> | ParseResultFail;

export function parseBinaryProtocolCommand(cmd: BinaryProtocolCommand): ParseClientCommandResult<ProtocolClientCommandBase> {
    switch (cmd.commandSet) {
        case CommandSetId.Reserved:
            throw new Error("unexpected reserved command_set command");
        case CommandSetId.Dump:
            throw new Error("TODO");
        case CommandSetId.EventPipe:
            return parseEventPipeCommand(cmd);
        case CommandSetId.Profiler:
            throw new Error("TODO");
        case CommandSetId.Process:
            return parseProcessCommand(cmd);
        default:
            return { success: false, error: `unexpected command_set ${cmd.commandSet} command` };
    }
}

const enum EventPipeCommandId {
    StopTracing = 1,
    CollectTracing = 2,
    CollectTracing2 = 3,
}

function parseEventPipeCommand(cmd: BinaryProtocolCommand & { commandSet: CommandSetId.EventPipe }): ParseClientCommandResult<EventPipeClientCommandBase> {
    switch (cmd.command) {
        case EventPipeCommandId.StopTracing:
            return parseEventPipeStopTracing(cmd);
        case EventPipeCommandId.CollectTracing:
            throw new Error("TODO");
        case EventPipeCommandId.CollectTracing2:
            return parseEventPipeCollectTracing2(cmd);
        default:
            console.warn("unexpected EventPipe command: " + cmd.command);
            return { success: false, error: `unexpected EventPipe command ${cmd.command}` };
    }
}

function parseEventPipeCollectTracing2(cmd: BinaryProtocolCommand & { commandSet: CommandSetId.EventPipe, command: EventPipeCommandId.CollectTracing2 }): ParseClientCommandResult<EventPipeCommandCollectTracing2> {
    const pos = { pos: 0 };
    const buf = cmd.payload;
    const circularBufferMB = Parser.tryParseUint32(buf, pos);
    if (circularBufferMB === undefined) {
        return { success: false, error: "failed to parse circularBufferMB in EventPipe CollectTracing2 command" };
    }
    const format = Parser.tryParseUint32(buf, pos);
    if (format === undefined) {
        return { success: false, error: "failed to parse format in EventPipe CollectTracing2 command" };
    }
    const requestRundown = Parser.tryParseBool(buf, pos);
    if (requestRundown === undefined) {
        return { success: false, error: "failed to parse requestRundown in EventPipe CollectTracing2 command" };
    }
    const numProviders = Parser.tryParseArraySize(buf, pos);
    if (numProviders === undefined) {
        return { success: false, error: "failed to parse numProviders in EventPipe CollectTracing2 command" };
    }
    const providers = new Array<EventPipeCollectTracingCommandProvider>(numProviders);
    for (let i = 0; i < numProviders; i++) {
        const result = parseEventPipeCollectTracingCommandProvider(buf, pos);
        if (!result.success) {
            return result;
        }
        providers[i] = result.result;
    }
    const command: EventPipeCommandCollectTracing2 = { command_set: "EventPipe", command: "CollectTracing2", circularBufferMB, format, requestRundown, providers };
    return { success: true, result: command };
}

function parseEventPipeCollectTracingCommandProvider(buf: Uint8Array, pos: { pos: number }): ParseClientCommandResult<EventPipeCollectTracingCommandProvider> {
    const keywords = Parser.tryParseUint64(buf, pos);
    if (keywords === undefined) {
        return { success: false, error: "failed to parse keywords in EventPipe CollectTracing provider" };
    }
    const logLevel = Parser.tryParseUint32(buf, pos);
    if (logLevel === undefined)
        return { success: false, error: "failed to parse logLevel in EventPipe CollectTracing provider" };
    const providerName = Parser.tryParseUtf16String(buf, pos);
    if (providerName === undefined)
        return { success: false, error: "failed to parse providerName in EventPipe CollectTracing provider" };
    const filterData = Parser.tryParseUtf16String(buf, pos);
    if (filterData === undefined)
        return { success: false, error: "failed to parse filterData in EventPipe CollectTracing provider" };
    const provider: EventPipeCollectTracingCommandProvider = { keywords, logLevel, provider_name: providerName, filter_data: filterData };
    return { success: true, result: provider };
}

function parseEventPipeStopTracing(cmd: BinaryProtocolCommand & { commandSet: CommandSetId.EventPipe, command: EventPipeCommandId.StopTracing }): ParseClientCommandResult<EventPipeCommandStopTracing> {
    const pos = { pos: 0 };
    const buf = cmd.payload;
    const sessionID = Parser.tryParseUint64(buf, pos);
    if (sessionID === undefined) {
        return { success: false, error: "failed to parse sessionID in EventPipe StopTracing command" };
    }
    const [lo, hi] = sessionID;
    if (hi !== 0) {
        return { success: false, error: "sessionID is too large in EventPipe StopTracing command" };
    }
    const command: EventPipeCommandStopTracing = { command_set: "EventPipe", command: "StopTracing", sessionID: lo };
    return { success: true, result: command };
}

const enum ProcessCommandId {
    ProcessInfo = 0,
    ResumeRuntime = 1,
    ProcessEnvironment = 2,
    ProcessInfo2 = 4,
}

function parseProcessCommand(cmd: BinaryProtocolCommand & { commandSet: CommandSetId.Process }): ParseClientCommandResult<ProcessClientCommandBase> {
    switch (cmd.command) {
        case ProcessCommandId.ProcessInfo:
            throw new Error("TODO");
        case ProcessCommandId.ResumeRuntime:
            return parseProcessResumeRuntime(cmd);
        case ProcessCommandId.ProcessEnvironment:
            throw new Error("TODO");
        case ProcessCommandId.ProcessInfo2:
            throw new Error("TODO");
        default:
            console.warn("unexpected Process command: " + cmd.command);
            return { success: false, error: `unexpected Process command ${cmd.command}` };
    }
}

function parseProcessResumeRuntime(cmd: BinaryProtocolCommand & { commandSet: CommandSetId.Process, command: ProcessCommandId.ResumeRuntime }): ParseClientCommandResult<ProcessCommandResumeRuntime> {
    const buf = cmd.payload;
    if (buf.byteLength !== 0) {
        return { success: false, error: "unexpected payload in Process ResumeRuntime command" };
    }
    const command: ProcessCommandResumeRuntime = { command_set: "Process", command: "ResumeRuntime" };
    return { success: true, result: command };
}

const enum ServerCommandId {
    OK = 0,
    Error = 0xFF,
}

const Serializer = {
    advancePos(pos: { pos: number }, count: number): void {
        pos.pos += count;
    },
    serializeMagic(buf: Uint8Array, pos: { pos: number }): void {
        buf.set(Parser.DOTNET_IPC_V1, pos.pos);
        Serializer.advancePos(pos, Parser.DOTNET_IPC_V1.byteLength);
    },
    serializeUint8(buf: Uint8Array, pos: { pos: number }, value: number): void {
        buf[pos.pos++] = value;
    },
    serializeUint16(buf: Uint8Array, pos: { pos: number }, value: number): void {
        buf[pos.pos++] = value & 0xFF;
        buf[pos.pos++] = (value >> 8) & 0xFF;
    },
    serializeHeader(buf: Uint8Array, pos: { pos: number }, commandSet: CommandSetId, command: ServerCommandId, len: number): void {
        Serializer.serializeMagic(buf, pos);
        Serializer.serializeUint16(buf, pos, len);
        Serializer.serializeUint8(buf, pos, commandSet);
        Serializer.serializeUint8(buf, pos, command);
        Serializer.serializeUint16(buf, pos, 0); // reserved
    }
};

export function createBinaryCommandOKReply(payload?: Uint8Array): Uint8Array {
    const fullHeaderSize = Parser.MinimalHeaderSize // magic, len
        + 2 // commandSet, command
        + 2; // reserved ;
    const len = fullHeaderSize + (payload !== undefined ? payload.byteLength : 0); // magic, size, commandSet, command, reserved
    const buf = new Uint8Array(len);
    const pos = { pos: 0 };
    Serializer.serializeHeader(buf, pos, CommandSetId.Server, ServerCommandId.OK, len);
    if (payload !== undefined) {
        buf.set(payload, pos.pos);
        Serializer.advancePos(pos, payload.byteLength);
    }
    return buf;
}
