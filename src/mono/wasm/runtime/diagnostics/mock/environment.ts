// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { RemoveCommandSetAndId, EventPipeCommandCollectTracing2, EventPipeCommandStopTracing } from "../server_pthread/protocol-client-commands";
import type { FilterPredicate, MockEnvironment } from "./types";
import Serializer from "../server_pthread/ipc-protocol/base-serializer";
import { CommandSetId, EventPipeCommandId, ProcessCommandId } from "../server_pthread/ipc-protocol/types";
import { assertNever } from "../../types/internal";
import { pthread_self } from "../../pthreads/worker";
import { createPromiseController, mono_assert } from "../../globals";


function expectAdvertise(data: ArrayBuffer): boolean {
    if (typeof (data) === "string") {
        assertNever(data);
    } else {
        const view = new Uint8Array(data);
        const ADVR_V1 = Array.from("ADVR_V1\0").map((c) => c.charCodeAt(0));
        /* TODO: check that the message is really long enough for the cookie, process ID and reserved bytes */
        return view.length >= ADVR_V1.length && ADVR_V1.every((v, i) => v === view[i]);
    }
}

function expectOk(payloadLength?: number): FilterPredicate {
    return (data) => {
        if (typeof (data) === "string") {
            assertNever(data);
        } else {
            const view = new Uint8Array(data);
            const extra = payloadLength !== undefined ? payloadLength : 0;
            return view.length >= (20 + extra) && view[16] === 0xFF && view[17] == 0x00;
        }
    };
}

function extractOkSessionID(data: ArrayBuffer): number {
    if (typeof (data) === "string") {
        assertNever(data);
    } else {
        const view = new Uint8Array(data, 20, 8);
        const sessionIDLo = view[0] | (view[1] << 8) | (view[2] << 16) | (view[3] << 24);
        const sessionIDHi = view[4] | (view[5] << 8) | (view[6] << 16) | (view[7] << 24);
        mono_assert(sessionIDHi === 0, "mock: sessionIDHi should be zero");
        return sessionIDLo;
    }
}

function computeStringByteLength(s: string | null): number {
    if (s === undefined || s === null || s === "")
        return 4; // just length of zero
    return 4 + 2 * s.length + 2; // length + UTF16 + null
}

function computeCollectTracing2PayloadByteLength(payload: RemoveCommandSetAndId<EventPipeCommandCollectTracing2>): number {
    let len = 0;
    len += 4; // circularBufferMB
    len += 4; // format
    len += 1; // requestRundown
    len += 4; // providers length
    for (const provider of payload.providers) {
        len += 8; // keywords
        len += 4; // level
        len += computeStringByteLength(provider.provider_name);
        len += computeStringByteLength(provider.filter_data);
    }
    return len;
}

function makeEventPipeCollectTracing2(payload: RemoveCommandSetAndId<EventPipeCommandCollectTracing2>): Uint8Array {
    const payloadLength = computeCollectTracing2PayloadByteLength(payload);
    const messageLength = Serializer.computeMessageByteLength(payloadLength);
    const buffer = new Uint8Array(messageLength);
    const pos = { pos: 0 };
    Serializer.serializeHeader(buffer, pos, CommandSetId.EventPipe, EventPipeCommandId.CollectTracing2, messageLength);
    Serializer.serializeUint32(buffer, pos, payload.circularBufferMB);
    Serializer.serializeUint32(buffer, pos, payload.format);
    Serializer.serializeUint8(buffer, pos, payload.requestRundown ? 1 : 0);
    Serializer.serializeUint32(buffer, pos, payload.providers.length);
    for (const provider of payload.providers) {
        Serializer.serializeUint64(buffer, pos, provider.keywords);
        Serializer.serializeUint32(buffer, pos, provider.logLevel);
        Serializer.serializeString(buffer, pos, provider.provider_name);
        Serializer.serializeString(buffer, pos, provider.filter_data);
    }
    return buffer;
}

function makeEventPipeStopTracing(payload: RemoveCommandSetAndId<EventPipeCommandStopTracing>): Uint8Array {
    const payloadLength = 8;
    const messageLength = Serializer.computeMessageByteLength(payloadLength);
    const buffer = new Uint8Array(messageLength);
    const pos = { pos: 0 };
    Serializer.serializeHeader(buffer, pos, CommandSetId.EventPipe, EventPipeCommandId.StopTracing, messageLength);
    Serializer.serializeUint32(buffer, pos, payload.sessionID);
    Serializer.serializeUint32(buffer, pos, 0);
    return buffer;
}

function makeProcessResumeRuntime(): Uint8Array {
    const payloadLength = 0;
    const messageLength = Serializer.computeMessageByteLength(payloadLength);
    const buffer = new Uint8Array(messageLength);
    const pos = { pos: 0 };
    Serializer.serializeHeader(buffer, pos, CommandSetId.Process, ProcessCommandId.ResumeRuntime, messageLength);
    return buffer;
}

function postMessageToBrowser(message: any, transferable?: Transferable[]): void {
    pthread_self.postMessageToBrowser({
        type: "diagnostic_server_mock",
        ...message
    }, transferable);
}

function addEventListenerFromBrowser(cmd: string, listener: (data: any) => void) {
    pthread_self.addEventListenerFromBrowser((event) => {
        if (event.data.cmd === cmd) listener(event.data);
    });
}

export function createMockEnvironment(): MockEnvironment {
    const command = {
        makeEventPipeCollectTracing2,
        makeEventPipeStopTracing,
        makeProcessResumeRuntime,
    };
    const reply = {
        expectOk,
        extractOkSessionID,
    };
    return {
        postMessageToBrowser,
        addEventListenerFromBrowser,
        createPromiseController,
        delay: (ms: number) => new Promise(resolve => globalThis.setTimeout(resolve, ms)),
        command,
        reply,
        expectAdvertise
    };
}
