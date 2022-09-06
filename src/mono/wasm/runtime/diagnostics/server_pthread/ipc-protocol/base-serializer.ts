// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { CommandSetId, ServerCommandId, EventPipeCommandId, ProcessCommandId } from "./types";
import Magic from "./magic";

function advancePos(pos: { pos: number }, count: number): void {
    pos.pos += count;
}


function serializeHeader(buf: Uint8Array, pos: { pos: number }, commandSet: CommandSetId.EventPipe, command: EventPipeCommandId, len: number): void;
function serializeHeader(buf: Uint8Array, pos: { pos: number }, commandSet: CommandSetId.Process, command: ProcessCommandId, len: number): void;
function serializeHeader(buf: Uint8Array, pos: { pos: number }, commandSet: CommandSetId.Server, command: ServerCommandId, len: number): void;
function serializeHeader(buf: Uint8Array, pos: { pos: number }, commandSet: CommandSetId, command: EventPipeCommandId | ProcessCommandId | ServerCommandId, len: number): void {
    Serializer.serializeMagic(buf, pos);
    Serializer.serializeUint16(buf, pos, len);
    Serializer.serializeUint8(buf, pos, commandSet);
    Serializer.serializeUint8(buf, pos, command);
    Serializer.serializeUint16(buf, pos, 0); // reserved
}


const Serializer = {
    computeMessageByteLength(payload?: number | Uint8Array): number {
        const fullHeaderSize = Magic.MinimalHeaderSize // magic, len
            + 2 // commandSet, command
            + 2; // reserved ;
        const payloadLength = payload ? (payload instanceof Uint8Array ? payload.byteLength : payload) : 0;
        const len = fullHeaderSize + payloadLength; // magic, size, commandSet, command, reserved
        return len;
    },
    serializeMagic(buf: Uint8Array, pos: { pos: number }): void {
        buf.set(Magic.DOTNET_IPC_V1, pos.pos);
        advancePos(pos, Magic.DOTNET_IPC_V1.byteLength);
    },
    serializeUint8(buf: Uint8Array, pos: { pos: number }, value: number): void {
        buf[pos.pos++] = value;
    },
    serializeUint16(buf: Uint8Array, pos: { pos: number }, value: number): void {
        buf[pos.pos++] = value & 0xFF;
        buf[pos.pos++] = (value >> 8) & 0xFF;
    },
    serializeUint32(buf: Uint8Array, pos: { pos: number }, value: number): void {
        buf[pos.pos++] = value & 0xFF;
        buf[pos.pos++] = (value >> 8) & 0xFF;
        buf[pos.pos++] = (value >> 16) & 0xFF;
        buf[pos.pos++] = (value >> 24) & 0xFF;
    },
    serializeUint64(buf: Uint8Array, pos: { pos: number }, value: [number, number]): void {
        Serializer.serializeUint32(buf, pos, value[0]);
        Serializer.serializeUint32(buf, pos, value[1]);
    },
    serializeHeader,
    serializePayload(buf: Uint8Array, pos: { pos: number }, payload: Uint8Array): void {
        buf.set(payload, pos.pos);
        advancePos(pos, payload.byteLength);
    },
    serializeString(buf: Uint8Array, pos: { pos: number }, s: string | null): void {
        if (s === null) {
            Serializer.serializeUint32(buf, pos, 0);
        } else {
            const len = s.length;
            const hasNul = s[len - 1] === "\0";
            Serializer.serializeUint32(buf, pos, len + (hasNul ? 0 : 1));
            for (let i = 0; i < len; i++) {
                Serializer.serializeUint16(buf, pos, s.charCodeAt(i));
            }
            if (!hasNul) {
                Serializer.serializeUint16(buf, pos, 0);
            }
        }
    },
};

export default Serializer;
