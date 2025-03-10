// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { GlobalObjects } from "../types/internal";
import type { CharPtr, VoidPtr } from "../types/emscripten";

import { Module, runtimeHelpers } from "./globals";
import { createDiagConnectionJs } from "./diag-js";
import { IDiagConnection } from "./common";
import { createDiagConnectionWs } from "./diag-ws";
import { diagnosticHelpers, setRuntimeGlobalsImpl } from "./globals";

let socket_handles:Map<number, IDiagConnection> = undefined as any;
let next_socket_handle = 1;

export function setRuntimeGlobals (globalObjects: GlobalObjects): void {
    setRuntimeGlobalsImpl(globalObjects);

    diagnosticHelpers.ds_rt_websocket_create = (urlPtr :CharPtr):number => {
        if (!socket_handles) {
            socket_handles = new Map<number, IDiagConnection>();
        }
        const url = runtimeHelpers.utf8ToString(urlPtr);
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

    diagnosticHelpers. ds_rt_websocket_close = (client_socket :number):number => {
        const wrapper = socket_handles.get(client_socket);
        if (!wrapper) {
            return -1;
        }
        socket_handles.delete(client_socket);
        return wrapper.close();
    };

    globalObjects.api.collectTrace = async () => {
        //TODO
    };
}
