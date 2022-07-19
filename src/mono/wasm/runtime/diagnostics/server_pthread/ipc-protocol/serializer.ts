// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Serializer from "./base-serializer";
import { CommandSetId, ServerCommandId } from "./types";

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
