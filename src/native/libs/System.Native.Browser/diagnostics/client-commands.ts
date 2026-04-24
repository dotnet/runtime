// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticCommandOptions } from "../types";
import { CommandSetId, EventPipeCommandId, Keywords, PayloadV2, ProcessCommandId, ServerCommandId, SessionId } from "./types";


// ADVR_V1\0
export const advert1 = [65, 68, 86, 82, 95, 86, 49, 0,];
// DOTNET_IPC_V1\0
export const dotnetIpcV1 = [68, 79, 84, 78, 69, 84, 95, 73, 80, 67, 95, 86, 49, 0];

// this file contains the IPC commands that are sent by client (like dotnet-trace) to the diagnostic server (like dotnet VM in the browser)
// just formatting bytes, no sessions management here


export function advertise() {
    // xxxxxxxx-xxxx-4xxx-xxxx-xxxxxxxxxxxx
    const uuid = new Uint8Array(16);
    globalThis.crypto.getRandomValues(uuid);
    uuid[7] = (uuid[7] & 0xf) | 0x40;// version 4

    const pid = 42;

    return Uint8Array.from([
        ...advert1,
        ...uuid,
        ...serializeUint64([0, pid]),
        0, 0// future
    ]);
}

export function commandStopTracing(sessionID: SessionId) {
    return Uint8Array.from([
        ...serializeHeader(CommandSetId.EventPipe, EventPipeCommandId.StopTracing, computeMessageByteLength(8)),
        ...serializeUint64(sessionID),
    ]);
}

export function commandResumeRuntime() {
    return Uint8Array.from([
        ...serializeHeader(CommandSetId.Process, ProcessCommandId.ResumeRuntime, computeMessageByteLength(0)),
    ]);
}

export function commandProcessInfo3() {
    return Uint8Array.from([
        ...serializeHeader(CommandSetId.Process, ProcessCommandId.ProcessInfo3, computeMessageByteLength(0)),
    ]);
}

export function commandGcHeapDump(options: DiagnosticCommandOptions) {
    return commandCollectTracing2({
        circularBufferMB: options.circularBufferMB ?? 256,
        format: 1,
        requestRundown: true,
        providers: [
            {
                keywords: [
                    0x0000_0000,
                    Keywords.GCHeapSnapshot, // 0x1980001
                    // GC_HEAP_DUMP_VTABLE_CLASS_REF_KEYWORD 0x8000000
                    // GC_FINALIZATION_KEYWORD               0x1000000
                    // GC_HEAP_COLLECT_KEYWORD               0x0800000
                    // GC_KEYWORD                            0x0000001
                ],
                logLevel: 5,
                providerName: "Microsoft-Windows-DotNETRuntime",
                arguments: null
            },
            ...options.extraProviders || [],
        ]
    });
}

function uuidv4() {
    return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
        (+c ^ globalThis.crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16)
    );
}

export function commandCounters(options: DiagnosticCommandOptions) {
    return commandCollectTracing2({
        circularBufferMB: options.circularBufferMB ?? 256,
        format: 1,
        requestRundown: false,
        providers: [
            {
                keywords: [0, Keywords.GCHandle],
                logLevel: 4,
                providerName: "System.Diagnostics.Metrics",
                arguments: `SessionId=SHARED;Metrics=System.Runtime;RefreshInterval=${options.intervalSeconds || 1};MaxTimeSeries=1000;MaxHistograms=10;ClientId=${uuidv4()};`,
            },
            ...options.extraProviders || [],
        ]
    });
}

export function commandSampleProfiler(options: DiagnosticCommandOptions) {
    return commandCollectTracing2({
        circularBufferMB: options.circularBufferMB ?? 256,
        format: 1,
        requestRundown: true,
        providers: [
            {
                keywords: [
                    0x0000_0000,
                    0x0000_0000,
                ],
                logLevel: 4,
                providerName: "Microsoft-DotNETCore-SampleProfiler",
                arguments: null
            },
            ...options.extraProviders || [],
        ]
    });
}

function commandCollectTracing2(payload2: PayloadV2) {
    const payloadLength = computeCollectTracing2PayloadByteLength(payload2);
    const messageLength = computeMessageByteLength(payloadLength);
    const message = [
        ...serializeHeader(CommandSetId.EventPipe, EventPipeCommandId.CollectTracing2, messageLength),
        ...serializeUint32(payload2.circularBufferMB),
        ...serializeUint32(payload2.format),
        ...serializeUint8(payload2.requestRundown ? 1 : 0),
        ...serializeUint32(payload2.providers.length),
    ];
    for (const provider of payload2.providers) {
        message.push(...serializeUint64(provider.keywords));
        message.push(...serializeUint32(provider.logLevel));
        message.push(...serializeString(provider.providerName));
        message.push(...serializeString(provider.arguments));
    }
    return Uint8Array.from(message);
}


function serializeMagic() {
    return Uint8Array.from(dotnetIpcV1);
}

function serializeUint8(value: number) {
    return Uint8Array.from([value]);
}

function serializeUint16(value: number) {
    return new Uint8Array(Uint16Array.from([value]).buffer);
}

function serializeUint32(value: number) {
    return new Uint8Array(Uint32Array.from([value]).buffer);
}

function serializeUint64(value: [number, number]) {
    // value == [hi, lo]
    return new Uint8Array(Uint32Array.from([value[1], value[0]]).buffer);
}

function serializeString(value: string | null) {
    const message = [];
    if (value === null || value === undefined || value === "") {
        message.push(...serializeUint32(1));
        message.push(...serializeUint16(0));
    } else {
        const len = value.length;
        const hasNul = value[len - 1] === "\0";
        message.push(...serializeUint32(len + (hasNul ? 0 : 1)));
        for (let i = 0; i < len; i++) {
            message.push(...serializeUint16(value.charCodeAt(i)));
        }
        if (!hasNul) {
            message.push(...serializeUint16(0));
        }
    }
    return message;
}

function computeStringByteLength(s: string | null) {
    if (s === undefined || s === null || s === "")
        return 4 + 2; // just length of empty zero terminated string
    return 4 + 2 * s.length + 2; // length + UTF16 + null
}

function computeMessageByteLength(payloadLength: number) {
    const fullHeaderSize = 14 + 2 // magic, len
        + 1 + 1 // commandSet, command
        + 2; // reserved ;
    return fullHeaderSize + payloadLength;
}

function serializeHeader(commandSet: CommandSetId, command: ServerCommandId | EventPipeCommandId | ProcessCommandId, len: number) {
    return Uint8Array.from([
        ...serializeMagic(),
        ...serializeUint16(len),
        ...serializeUint8(commandSet),
        ...serializeUint8(command),
        ...serializeUint16(0), // reserved*/
    ]);
}

function computeCollectTracing2PayloadByteLength(payload2: PayloadV2) {
    let len = 0;
    len += 4; // circularBufferMB
    len += 4; // format
    len += 1; // requestRundown
    len += 4; // providers length
    for (const provider of payload2.providers) {
        len += 8; // keywords
        len += 4; // level
        len += computeStringByteLength(provider.providerName);
        len += computeStringByteLength(provider.arguments);
    }
    return len;
}
