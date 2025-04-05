// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { GlobalObjects } from "../types/internal";
import type { CharPtr, VoidPtr } from "../types/emscripten";

import { Module, runtimeHelpers } from "./globals";
import { cleanup_client as cleanup_js_client, createDiagConnectionJs, serverSession } from "./diag-js";
import { IDiagConnection } from "./common";
import { createDiagConnectionWs } from "./diag-ws";
import { diagnosticHelpers, setRuntimeGlobalsImpl } from "./globals";
import { collectCpuSamples } from "./dotnet-cpu-profiler";
import { collectPerfCounters } from "./dotnet-counters";
import { collectGcDump } from "./dotnet-gcdump";
import { advert1Full } from "./client-commands";

let socket_handles:Map<number, IDiagConnection> = undefined as any;
let next_socket_handle = 1;
let url_override:string | undefined = undefined;

export function setRuntimeGlobals (globalObjects: GlobalObjects): void {
    setRuntimeGlobalsImpl(globalObjects);

    diagnosticHelpers.ds_rt_websocket_create = (urlPtr :CharPtr):number => {
        if (!socket_handles) {
            socket_handles = new Map<number, IDiagConnection>();
        }
        const url = url_override ?? runtimeHelpers.utf8ToString(urlPtr);
        const socket_handle = next_socket_handle++;
        const isWebSocket = url.startsWith("ws://") || url.startsWith("wss://");
        const wrapper = isWebSocket
            ? createDiagConnectionWs(socket_handle, url)
            : createDiagConnectionJs(socket_handle, url);
        socket_handles.set(socket_handle, wrapper);
        return socket_handle;
    };

    diagnosticHelpers.ds_rt_websocket_send = (client_socket :number, buffer:VoidPtr, bytes_to_write:number):number => {
        const wrapper = socket_handles.get(client_socket);
        if (!wrapper) {
            return -1;
        }
        const message = (new Uint8Array(Module.HEAPU8.buffer, buffer as any, bytes_to_write)).slice();
        return wrapper.send(message);
    };

    diagnosticHelpers.ds_rt_websocket_poll = (client_socket :number):number => {
        const wrapper = socket_handles.get(client_socket);
        if (!wrapper) {
            return 0;
        }
        return wrapper.poll();
    };

    diagnosticHelpers.ds_rt_websocket_recv = (client_socket :number, buffer:VoidPtr, bytes_to_read:number):number => {
        const wrapper = socket_handles.get(client_socket);
        if (!wrapper) {
            return -1;
        }
        return wrapper.recv(buffer, bytes_to_read);
    };

    diagnosticHelpers.ds_rt_websocket_close = (client_socket :number):number => {
        const wrapper = socket_handles.get(client_socket);
        if (!wrapper) {
            return -1;
        }
        socket_handles.delete(client_socket);
        return wrapper.close();
    };

    globalObjects.api.collectCpuSamples = collectCpuSamples;
    globalObjects.api.collectPerfCounters = collectPerfCounters;
    globalObjects.api.collectGcDump = collectGcDump;
    globalObjects.api.connectDSRouter = connectDSRouter;

    cleanup_js_client();
}

// this will take over the existing connection to JS and send new advert message to WS client
// use dotnet-dsrouter server-websocket -v trace
function connectDSRouter (url: string): void {
    if (!serverSession) {
        throw new Error("No active session to reconnect");
    }

    // make sure new sessions hit the new URL
    url_override = url;

    const wrapper = createDiagConnectionWs(serverSession.client_socket, url);
    socket_handles.set(serverSession.client_socket, wrapper);
    wrapper.send(new Uint8Array(advert1Full));
}
