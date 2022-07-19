// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { CommandSetId, ServerCommandId } from "./types";
import Magic from "./magic";

function advancePos(pos: { pos: number }, count: number): void {
    pos.pos += count;
}


const Serializer = {
    computeMessageByteLength(payload?: Uint8Array): number {
        const fullHeaderSize = Magic.MinimalHeaderSize // magic, len
            + 2 // commandSet, command
            + 2; // reserved ;
        const len = fullHeaderSize + (payload !== undefined ? payload.byteLength : 0); // magic, size, commandSet, command, reserved
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
    serializeHeader(buf: Uint8Array, pos: { pos: number }, commandSet: CommandSetId, command: ServerCommandId, len: number): void {
        Serializer.serializeMagic(buf, pos);
        Serializer.serializeUint16(buf, pos, len);
        Serializer.serializeUint8(buf, pos, commandSet);
        Serializer.serializeUint8(buf, pos, command);
        Serializer.serializeUint16(buf, pos, 0); // reserved
    },
    serializePayload(buf: Uint8Array, pos: { pos: number }, payload: Uint8Array): void {
        buf.set(payload, pos.pos);
        advancePos(pos, payload.byteLength);
    }
};

export default Serializer;
