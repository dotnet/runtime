// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Serializer from "./base-serializer";
import { CommandSetId, ServerCommandId } from "./types";
import { mono_assert } from "../../../types";

export function createBinaryCommandOKReply(payload?: Uint8Array): Uint8Array {
    const len = Serializer.computeMessageByteLength(payload);
    const buf = new Uint8Array(len);
    const pos = { pos: 0 };
    Serializer.serializeHeader(buf, pos, CommandSetId.Server, ServerCommandId.OK, len);
    if (payload !== undefined) {
        Serializer.serializePayload(buf, pos, payload);
    }
    return buf;
}

function serializeGuid(buf: Uint8Array, pos: { pos: number }, guid: string): void {
    guid.split("-").forEach((part) => {
        // FIXME: I'm sure the endianness is wrong here
        for (let i = 0; i < part.length; i += 2) {
            const idx = part.length - i - 2; // go through the pieces backwards
            buf[pos.pos++] = Number.parseInt(part.substring(idx, idx + 2), 16);
        }
    });
}

function serializeAsciiLiteralString(buf: Uint8Array, pos: { pos: number }, s: string): void {
    const len = s.length;
    const hasNul = s[len - 1] === "\0";
    for (let i = 0; i < len; i++) {
        Serializer.serializeUint8(buf, pos, s.charCodeAt(i));
    }
    if (!hasNul) {
        Serializer.serializeUint8(buf, pos, 0);
    }
}


export function createAdvertise(guid: string, processId: [/*lo*/ number, /*hi*/number]): Uint8Array {
    const BUF_LENGTH = 34;
    const buf = new Uint8Array(BUF_LENGTH);
    const pos = { pos: 0 };
    const advrText = "ADVR_V1\0";
    serializeAsciiLiteralString(buf, pos, advrText);
    serializeGuid(buf, pos, guid);
    Serializer.serializeUint64(buf, pos, processId);
    Serializer.serializeUint16(buf, pos, 0); // reserved
    mono_assert(pos.pos == BUF_LENGTH, "did not format ADVR_V1 correctly");
    return buf;
}
