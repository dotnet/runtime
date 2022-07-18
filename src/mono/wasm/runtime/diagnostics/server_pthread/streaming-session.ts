// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import {
    EventPipeSessionIDImpl
} from "../shared/types";
import { EventPipeSocketConnection, takeOverSocket } from "./socket-connection";
import { StreamQueue, allocateQueue } from "./stream-queue";
import type { MockRemoteSocket } from "../mock";
import type { VoidPtr } from "../../types/emscripten";
import cwraps from "../../cwraps";
import {
    EventPipeCommandCollectTracing2,
    EventPipeCollectTracingCommandProvider,
} from "./protocol-client-commands";
import { createEventPipeStreamingSession } from "../shared/create-session";

export class EventPipeStreamingSession {
    constructor(readonly sessionID: EventPipeSessionIDImpl, readonly ws: WebSocket | MockRemoteSocket,
        readonly queue: StreamQueue, readonly connection: EventPipeSocketConnection) { }
}

export async function makeEventPipeStreamingSession(ws: WebSocket | MockRemoteSocket, cmd: EventPipeCommandCollectTracing2): Promise<EventPipeStreamingSession> {
    // First, create the native IPC stream and get its queue.
    const ipcStreamAddr = cwraps.mono_wasm_diagnostic_server_create_stream(); // FIXME: this should be a wrapped in a JS object so we can free it when we're done.
    const queueAddr = mono_wasm_diagnostic_server_get_stream_queue(ipcStreamAddr);
    // then take over the websocket connection
    const conn = takeOverSocket(ws);
    // and set up queue notifications
    const queue = allocateQueue(queueAddr, conn.write.bind(conn), conn.close.bind(conn));
    const options = {
        rundownRequested: cmd.requestRundown,
        bufferSizeInMB: cmd.circularBufferMB,
        providers: providersStringFromObject(cmd.providers),
    };
    // create the event pipe session
    const sessionID = createEventPipeStreamingSession(ipcStreamAddr, options);
    if (sessionID === false)
        throw new Error("failed to create event pipe session");
    return new EventPipeStreamingSession(sessionID, ws, queue, conn);
}


function providersStringFromObject(providers: EventPipeCollectTracingCommandProvider[]) {
    const providersString = providers.map(providerToString).join(",");
    return providersString;

    function providerToString(provider: EventPipeCollectTracingCommandProvider): string {
        const keyword_str = provider.keywords[0] === 0 && provider.keywords[1] === 0 ? "" : keywordsToHexString(provider.keywords);
        const args_str = provider.filter_data === "" ? "" : ":" + provider.filter_data;
        return provider.provider_name + ":" + keyword_str + ":" + provider.logLevel + args_str;
    }

    function keywordsToHexString(k: [number, number]): string {
        const lo = k[0];
        const hi = k[1];
        const lo_hex = lo.toString(16);
        const hi_hex = hi.toString(16);
        return hi_hex + lo_hex;
    }
}


const IPC_STREAM_QUEUE_OFFSET = 4; /* keep in sync with mono_wasm_diagnostic_server_create_stream() in C */
function mono_wasm_diagnostic_server_get_stream_queue(streamAddr: VoidPtr): VoidPtr {
    // TODO: this can probably be in JS if we put the queue at a known address in the stream. (probably offset 4);
    return <any>streamAddr + IPC_STREAM_QUEUE_OFFSET;
}
