// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Magic from "./magic";
import { BinaryProtocolCommand } from "./types";

function advancePos(pos: { pos: number }, offset: number): void {
    pos.pos += offset;
}

const Parser = {
    tryParseHeader(buf: Uint8Array, pos: { pos: number }): boolean {
        let j = pos.pos;
        for (let i = 0; i < Magic.DOTNET_IPC_V1.length; i++) {
            if (buf[j++] !== Magic.DOTNET_IPC_V1[i]) {
                return false;
            }
        }
        advancePos(pos, Magic.DOTNET_IPC_V1.length);
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
        advancePos(pos, 1);
        return size;
    },
    tryParseUint16(buf: Uint8Array, pos: { pos: number }): number | undefined {
        const j = pos.pos;
        if (j + 1 >= buf.byteLength) {
            return undefined;
        }
        const size = (buf[j + 1] << 8) | buf[j];
        advancePos(pos, 2);
        return size;
    },
    tryParseUint32(buf: Uint8Array, pos: { pos: number }): number | undefined {
        const j = pos.pos;
        if (j + 3 >= buf.byteLength) {
            return undefined;
        }
        const size = (buf[j + 3] << 24) | (buf[j + 2] << 16) | (buf[j + 1] << 8) | buf[j];
        advancePos(pos, 4);
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
        advancePos(pos, length * 2);

        /* Trim trailing nul character(s) that are added by the protocol */
        let trailingNulStart = -1;
        for (let i = result.length - 1; i >= 0; i--) {
            if (result[i] === 0) {
                trailingNulStart = i;
            } else {
                break;
            }
        }
        if (trailingNulStart >= 0)
            result.splice(trailingNulStart);

        return String.fromCharCode.apply(null, result);
    }
};

export default Parser;
